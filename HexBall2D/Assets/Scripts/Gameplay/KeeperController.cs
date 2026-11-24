using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Oyuncunun kalecisi (daire). Kafesinde hareket eder, duvarlara çarpınca durup kayar.
/// Kendi keeper'ın tahminle (prediction) anında hareket eder; host sonucu doğrular.
/// Prefab: NetworkObject + NetworkTransform + bu script. Collider GEREKMEZ.
/// </summary>
public class KeeperController : NetworkBehaviour
{
    [SerializeField] float moveSpeed = 7f;
    [SerializeField] float radius = 0.45f;

    const float Skin = 0.01f; // duvara yapışmayı önleyen küçük boşluk

    public static readonly List<KeeperController> All = new List<KeeperController>();
    public static KeeperController Local { get; private set; } // bu bilgisayardaki oyuncunun keeper'ı

    [Networked] public int Slot { get; set; }
    [Networked] public Vector2 LocalPos { get; set; } // ArenaLayout yerel koordinatı
    [Networked] public NetworkButtons PrevButtons { get; set; }
    [Networked] public TickTimer StunTimer { get; set; } // Donduran Disk: süre bitene kadar hareket edemez
    [Networked] public int DeckCode { get; set; }        // oyuncunun destesi (PlayerDeck kodu, 0 = henüz gelmedi)
    [Networked] public TickTimer ReverseTimer { get; set; } // Ters Kontrol: süre bitene kadar kontroller ters
    [Networked] public TickTimer FlipTimer { get; set; }    // Ters Dünya: süre bitene kadar kamera baş aşağı
    [Networked] public TickTimer SilenceTimer { get; set; } // Susturma: süre bitene kadar yetenek kullanamaz
    [Networked] public int PuppetMasterSlot { get; set; }   // Kukla Ustası: beni kontrol eden (-1 = yok)
    [Networked] public TickTimer PuppetTimer { get; set; }
    [Networked] public NetworkButtons PuppetPrevButtons { get; set; } // ustanın önceki tuşları (kuklanın şutu için)

    /// Destenin i. slotundaki yetenek (-1 = boş ya da deste henüz gelmedi).
    public int DeckSkillAt(int index) => (DeckCode & PlayerDeck.SetFlag) == 0 ? -1 : PlayerDeck.SlotOf(DeckCode, index);

    public bool HasInDeck(SkillId id) => DeckIndexOf(id) >= 0;

    /// Yeteneğin destedeki sırası (-1 = destede yok). Yetenek butonlarının ağ bitleri bu sıraya göredir.
    public int DeckIndexOf(SkillId id)
    {
        for (int i = 0; i < PlayerDeck.Size; i++)
            if (DeckSkillAt(i) == (int)id) return i;
        return -1;
    }

    /// Client kendi destesini host'a bildirir (maç başında bir kez).
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetDeck(int code)
    {
        if (DeckCode != 0 || !PlayerDeck.IsValidCode(code)) return; // sadece bir kez, geçerliyse
        DeckCode = code;
    }

    public bool IsStunned => Runner != null && !StunTimer.ExpiredOrNotRunning(Runner);

    /// Host: keeper'ı dondurur (zaten donuksa daha uzun olan süre geçerli).
    public void ServerStun(float seconds)
    {
        if (!HasStateAuthority) return;
        float left = StunTimer.RemainingTime(Runner) ?? 0f;
        if (seconds > left) StunTimer = TickTimer.CreateFromSeconds(Runner, seconds);
    }

    public bool IsReversed => Runner != null && !ReverseTimer.ExpiredOrNotRunning(Runner);
    public bool IsFlipped => Runner != null && !FlipTimer.ExpiredOrNotRunning(Runner);

    /// Host (Ters Dünya): kamerayı baş aşağı çevirir (zaten tersse daha uzun olan süre geçerli).
    public void ServerFlipCamera(float seconds)
    {
        if (!HasStateAuthority) return;
        float left = FlipTimer.RemainingTime(Runner) ?? 0f;
        if (seconds > left) FlipTimer = TickTimer.CreateFromSeconds(Runner, seconds);
    }

