using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;

/// <summary>
/// Top. Rigidbody YOK: hareketi kod yapar, duvarları CircleCast ile bulup Vector2.Reflect ile seker.
/// Keeper'a değerse durur (hız ve dönüş sıfır), keeper'ın önüne yapışır ve onunla hareket eder.
/// Space ile karşıya fırlatılır. Tüm kararları host verir.
/// Prefab: NetworkObject + NetworkTransform + bu script. Collider GEREKMEZ.
/// </summary>
public class Ball : NetworkBehaviour
{
    public static readonly List<Ball> All = new List<Ball>();

    [SerializeField] float radius = 0.3f;
    [SerializeField] float throwSpeed = 14f;
    [SerializeField] float serveSpeed = 8f;
    [Tooltip("Keeper merkezinden topun duracağı mesafe (yarıçaplar hariç ek boşluk)")]
    [SerializeField] float holdGap = 0.08f;
    [Tooltip("Fırlatırken sağ/sol tuşunun yönü ne kadar saptıracağı (0 = hep tam karşıya)")]
    [SerializeField, Range(0f, 1f)] float aimInfluence = 0.4f;
    [SerializeField] float rethrowIgnoreSeconds = 0.25f;

    const float Skin = 0.01f;
    const int MaxBouncesPerTick = 3;

    [Networked] public Vector2 Velocity { get; set; }
    [Networked] public int HolderSlot { get; set; }      // -1 = serbest
    [Networked] public int LastThrowerSlot { get; set; }
    [Networked] public TickTimer IgnoreThrower { get; set; }

    // Güçlü Şut yeteneği
    [Networked] public NetworkBool Boosted { get; set; }   // hızlandırılmış şut: ilk çarpmada normale döner
    [Networked] public int ChainOwnerSlot { get; set; }    // zincirli şut sahibi (-1 = yok)
    [Networked] public int ChainReturnsLeft { get; set; }  // gol olursa kaç kez daha geri dönecek
    [Networked] public int ReturnToSlot { get; set; }      // geri dönüyor (-1 = hayır). Bu sırada tutulamaz

    // Top Boyutu yeteneği
    [Networked] public float SizeScale { get; set; }        // 1 = normal (0 da normal sayılır)
    [Networked] public TickTimer SizeTimer { get; set; }    // süre bitince normale döner

    /// Topun o anki yarıçapı (boyut yeteneği dahil).
    public float R => radius * (SizeScale > 0f ? SizeScale : 1f);

    /// Host: topun boyutunu belirli süreliğine değiştirir.
    public void ServerSetSize(float scale, float seconds)
    {
        if (!HasStateAuthority) return;
        SizeScale = scale;
        SizeTimer = TickTimer.CreateFromSeconds(Runner, seconds);
    }

    // Çoğalan Şut yeteneği: aynı gruptaki toplar ilk temasta yok olur, sona kalan gerçek top olur
    [Networked] public int SplitGroup { get; set; }         // 0 = normal top
    bool _dead;                                              // host: bu tick yok edildi (sayımda atlanır)
    static int _nextSplitGroup;

    public bool IsSplitCopy => SplitGroup != 0;

    // Yem Top yeteneği: sahte top ilk temasta yok olur
    [Networked] public NetworkBool IsDecoy { get; set; }
    [Networked] public NetworkBool DecoyScores { get; set; }   // sahte gol sayılır mı (seviye 2+)
    [Networked] public float DecoyStunSeconds { get; set; }    // golü yiyen kaç sn hareketsiz kalır (seviye 3)

    // Sıcak Bomba: süre dolunca top kimdeyse / en son kimden çıktıysa o kişi BombDamage can kaybeder
    [Networked] public TickTimer BombTimer { get; set; }
    [Networked] public int BombDamage { get; set; }
    [Networked] public int BombOwnerSlot { get; set; }

    public bool HasBomb => BombDamage > 0 && Runner != null && BombTimer.IsRunning;

    /// Host: topa bomba kurar.
    public void ServerArmBomb(int ownerSlot, int damage, float seconds)
    {
        if (!HasStateAuthority || HasBomb) return;
        BombOwnerSlot = ownerSlot;
        BombDamage = damage;
        BombTimer = TickTimer.CreateFromSeconds(Runner, seconds);
    }

    /// Host: süre dolduysa bomba patlar.
    void ServerTickBomb()
    {
        if (BombDamage <= 0 || !BombTimer.Expired(Runner)) return;

        int victim = IsHeld ? HolderSlot : LastThrowerSlot;
        int damage = BombDamage;
        int owner = BombOwnerSlot;
        BombDamage = 0;
        BombTimer = TickTimer.None;

        MatchManager mm = MatchManager.Instance;
        if (mm != null && victim >= 0 && mm.IsAlive(victim))
            mm.ServerDamage(victim, damage, owner); // gol yemiş sayılır
    }

    /// Kopya yok olurken bombası grupta kalan bir topa geçer.
    void PassBombTo(Ball other)
    {
        other.BombDamage = BombDamage;
        other.BombOwnerSlot = BombOwnerSlot;
        other.BombTimer = BombTimer;
        BombDamage = 0;
    }