    /// Host: kontrolleri ters çevirir (zaten tersse daha uzun olan süre geçerli).
    public void ServerReverse(float seconds)
    {
        if (!HasStateAuthority) return;
        float left = ReverseTimer.RemainingTime(Runner) ?? 0f;
        if (seconds > left) ReverseTimer = TickTimer.CreateFromSeconds(Runner, seconds);
    }

    public bool IsSilenced => Runner != null && !SilenceTimer.ExpiredOrNotRunning(Runner);
    public float SilenceRemaining => Runner != null ? SilenceTimer.RemainingTime(Runner) ?? 0f : 0f;

    /// Host: keeper'ı susturur (zaten susturulmuşsa daha uzun olan süre geçerli).
    public void ServerSilence(float seconds)
    {
        if (!HasStateAuthority) return;
        if (seconds > SilenceRemaining) SilenceTimer = TickTimer.CreateFromSeconds(Runner, seconds);
    }

    /// Host (Ödül Avcısı): tüm yeteneklerin bekleme süresini kısaltır.
    public void ServerReduceCooldowns(float seconds)
    {
        foreach (SkillBase s in _skills) s.ServerReduceCooldowns(seconds);
    }

    /// Host (Arındır): tüm kötü etkileri kaldırır.
    public void ServerCleanse()
    {
        if (!HasStateAuthority) return;
        StunTimer = TickTimer.None;
        SilenceTimer = TickTimer.None;
        ReverseTimer = TickTimer.None;
        FlipTimer = TickTimer.None;
        PuppetMasterSlot = -1; // Kukla Ustası
        if (MatchManager.Instance != null) MatchManager.Instance.ServerRemoveCurse(Slot); // Lanet
        RPC_ClearPaint();
    }

    /// Boya sadece oyuncunun kendi ekranında; onu oyuncunun kendisi siler.
    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    void RPC_ClearPaint() => PaintOverlay.ClearAll();

    public float Radius => radius;

    /// Görünmez Şut 3. seviye: rakiplere görünmez.
    public bool IsHidden
    {
        get
        {
            InvisibleShotSkill s = GetSkill<InvisibleShotSkill>();
            return s != null && s.KeeperHidden;
        }
    }

    readonly List<SpriteRenderer> _sprites = new List<SpriteRenderer>();
    readonly List<Color> _spriteColors = new List<Color>();
    int _shownVisualState;
    static readonly Color IceColor = new Color(0.6f, 0.85f, 1f);
    static readonly Color ReverseColor = new Color(0.65f, 0.2f, 0.9f); // Ters Kontrol: mor
    static readonly Color SilenceColor = new Color(0.45f, 0.45f, 0.45f); // Susturma: gri
    static readonly Color CurseColor = new Color(0.25f, 0.05f, 0.3f);   // Lanet: koyu mor
    public Vector2 WorldPosition => ArenaLayout.Instance.LocalToWorld(Slot, LocalPos);

    SkillBase[] _skills = new SkillBase[0];

    /// Bu keeper'daki yetenek (yoksa null).
    public SkillBase GetSkill(SkillId id)
    {
        foreach (SkillBase s in _skills)
            if (s.Id == id) return s;
        return null;
    }

    public T GetSkill<T>() where T : SkillBase
    {
        foreach (SkillBase s in _skills)
            if (s is T t) return t;
        return null;
    }

    /// Client dükkandan satın almak ister; kararı host verir.
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_BuySkill(int skillId)
    {
        SkillBase skill = GetSkill((SkillId)skillId);
        if (skill != null) skill.ServerTryBuy();
    }

    public static KeeperController FindBySlot(int slot)
    {
        foreach (KeeperController k in All)
            if (k.Slot == slot) return k;
        return null;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        All.Remove(this);
        if (Local == this) Local = null;
    }

    /// Host, spawn'dan hemen önce çağırır.
    public void Init(int slot)
    {
        Slot = slot;
        LocalPos = Vector2.zero; // spawn noktasının tam üstü
    }

    public override void Spawned()
    {
        All.Add(this);
        _skills = GetComponents<SkillBase>();
        if (HasStateAuthority && !IsPuppeted) PuppetMasterSlot = -1;
        ApplyTransform();
        AddCircle("Body", radius * 2f, HexArena.SlotColors[Slot], 10);
        if (HasInputAuthority) BecomeLocal();
    }

    /// Host değişiminden sonra kontrol sonradan verilebilir; o yüzden her karede kontrol edilir.
    public override void Render()
    {
        if (HasInputAuthority && Local != this) BecomeLocal();
        if (Local == this) UpdateCameraFlip();

        // Görünmezlik: rakipler hiç görmez, sahibi yarı saydam görür. Donmuşken buz mavisi.
        bool hidden = IsHidden;
        bool stunned = IsStunned;
        bool reversed = IsReversed;
        bool silenced = IsSilenced;
        bool cursed = MatchManager.Instance != null && MatchManager.Instance.IsCursed(Slot);
        bool puppet = IsPuppeted;
        int visualState = (hidden ? 1 : 0) | (stunned ? 2 : 0) | (reversed ? 4 : 0) | (silenced ? 8 : 0) | (cursed ? 16 : 0)
                        | (puppet ? 32 + PuppetMasterSlot * 64 : 0);
        if (visualState != _shownVisualState)
        {
            _shownVisualState = visualState;
            for (int i = 0; i < _sprites.Count; i++)
            {
                Color c = _spriteColors[i];
                if (puppet) c = Color.Lerp(c, HexArena.SlotColors[PuppetMasterSlot], 0.55f); // Kukla: ustanın rengine bürünür
                if (cursed) c = Color.Lerp(c, CurseColor, 0.5f);
                if (silenced) c = Color.Lerp(c, SilenceColor, 0.6f);
                if (reversed) c = Color.Lerp(c, ReverseColor, 0.6f);
                if (stunned) c = Color.Lerp(c, IceColor, 0.65f);
                if (hidden) c.a *= 0.35f;
                _sprites[i].color = c;
                _sprites[i].enabled = !hidden || HasInputAuthority;
            }
        }
    }

    const float CameraFlipSpeed = 540f; // derece/sn: ters dönme animasyonu (~0.33 sn)

    /// Ters Dünya: kendi kameramı yumuşakça 180° çevirir / geri döndürür.
    void UpdateCameraFlip()
    {
        ArenaLayout arena = ArenaLayout.Instance;
        Camera cam = Camera.main;
        if (arena == null || cam == null) return;

        float target = arena.ViewAngle(Slot) + (IsFlipped ? 180f : 0f);
        float current = cam.transform.eulerAngles.z;
        if (Mathf.Abs(Mathf.DeltaAngle(current, target)) < 0.01f) return;

        float z = Mathf.MoveTowardsAngle(current, target, CameraFlipSpeed * Time.deltaTime);
        cam.transform.rotation = Quaternion.Euler(0f, 0f, z);
    }

    void BecomeLocal()
    {
        Local = this;
        NetworkBootstrap.LocalSlot = Slot;
        if (DeckCode == 0) RPC_SetDeck(PlayerDeck.Encode()); // destemi host'a gönder
        AddCircle("Outline", radius * 2f + 0.14f, Color.white, 9); // kendini tanıman için beyaz halka

        // Herkes kendi kalesini ekranın altında görür.
        ArenaLayout arena = ArenaLayout.Instance;
        Camera cam = Camera.main;
        if (arena != null && cam != null)
        {
            cam.transform.SetPositionAndRotation(
                new Vector3(arena.Center.x, arena.Center.y, cam.transform.position.z),
                Quaternion.Euler(0, 0, arena.ViewAngle(Slot)));
        }
    }