    // Kamikaze Şut: gol olursa iki taraf da bu kadar can kaybeder (0 = normal)
    [Networked] public int KamikazeDamage { get; set; }

    // Falso yeteneği
    [Networked] public float CurveRate { get; set; }   // işaretli dönüş hızı (derece/sn), 0 = düz
    [Networked] public float CurveDecay { get; set; }  // dönüşün saniyedeki azalma oranı

    // Görünmez Şut yeteneği
    [Networked] public int InvisibleOwnerSlot { get; set; } // top bu oyuncu dışındakilere görünmez (-1 = görünür)
    [Networked] public int InvisibleHitsLeft { get; set; }  // görünmezken kaç çarpmaya daha dayanır

    public bool IsHeld => HolderSlot >= 0;

    Transform _visual;
    SpriteRenderer _sprite;

    /// Host, spawn'dan hemen önce çağırır.
    public void Init(Vector2 velocity)
    {
        Velocity = velocity;
        HolderSlot = -1;
        LastThrowerSlot = -1;
        ChainOwnerSlot = -1;
        ReturnToSlot = -1;
        InvisibleOwnerSlot = -1;
        SplitGroup = 0;
        SizeScale = 1f;
        CurveRate = 0f;
        IsDecoy = false;
        KamikazeDamage = 0;
    }

    /// Bu oyuncunun elindeki top (yoksa null).
    public static Ball HeldBy(int slot)
    {
        foreach (Ball b in All)
            if (b.HolderSlot == slot) return b;
        return null;
    }

    /// Host: top, sahibi dışındaki herkese görünmez olur.
    public void MakeInvisible(int ownerSlot, int hitsThrough)
    {
        InvisibleOwnerSlot = ownerSlot;
        InvisibleHitsLeft = hitsThrough;
    }

    public override void Spawned()
    {
        All.Add(this);
        transform.rotation = Quaternion.identity;
        CreateVisual();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        All.Remove(this);
    }

    // ---------------- Simülasyon (sadece host) ----------------

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        if (SizeTimer.Expired(Runner)) // boyut süresi bitti
        {
            SizeScale = 1f;
            SizeTimer = TickTimer.None;
        }
        if (MatchManager.Instance == null || MatchManager.Instance.Phase != MatchPhase.Playing) return;
        ServerTickBomb(); // Sıcak Bomba

        if (IsHeld)
        {
            KeeperController holder = KeeperController.FindBySlot(HolderSlot);
            if (holder == null) // tutan oyuncu elendi/çıktı
            {
                EndChain();
                InvisibleOwnerSlot = -1;
                Release(RandomServeVelocity(serveSpeed));
                return;
            }
            transform.position = HoldPosition(holder);
            return;
        }

        if (ReturnToSlot >= 0)
        {
            MoveBackToOwner();
            return;
        }

        MoveWithBounces(transform.position, Runner.DeltaTime);
        if (!_dead) KeepInsideArena();
    }

    /// Topu ilerletir. Her düz parçada önce keeper'a değdi mi bakar (değdiyse tutulur),
    /// sonra duvara/kaleye çarptıysa seker. Böylece keeper'ın arkasındaki kale yanlışlıkla sayılmaz.
    void MoveWithBounces(Vector2 pos, float dt)
    {
        ArenaLayout arena = ArenaLayout.Instance;
        pos = Depenetrate(pos, arena.BallCastMask); // büyüyünce duvarın içine girdiyse önce dışarı it
        Vector2 vel = Velocity;
        ApplyMagnets(pos, ref vel, dt);

        if (CurveRate != 0f) // Falso: yönü döndür, dönüş gittikçe azalır (çember büyür)
        {
            vel = (Vector2)(Quaternion.Euler(0f, 0f, CurveRate * dt) * (Vector3)vel);
            float rate = CurveRate * Mathf.Exp(-CurveDecay * dt);
            CurveRate = Mathf.Abs(rate) < 2f ? 0f : rate;
        }

        float remaining = vel.magnitude * dt;

        for (int i = 0; i < MaxBouncesPerTick && remaining > 0.0001f; i++)
        {
            Vector2 dir = vel.normalized;
            RaycastHit2D hit = Physics2D.CircleCast(pos, R, dir, remaining + Skin, arena.BallCastMask);
            bool blocked = hit && hit.fraction > 0f;
            float hitDistance = blocked ? hit.distance : float.MaxValue;
            Vector2 hitNormal = blocked ? hit.normal : Vector2.zero;
            Collider2D hitCollider = blocked ? hit.collider : null;

            // Yetenek duvarları (Duvar Örme) sahnede collider değil; kendimiz kontrol ediyoruz
            WallSkill hitWall = null;
            int hitWallAction = -1;
            if (CastSkillWalls(pos, dir, remaining + Skin, out float wallDist, out Vector2 wallNormal, out WallSkill wall, out int wallAction)
                && wallDist < hitDistance)
            {
                blocked = true;
                hitDistance = wallDist;
                hitNormal = wallNormal;
                hitCollider = null;
                hitWall = wall;
                hitWallAction = wallAction;
            }

            float travel = blocked ? Mathf.Max(0f, hitDistance - Skin) : remaining;
            Vector2 next = pos + dir * travel;

            // Ayna: kaleye doğru gelen top geldiği yöne geri döner
            if (CastMirrors(pos, dir, travel, out float mirrorDist, out MirrorSkill mirror))
            {
                pos += dir * mirrorDist;
                remaining -= mirrorDist;

                if (IsDecoy) { DespawnSelf(); return; }                        // sahte top temasla yok olur
                if (SplitGroup != 0 && AliveInGroup(SplitGroup) > 1) { DieAsSplitCopy(); return; }

                vel = -vel * mirror.SpeedMultiplier;
                ApplyMirror(mirror);
                continue;
            }

            if (TryCatch(pos, next)) return; // tutuldu: pozisyonu TryCatch ayarladı

            pos = next;
            remaining -= travel;
            if (!blocked) break;

            int hitGoal = hitCollider != null ? arena.GoalSlotOf(hitCollider) : -1;

            // Yem Top: sahte top ilk temasta yok olur (seviye 2+ kaleye girdiyse gol sayılır)
            if (IsDecoy)
            {
                if (hitGoal >= 0 && DecoyScores && MatchManager.Instance != null)
                {
                    MatchManager.Instance.ServerGoal(hitGoal, LastThrowerSlot);
                    KeeperController victim = KeeperController.FindBySlot(hitGoal);
                    if (victim != null && DecoyStunSeconds > 0f && MatchManager.Instance.IsAlive(hitGoal)) victim.ServerStun(DecoyStunSeconds);
                }
                DespawnSelf();
                return;
            }

            // Çoğalan Şut: grupta başka top varken bir şeye çarpan top yok olur (gol olduysa gol sayılır)
            if (SplitGroup != 0 && AliveInGroup(SplitGroup) > 1)
            {
                if (hitWall != null) hitWall.ServerBreak(hitWallAction);
                if (hitGoal >= 0 && MatchManager.Instance != null)
                    MatchManager.Instance.ServerGoal(hitGoal, LastThrowerSlot);
                DieAsSplitCopy();
                return;
            }

            vel = Vector2.Reflect(vel, hitNormal); // sekme
            CurveRate = 0f;                        // falso çarpınca biter
            if (hitWall != null) hitWall.ServerBreak(hitWallAction); // yetenek duvarı tek çarpmada yok olur

            if (InvisibleOwnerSlot >= 0) // görünmez top: hakkı varsa korunur, yoksa görünür olur
            {
                if (InvisibleHitsLeft > 0) InvisibleHitsLeft--;
                else InvisibleOwnerSlot = -1;
            }

            if (Boosted) // güçlü şut ilk çarpmada normal hıza döner
            {
                vel = vel.normalized * throwSpeed;
                Boosted = false;
            }

            int goalSlot = hitGoal;
            if (goalSlot >= 0 && MatchManager.Instance != null)
            {
                int kamikaze = KamikazeDamage; // Kamikaze Şut: gol olunca biter
                KamikazeDamage = 0;
                MatchManager.Instance.ServerGoal(goalSlot, LastThrowerSlot, kamikaze); // o oyuncu can kaybeder
            }

            if (ChainOwnerSlot >= 0)
            {
                bool scored = goalSlot >= 0 && goalSlot != ChainOwnerSlot;
                if (scored && ChainReturnsLeft > 0 && KeeperController.FindBySlot(ChainOwnerSlot) != null)
                {
                    // Zincirli şut gol oldu: top şutu atana geri dönsün
                    ChainReturnsLeft--;
                    ReturnToSlot = ChainOwnerSlot;
                    Velocity = Vector2.zero;
                    transform.position = pos;
                    return;
                }
                EndChain(); // duvara çarptı, kendi kalesi ya da dönüş hakkı bitti
            }
        }

        Velocity = vel;
        transform.position = pos;
    }

    static readonly Collider2D[] _overlaps = new Collider2D[8];

    /// Top bir duvarın/kalenin içine girmişse (ör. duvar dibinde büyütülünce) onu dışarı iter.
    /// Yoksa CircleCast içeriden başladığı için duvarı göremez ve top arenadan kaçardı.
    Vector2 Depenetrate(Vector2 pos, int mask)
    {
        float r = R;
        var filter = new ContactFilter2D { useTriggers = true };
        filter.SetLayerMask(mask);

        for (int iteration = 0; iteration < 2; iteration++)
        {
            int n = Physics2D.OverlapCircle(pos, r, filter, _overlaps);
            if (n == 0) break;

            for (int i = 0; i < n; i++)
            {
                Vector2 closest = _overlaps[i].ClosestPoint(pos);
                Vector2 delta = pos - closest;
                float d = delta.magnitude;
                if (d >= r) continue;

                Vector2 normal = d > 0.0001f ? delta / d : (ArenaLayout.Instance.Center - pos).normalized; // merkez duvarın içindeyse arenaya doğru
                pos = closest + normal * (r + Skin);
            }
        }
        return pos;
    }

    /// Son güvenlik: top her ne olursa olsun arenadan çıktıysa ortaya geri gelir.
    void KeepInsideArena()
    {
        ArenaLayout arena = ArenaLayout.Instance;
        Vector2 pos = transform.position;
        if ((pos - arena.Center).sqrMagnitude <= arena.BoundsRadius * arena.BoundsRadius) return;

        if (IsDecoy) { DespawnSelf(); return; } // kaçan sahte top yok olur

        if (SplitGroup != 0 && AliveInGroup(SplitGroup) > 1)
        {
            DieAsSplitCopy(); // kaçan kopya yok olur
            return;
        }

        transform.position = arena.Center;
        Velocity = RandomServeVelocity(serveSpeed);
        Boosted = false;
        KamikazeDamage = 0;
        CurveRate = 0f;
    }

    /// Yetenek duvarlarıyla çarpışma: topun önündeki en yakın duvarı bulur.
    /// Duvar = kalın bir çizgi; top yarıçapı + yarım kalınlık mesafesinde değer.
    bool CastSkillWalls(Vector2 pos, Vector2 dir, float maxDist,
        out float hitDist, out Vector2 normal, out WallSkill hitSkill, out int hitAction)
    {
        hitDist = float.MaxValue;
        normal = Vector2.zero;
        hitSkill = null;
        hitAction = -1;

        foreach (KeeperController k in KeeperController.All)
        {
            WallSkill w = k.GetSkill<WallSkill>();
            if (w == null) continue;

            for (int a = 0; a < 2; a++)
            {
                if (!w.IsUp(a)) continue;

                w.GetWall(a, out Vector2 p0, out Vector2 p1);
                Vector2 along = p1 - p0;
                float len = along.magnitude;
                if (len < 0.0001f) continue;

                Vector2 t = along / len;
                Vector2 n = new Vector2(-t.y, t.x);
                float reach = R + w.Thickness * 0.5f;

                float d0 = Vector2.Dot(pos - p0, n);
                float side = d0 >= 0f ? 1f : -1f;
                float approach = -Vector2.Dot(dir, n) * side; // > 0: duvara doğru gidiyor
                if (approach <= 0.00001f) continue;

                float gap = Mathf.Abs(d0) - reach;
                if (gap < 0f) continue; // zaten içinde

                float s = gap / approach;
                if (s > maxDist || s >= hitDist) continue;

                float u = Vector2.Dot(pos + dir * s - p0, t);
                if (u < -R || u > len + R) continue; // duvarın yanından geçiyor

                hitDist = s;
                normal = n * side;
                hitSkill = w;
                hitAction = a;
            }
        }
        return hitSkill != null;
    }

    /// Mıknatıs Eldiven: yakındaki keeper'a doğru gelen topun yönü ona doğru kıvrılır (hız aynı kalır).
    void ApplyMagnets(Vector2 pos, ref Vector2 vel, float dt)
    {
        float speed = vel.magnitude;
        if (speed < 0.01f) return;

        foreach (KeeperController k in KeeperController.All)
        {
            MagnetSkill m = k.GetSkill<MagnetSkill>();
            if (m == null || m.Level <= 0) continue;
            if (k.Slot == LastThrowerSlot && !IgnoreThrower.ExpiredOrNotRunning(Runner)) continue; // yeni attığı top

            Vector2 toKeeper = k.WorldPosition - pos;
            float dist = toKeeper.magnitude;
            if (dist > m.PullRadius || dist < 0.001f) continue;
            if (Vector2.Dot(vel, toKeeper) <= 0f) continue; // uzaklaşan topu çekmez

            float closeness = 1f - dist / m.PullRadius;      // yakınlaştıkça güçlenir
            float maxTurn = m.PullDegreesPerSecond * closeness * dt;
            float angle = Vector2.SignedAngle(vel, toKeeper);
            float turn = Mathf.Clamp(angle, -maxTurn, maxTurn);
            vel = (Vector2)(Quaternion.Euler(0f, 0f, turn) * (Vector3)vel);
        }
        vel = vel.normalized * speed;
    }

    /// Host (Shinra Tensei): top geldiği yöne, daha hızlı geri fırlar. Artık itenin şutu sayılır.
    public void ServerPushBack(KeeperController pusher, float speedMultiplier)
    {
        if (!HasStateAuthority || IsHeld || ReturnToSlot >= 0) return;

        EndChain();
        KamikazeDamage = 0;
        CurveRate = 0f;
        InvisibleOwnerSlot = -1;
        LastThrowerSlot = pusher.Slot;
        IgnoreThrower = TickTimer.CreateFromSeconds(Runner, rethrowIgnoreSeconds);
        Velocity = -Velocity * speedMultiplier;
        Boosted = speedMultiplier > 1f; // ilk çarpmada normal hıza iner
    }

    /// Host (Mıknatıs Eldiven seviye 3): top kimdeyse ondan koparılır, tutulamaz halde 'slot'a uçar ve onun olur.
    public void ServerStealTo(int slot)
    {
        if (!HasStateAuthority) return;

        if (HolderSlot >= 0) // elinden alınan keeper'ın görünmezliği de biter
        {
            KeeperController victim = KeeperController.FindBySlot(HolderSlot);
            InvisibleShotSkill inv = victim != null ? victim.GetSkill<InvisibleShotSkill>() : null;
            if (inv != null) inv.KeeperHidden = false;
        }

        HolderSlot = -1;          // eski sahibin şut hakkı biter
        EndChain();
        KamikazeDamage = 0;
        Boosted = false;
        InvisibleOwnerSlot = -1;
        CurveRate = 0f;
        Velocity = Vector2.zero;
        ReturnToSlot = slot;      // MoveBackToOwner: tutulamaz halde gelir, varınca HolderSlot = slot
    }

    /// Ayna yeteneği: topun önündeki en yakın aynayı bulur (top + ayna yarıçapı = değme mesafesi).
    bool CastMirrors(Vector2 pos, Vector2 dir, float maxDist, out float hitDist, out MirrorSkill hitMirror)
    {
        hitDist = float.MaxValue;
        hitMirror = null;

        foreach (KeeperController k in KeeperController.All)
        {
            MirrorSkill m = k.GetSkill<MirrorSkill>();
            if (m == null || !m.IsUp || !m.Catches(dir)) continue;

            float reach = R + m.MirrorRadius;
            Vector2 toBall = pos - m.WorldPos;
            float b = Vector2.Dot(toBall, dir);
            float c = toBall.sqrMagnitude - reach * reach;

            float t;
            if (c <= 0f) t = 0f; // zaten aynanın içinde
            else
            {
                float disc = b * b - c;
                if (b > 0f || disc < 0f) continue; // uzaklaşıyor ya da yanından geçiyor
                t = -b - Mathf.Sqrt(disc);
            }

            if (t > maxDist || t >= hitDist) continue;
            hitDist = t;
            hitMirror = m;
        }
        return hitMirror != null;
    }

    /// Host: top aynadan döndü. Artık aynanın sahibinin şutu sayılır.
    void ApplyMirror(MirrorSkill mirror)
    {
        int owner = mirror.OwnerSlot;
        EndChain();
        KamikazeDamage = 0;
        LastThrowerSlot = owner;
        IgnoreThrower = TickTimer.None;
        CurveRate = 0f;
        if (mirror.SpeedMultiplier > 1f) Boosted = true; // hızlı dönüş ilk çarpmada normale iner
        if (mirror.SendsInvisible) MakeInvisible(owner, 0); // ilk çarpmada görünür olur
        else InvisibleOwnerSlot = -1;
    }

    /// Topun bu tick'te aldığı yol bir keeper'a değdi mi? (hızlı topta bile kaçırmaz)
    bool TryCatch(Vector2 from, Vector2 to)
    {
        foreach (KeeperController k in KeeperController.All)
        {
            if (k.Slot == LastThrowerSlot && !IgnoreThrower.ExpiredOrNotRunning(Runner)) continue;

            float touch = R + k.Radius;
            Vector2 keeperPos = k.WorldPosition;
            if (DistancePointSegment(keeperPos, from, to) <= touch)
            {
                if (IsDecoy) // sahte top keeper'a değince yok olur
                {
                    DespawnSelf();
                    return true;
                }

                // Çoğalan Şut: grupta başka top varken keeper'a değen top tutulmaz, yok olur
                if (SplitGroup != 0 && AliveInGroup(SplitGroup) > 1)
                {
                    DieAsSplitCopy();
                    return true;
                }

                EndChain(); // zincirli şut tutulursa biter
                Boosted = false;
                InvisibleOwnerSlot = -1; // tutulan top görünür olur
                KamikazeDamage = 0;
                CurveRate = 0f;
                HolderSlot = k.Slot;
                Velocity = Vector2.zero;           // hız sıfır
                transform.rotation = Quaternion.identity; // dönüş sıfır
                transform.position = HoldPosition(k);
                return true;
            }
        }
        return false;
    }

    /// Host çağırır: tutan keeper Space'e bastı.
    /// localAim: joystick ile nişan alındıysa yön (oyuncunun ekranına göre, y > 0). Sıfırsa klavye nişanı kullanılır.
    public void ServerThrow(KeeperController thrower, float aimX, Vector2 localAim = default)
    {
        if (!HasStateAuthority || HolderSlot != thrower.Slot) return;

        ArenaLayout arena = ArenaLayout.Instance;
        Vector2 dir;
        if (localAim.y > 0.01f) // joystick: tam o yöne
            dir = (arena.Right(thrower.Slot) * localAim.x + arena.Inward(thrower.Slot) * localAim.y).normalized;
        else                    // klavye: karşıya, sağ/sol tuşuyla biraz sapma
            dir = (arena.Inward(thrower.Slot) + arena.Right(thrower.Slot) * (Mathf.Clamp(aimX, -1f, 1f) * aimInfluence)).normalized;

        float side = localAim.y > 0.01f ? localAim.x : Mathf.Clamp(aimX, -1f, 1f);
        Launch(thrower, dir, side, 1f, withSkills: true, fromHand: true);
    }

    /// Host (Telekinezi): serbest top olduğu yerden 'worldDir' yönüne fırlatılır. Önceki hızı/etkileri sıfırlanır.
    /// withSkills: atanın şut yetenekleri de uygulanır (Telekinezi 3. seviye).
    public void ServerTelekinesis(KeeperController thrower, Vector2 worldDir, float side, float speedMultiplier, bool withSkills)
    {
        if (!HasStateAuthority || IsHeld || ReturnToSlot >= 0 || IsDecoy || IsSplitCopy) return;

        EndChain();
        Boosted = false;
        CurveRate = 0f;
        InvisibleOwnerSlot = -1;
        KamikazeDamage = 0;
        Launch(thrower, worldDir.normalized, side, speedMultiplier, withSkills, fromHand: false);
    }

    /// Şutun ortak kısmı: hız, Güçlü Şut, Falso, Görünmez Şut, Yem Top, Çoğalan Şut.
    void Launch(KeeperController thrower, Vector2 dir, float side, float speedMultiplier, bool withSkills, bool fromHand)
    {
        ArenaLayout arena = ArenaLayout.Instance;

        PowerShotSkill powerShot = withSkills ? thrower.GetSkill<PowerShotSkill>() : null;
        float bonus = powerShot != null ? throwSpeed * powerShot.ThrowSpeedBonusRatio : 0f; // normal hız × oran

        if (powerShot != null && powerShot.ConsumeArmed())
        {
            ChainOwnerSlot = thrower.Slot;               // yeni zincirli şut
            ChainReturnsLeft = powerShot.MaxReturns;
        }
        else if (ChainOwnerSlot != thrower.Slot)
        {
            EndChain();                                  // geri dönen topu atıyorsa zincir sürer
        }

        CurveSkill curve = withSkills ? thrower.GetSkill<CurveSkill>() : null; // Falso
        if (curve != null) // düz şut: kavis yok, hafif çapraz gider
            dir = (Vector2)(Quaternion.Euler(0f, 0f, curve.StraightTilt(side)) * (Vector3)dir);

        float speed = (throwSpeed + bonus) * speedMultiplier;
        Boosted = speed > throwSpeed + 0.01f; // hızlı şut ilk çarpmada normale iner
        if (fromHand) Release(dir * speed);
        else
        {
            LastThrowerSlot = thrower.Slot; // gol olursa onun golü
            IgnoreThrower = TickTimer.None;
            Velocity = dir * speed;
            CurveRate = 0f;
        }

        if (!withSkills) return;

        InvisibleShotSkill invisible = thrower.GetSkill<InvisibleShotSkill>();
        if (invisible != null) invisible.ServerOnThrow(this); // seviye 3: keeper görünür, top görünmez gider

        // Falso: yana meyilli şut o yana kavis alır
        if (curve != null && curve.GetCurve(side, out float turnSide, out float rate, out float decay))
        {
            Vector2 turnTo = arena.Right(thrower.Slot) * turnSide;
            float cross = dir.x * turnTo.y - dir.y * turnTo.x; // + ise saat yönünün tersine dönmeli
            CurveRate = cross >= 0f ? rate : -rate;
            CurveDecay = decay;
        }

        KamikazeSkill kamikaze = thrower.GetSkill<KamikazeSkill>(); // Kamikaze Şut
        if (kamikaze != null && kamikaze.ConsumeArmed())
            KamikazeDamage = kamikaze.Damage;

        DecoySkill decoy = thrower.GetSkill<DecoySkill>(); // Yem Top: +açı ve -açı, biri sahte
        if (decoy != null && decoy.ConsumeArmed())
            ServerSpawnDecoy(decoy);

        MultiShotSkill multi = thrower.GetSkill<MultiShotSkill>();
        if (multi != null && multi.ConsumeArmed())
            ServerSplit(multi.BallCount, multi.SpreadDegrees);
    }

    // ---------------- Yem Top ----------------

    /// Host: gerçek top nişanın bir yanına, sahte top öbür yanına gider (hangisi rastgele).
    void ServerSpawnDecoy(DecoySkill skill)
    {
        if (MatchManager.Instance == null) return;

        float realSide = UnityEngine.Random.value < 0.5f ? 1f : -1f;
        Vector2 baseVel = Velocity;
        Vector2 realVel = (Vector2)(Quaternion.Euler(0f, 0f, skill.SplitAngle * realSide) * (Vector3)baseVel);
        Vector2 fakeVel = (Vector2)(Quaternion.Euler(0f, 0f, -skill.SplitAngle * realSide) * (Vector3)baseVel);
        Velocity = realVel;

        bool scores = skill.FakeScores;
        float stun = skill.FakeStunSeconds;
        Runner.Spawn(MatchManager.Instance.BallPrefab, transform.position, Quaternion.identity, null,
            (r, o) => o.GetComponent<Ball>().InitDecoy(this, fakeVel, scores, stun));
    }

    /// Host: sahte topu gerçeğin görünüşüyle hazırlar.
    void InitDecoy(Ball source, Vector2 velocity, bool scores, float stun)
    {
        Velocity = velocity;
        HolderSlot = -1;
        LastThrowerSlot = source.LastThrowerSlot;
        IgnoreThrower = source.IgnoreThrower;
        Boosted = source.Boosted;
        InvisibleOwnerSlot = source.InvisibleOwnerSlot;
        InvisibleHitsLeft = source.InvisibleHitsLeft;
        ChainOwnerSlot = source.ChainOwnerSlot; // sadece renk aynı görünsün diye (sahte ilk temasta yok olur)
        ReturnToSlot = -1;
        SplitGroup = 0;
        SizeScale = source.SizeScale;
        SizeTimer = source.SizeTimer;
        CurveRate = source.CurveRate;
        CurveDecay = source.CurveDecay;
        IsDecoy = true;
        KamikazeDamage = source.KamikazeDamage; // sadece aynı görünsün diye (sahtenin golü kamikaze sayılmaz)
        DecoyScores = scores;
        DecoyStunSeconds = stun;
    }

    void DespawnSelf()
    {
        _dead = true;
        Runner.Despawn(Object);
    }

    // ---------------- Çoğalan Şut ----------------

    /// Host: bu top, yelpaze şeklinde 'count' topa bölünür (kendisi de içlerinden biri).
    void ServerSplit(int count, float spreadDegrees)
    {
        if (count <= 1 || MatchManager.Instance == null) return;

        int group = ++_nextSplitGroup;
        if (group == 0) group = ++_nextSplitGroup;

        EndChain();            // zincirli şut çoğalmaz
        ReturnToSlot = -1;
        SplitGroup = group;

        Vector2 baseVel = Velocity;
        int self = count / 2;  // ortadaki yön bu topta kalır
        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : i / (float)(count - 1);
            float angle = Mathf.Lerp(-spreadDegrees * 0.5f, spreadDegrees * 0.5f, t);
            Vector2 vel = (Vector2)(Quaternion.Euler(0, 0, angle) * (Vector3)baseVel);

            if (i == self)
            {
                Velocity = vel;
                continue;
            }

            Runner.Spawn(MatchManager.Instance.BallPrefab, transform.position, Quaternion.identity, null,
                (r, o) => o.GetComponent<Ball>().InitSplitCopy(this, vel, group));
        }
    }

    /// Host: kopyayı asıl topun özelliklerini taşıyarak hazırlar.
    void InitSplitCopy(Ball source, Vector2 velocity, int group)
    {
        Velocity = velocity;
        HolderSlot = -1;
        LastThrowerSlot = source.LastThrowerSlot;
        IgnoreThrower = source.IgnoreThrower;
        Boosted = source.Boosted;
        InvisibleOwnerSlot = source.InvisibleOwnerSlot;
        InvisibleHitsLeft = source.InvisibleHitsLeft;
        ChainOwnerSlot = -1;
        ReturnToSlot = -1;
        SplitGroup = group;
        SizeScale = source.SizeScale;
        SizeTimer = source.SizeTimer;
        CurveRate = source.CurveRate;
        CurveDecay = source.CurveDecay;
        KamikazeDamage = source.KamikazeDamage; // görünüş (kopyanın golü kamikaze sayılmaz)
    }

    static int AliveInGroup(int group)
    {
        int n = 0;
        foreach (Ball b in All)
            if (b.SplitGroup == group && !b._dead) n++;
        return n;
    }

    /// Host: bu kopya yok olur. Grupta tek top kalırsa o artık normal (gerçek) toptur.
    void DieAsSplitCopy()
    {
        int group = SplitGroup;
        _dead = true;

        if (BombDamage > 0) // bombalı top yok oluyorsa bomba gruptaki başka topa geçer
        {
            foreach (Ball b in All)
                if (b.SplitGroup == group && !b._dead && b != this) { PassBombTo(b); break; }
        }

        if (AliveInGroup(group) == 1)
        {
            foreach (Ball b in All)
                if (b.SplitGroup == group && !b._dead) b.SplitGroup = 0; // sona kalan gerçek top
        }

        Runner.Despawn(Object);
    }

    /// Gol sonrası top, tutulamaz halde şutu atanın önüne uçar ve onun kontrolüne geçer.
    void MoveBackToOwner()
    {
        KeeperController owner = KeeperController.FindBySlot(ReturnToSlot);
        if (owner == null)
        {
            ReturnToSlot = -1;
            EndChain();
            Velocity = RandomServeVelocity(serveSpeed);
            return;
        }

        PowerShotSkill powerShot = owner.GetSkill<PowerShotSkill>();
        float speed = powerShot != null ? powerShot.ReturnSpeed : 25f;

        Vector2 pos = transform.position;
        Vector2 target = HoldPosition(owner);
        Vector2 toTarget = target - pos;
        float step = speed * Runner.DeltaTime;

        if (toTarget.magnitude <= step)
        {
            transform.position = target;
            HolderSlot = ReturnToSlot;
            ReturnToSlot = -1;
            Velocity = Vector2.zero;
        }
        else
        {
            transform.position = pos + toTarget.normalized * step;
        }
    }

    void EndChain()
    {
        ChainOwnerSlot = -1;
        ChainReturnsLeft = 0;
    }

    void Release(Vector2 velocity)
    {
        LastThrowerSlot = HolderSlot;
        IgnoreThrower = TickTimer.CreateFromSeconds(Runner, rethrowIgnoreSeconds);
        HolderSlot = -1;
        Velocity = velocity;
        CurveRate = 0f;
    }

    Vector2 HoldPosition(KeeperController k)
    {
        ArenaLayout arena = ArenaLayout.Instance;
        return k.WorldPosition + arena.Inward(k.Slot) * (k.Radius + R + holdGap);
    }

    public static Vector2 RandomServeVelocity(float speed)
    {
        float a = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        return new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * speed;
    }

    public float ServeSpeed => serveSpeed;
    public float AimInfluence => aimInfluence;
    /// Ekranda çizilen yer (kendi tuttuğum topta tahmin edilen keeper'ın önü).
    public Vector2 VisualPosition => _visual != null ? (Vector2)_visual.position : (Vector2)transform.position;

    public static float DistancePointSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float len2 = ab.sqrMagnitude;
        if (len2 < 1e-8f) return Vector2.Distance(p, a);
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
        return Vector2.Distance(p, a + ab * t);
    }

    // ---------------- Görüntü ----------------

    /// Top bende ise gecikmesiz görünsün diye, görüntüyü kendi (tahmin edilen) keeper'ımın önüne koy.
    void LateUpdate()
    {
        if (_visual == null || Object == null || !Object.IsValid) return;

        KeeperController local = KeeperController.Local;
        if (IsHeld && local != null && HolderSlot == local.Slot && ArenaLayout.Instance != null)
        {
            ArenaLayout arena = ArenaLayout.Instance;
            Vector2 keeperDrawn = local.transform.position; // ekranda görünen (tahmin edilen) keeper
            _visual.position = keeperDrawn + arena.Inward(local.Slot) * (local.Radius + R + holdGap);
        }
        else
            _visual.localPosition = Vector3.zero;

        // Boyut (Top Boyutu yeteneği)
        float d = R * 2f;
        if (!Mathf.Approximately(_visual.localScale.x, d)) _visual.localScale = new Vector3(d, d, 1f);

        // Zincirli şutta top sahibinin renginde, geri dönerken yarı saydam
        Color c = ChainOwnerSlot >= 0 ? HexArena.SlotColors[ChainOwnerSlot] : Color.white;
        if (KamikazeDamage > 0) c = Color.Lerp(c, new Color(1f, 0.25f, 0.1f), 0.7f); // kamikaze: kırmızımsı
        if (ReturnToSlot >= 0) c.a = 0.5f;
        if (IsDecoy && LastThrowerSlot == NetworkBootstrap.LocalSlot) c.a *= 0.4f; // sahteyi sadece atan anlar

        // Görünmezlik: top görünmez ya da onu tutan keeper görünmezse rakipler görmez, sahibi soluk görür
        int hiddenOwner = InvisibleOwnerSlot;
        if (hiddenOwner < 0 && IsHeld)
        {
            KeeperController holder = KeeperController.FindBySlot(HolderSlot);
            if (holder != null && holder.IsHidden) hiddenOwner = HolderSlot;
        }
        bool visible = true;
        if (hiddenOwner >= 0)
        {
            if (hiddenOwner == NetworkBootstrap.LocalSlot) c.a = 0.35f;
            else visible = false;
        }

        if (_sprite.enabled != visible) _sprite.enabled = visible;
        if (_sprite.color != c) _sprite.color = c;

        UpdateBombText(visible);
    }

    // Sıcak Bomba sayacı (topun üstünde, kameraya dönük)
    TextMeshPro _bombText;
    int _shownBomb = -1;

    void UpdateBombText(bool ballVisible)
    {
        float left = BombDamage > 0 ? BombTimer.RemainingTime(Runner) ?? 0f : 0f;
        bool show = ballVisible && BombDamage > 0 && left > 0f;

        if (_bombText == null)
        {
            if (!show) return;
            var go = new GameObject("BombTimer");
            go.transform.SetParent(transform, false);
            _bombText = go.AddComponent<TextMeshPro>();
            _bombText.alignment = TextAlignmentOptions.Center;
            _bombText.fontSize = 5f;
            _bombText.fontStyle = FontStyles.Bold;
            _bombText.sortingOrder = 30;
            _bombText.rectTransform.sizeDelta = new Vector2(3f, 1.5f);
        }

        if (_bombText.gameObject.activeSelf != show) _bombText.gameObject.SetActive(show);
        if (!show) return;

        int seconds = Mathf.CeilToInt(left);
        if (seconds != _shownBomb)
        {
            _shownBomb = seconds;
            _bombText.text = seconds.ToString();
        }
        _bombText.color = left < 3f && Mathf.Repeat(left * 4f, 1f) < 0.5f ? Color.red : new Color(1f, 0.6f, 0.1f);

        // Topun üstünde dur, kameraya göre dik oku
        Camera cam = Camera.main;
        Quaternion rot = cam != null ? Quaternion.Euler(0f, 0f, cam.transform.eulerAngles.z) : Quaternion.identity;
        _bombText.transform.SetPositionAndRotation(
            (Vector2)_visual.position + (Vector2)(rot * Vector3.up) * (R + 0.45f), rot);
    }

    void CreateVisual()
    {
        var go = new GameObject("Visual");
        go.transform.SetParent(transform, false);
        go.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteUtil.Circle;
        sr.color = Color.white;
        sr.sortingOrder = 20;
        _visual = go.transform;
        _sprite = sr;
    }
}