    public override void FixedUpdateNetwork()
    {
        MatchManager mm = MatchManager.Instance;
        ArenaLayout arena = ArenaLayout.Instance;
        if (mm == null || arena == null || mm.Phase != MatchPhase.Playing) return;

        // Kukla Ustası: süresi dolunca kukla serbest kalır
        if (HasStateAuthority && PuppetMasterSlot >= 0 && !IsPuppeted) PuppetMasterSlot = -1;

        bool puppeted = IsPuppeted;
        if (puppeted && HasStateAuthority) DriveAsPuppet(arena); // host: ustanın girdisiyle hareket et

        if (GetInput(out NetworkInputData input))
        {
            Vector2 move = Vector2.ClampMagnitude(input.Move, 1f);
            Vector2 throwAim = input.ThrowAim;
            if (IsReversed) // Ters Kontrol: sağ-sol ve ileri-geri yer değiştirir, nişanın sağ-solu da
            {
                move = -move;
                throwAim.x = -throwAim.x;
            }

            // Kukla iken kendi hareketim/şutum çalışmaz; kukla oynatırken de kendi keeper'ım durur
            bool ownBodyFree = !puppeted && !IsPuppeteering;
            if (ownBodyFree)
            {
                MoveBy(move, arena);

                // Space: topu tutuyorsan fırlat (karar host'ta)
                if (input.Buttons.WasPressed(PrevButtons, InputButton.Throw) && HasStateAuthority)
                    ThrowHeldBall(move.x, throwAim);
            }

            // Yetenek butonları (dükkandaki "kullan" butonları). Ağ biti = destedeki sıra.
            // Susturulmuşken ya da kuklayken sadece "susturmada da çalışan" yetenekler (Arındır) kullanılabilir.
            bool blocked = IsSilenced || puppeted;
            for (int deckIndex = 0; deckIndex < PlayerDeck.Size; deckIndex++)
            {
                int id = DeckSkillAt(deckIndex);
                SkillBase skill = id >= 0 ? GetSkill((SkillId)id) : null;
                if (skill == null) continue;
                if (blocked && !skill.UsableWhileSilenced) continue;
                if (!HasStateAuthority && !skill.PredictOnClient) continue; // çoğu yeteneğe sadece host karar verir
                for (int action = 0; action < SkillBase.MaxActions; action++)
                {
                    int button = SkillBase.InputButtonFor(deckIndex, action);
                    if (skill.HasAction(action) && input.Buttons.WasPressed(PrevButtons, button))
                        skill.UseAction(action, move.x);
                }
            }

            PrevButtons = input.Buttons;
        }

        ApplyTransform();
    }

    /// Keeper'ı yerel koordinatta 'move' yönünde (−1..1) bir tick ilerletir.
    void MoveBy(Vector2 move, ArenaLayout arena)
    {
        SpeedSkill speedSkill = GetSkill<SpeedSkill>();
        float speed = moveSpeed * (speedSkill != null ? speedSkill.SpeedMultiplier : 1f); // Hızlı Hareket
        if (IsStunned) speed = 0f; // donmuş keeper hareket edemez
        Vector2 targetLocal = arena.ClampToCage(LocalPos + move * speed * Runner.DeltaTime);

        Vector2 from = arena.LocalToWorld(Slot, LocalPos);
        Vector2 to = arena.LocalToWorld(Slot, targetLocal);
        Vector2 end = MoveWithWalls(from, to - from, arena.WallMask);

        LocalPos = arena.ClampToCage(arena.WorldToLocal(Slot, end));
    }

    void ThrowHeldBall(float aimX, Vector2 throwAim)
    {
        foreach (Ball ball in Ball.All)
        {
            if (ball.HolderSlot == Slot)
            {
                ball.ServerThrow(this, aimX, throwAim);
                break;
            }
        }
    }

    // ---------------- Kukla Ustası ----------------

    public bool IsPuppeted => PuppetMasterSlot >= 0 && Runner != null && !PuppetTimer.ExpiredOrNotRunning(Runner);

    /// Ben şu an başka bir keeper'ı kukla olarak oynatıyor muyum?
    public bool IsPuppeteering
    {
        get
        {
            foreach (KeeperController k in All)
                if (k != this && k.PuppetMasterSlot == Slot && k.IsPuppeted) return true;
            return false;
        }
    }

    /// Host: bu keeper 'seconds' boyunca 'master'ın kontrolüne geçer.
    public void ServerMakePuppet(KeeperController master, float seconds)
    {
        if (!HasStateAuthority || master == null || master == this) return;
        PuppetMasterSlot = master.Slot;
        PuppetTimer = TickTimer.CreateFromSeconds(Runner, seconds);
        PuppetPrevButtons = master.PrevButtons; // ustanın basılı tuşu hemen şut saymasın
    }

    /// Host: ustanın girdisini okuyup kuklayı onun ekranına göre hareket ettirir, şut attırır.
    void DriveAsPuppet(ArenaLayout arena)
    {
        KeeperController master = FindBySlot(PuppetMasterSlot);
        if (master == null || master.Object == null) { PuppetMasterSlot = -1; return; }
        if (!Runner.TryGetInputForPlayer(master.Object.InputAuthority, out NetworkInputData mi)) return;

        // Ustanın ekranındaki yön → dünya yönü → kuklanın yerel yönü
        Vector2 mMove = Vector2.ClampMagnitude(mi.Move, 1f);
        Vector2 world = arena.Right(master.Slot) * mMove.x + arena.Inward(master.Slot) * mMove.y;
        Vector2 local = new Vector2(Vector2.Dot(world, arena.Right(Slot)), Vector2.Dot(world, arena.Inward(Slot)));
        MoveBy(local, arena);

        if (mi.Buttons.WasPressed(PuppetPrevButtons, InputButton.Throw))
        {
            Vector2 aim = Vector2.zero;
            if (mi.ThrowAim.y > 0.01f)
            {
                Vector2 aimWorld = arena.Right(master.Slot) * mi.ThrowAim.x + arena.Inward(master.Slot) * mi.ThrowAim.y;
                aim = new Vector2(Vector2.Dot(aimWorld, arena.Right(Slot)), Vector2.Dot(aimWorld, arena.Inward(Slot)));
            }
            ThrowHeldBall(local.x, aim); // geriye doğru nişan olursa düz ileri atar
        }
        PuppetPrevButtons = mi.Buttons;
    }

    /// Daireyi duvarlara çarpana kadar ilerletir, çarparsa duvar boyunca kaydırır.
    Vector2 MoveWithWalls(Vector2 pos, Vector2 delta, LayerMask mask)
    {
        for (int i = 0; i < 3; i++)
        {
            float dist = delta.magnitude;
            if (dist < 0.0001f) break;

            Vector2 dir = delta / dist;
            RaycastHit2D hit = Physics2D.CircleCast(pos, radius, dir, dist + Skin, mask);
            if (!hit || hit.fraction <= 0f) // çarpma yok ya da zaten içinde başladı (sıkışmayı önle)
            {
                pos += delta;
                break;
            }

            float allowed = Mathf.Max(0f, hit.distance - Skin);
            pos += dir * allowed;

            Vector2 remaining = delta - dir * allowed;
            delta = remaining - Vector2.Dot(remaining, hit.normal) * hit.normal; // duvara paralel kısım
        }
        return pos;
    }

    /// Işınlanma: kafesin en soluna (-1) ya da en sağına (+1), aynı derinlikte.
    public void TeleportToEdge(int side)
    {
        ArenaLayout arena = ArenaLayout.Instance;
        if (arena == null || IsStunned) return; // donmuşken ışınlanamaz
        LocalPos = arena.ClampToCage(new Vector2(side * arena.CageHalfWidth, LocalPos.y));
        ApplyTransform();
    }

    void ApplyTransform()
    {
        ArenaLayout arena = ArenaLayout.Instance;
        if (arena == null) return;
        transform.SetPositionAndRotation(
            arena.LocalToWorld(Slot, LocalPos),
            Quaternion.Euler(0, 0, arena.ViewAngle(Slot)));
    }

    void AddCircle(string name, float diameter, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localScale = new Vector3(diameter, diameter, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteUtil.Circle;
        sr.color = color;
        sr.sortingOrder = order;
        _sprites.Add(sr);
        _spriteColors.Add(color);
    }
}
