using System.Collections.Generic;
using Fusion;
using UnityEngine;

public enum MatchPhase : byte
{
    Waiting,   // oyuncu bekleniyor
    Countdown, // oda doldu, geri sayım
    Playing,
    Finished,
}

/// <summary>
/// Maçın durumunu yöneten tek ağ nesnesi. Kararları sadece host verir, herkes okur.
/// Oyuncular "slot" ile (0..5) ve kalıcı bir kimlik (OwnerId) ile takip edilir.
/// OwnerId sayesinde host değişince (Host Migration) herkes kendi keeper'ına geri bağlanır.
/// </summary>
public class MatchManager : NetworkBehaviour
{
    public static MatchManager Instance { get; private set; }

    [SerializeField] NetworkPrefabRef keeperPrefab;
    [SerializeField] NetworkPrefabRef ballPrefab;
    public NetworkPrefabRef BallPrefab => ballPrefab;
    [Tooltip("Test için 2 yap. Gerçek oyunda 6.")]
    [SerializeField, Range(1, HexArena.SlotCount)] int requiredPlayers = HexArena.SlotCount;
    [SerializeField] float countdownSeconds = 3f;
    [SerializeField] int startLives = 3;
    [Tooltip("Elenen oyuncu kaç saniye sonra oyundan atılır (mesajı görebilsin diye)")]
    [SerializeField] float kickDelay = 2f;
    [Tooltip("Host değişiminden sonra oyuncuların geri bağlanması için beklenecek süre")]
    [SerializeField] float reconnectGraceSeconds = 5f;
    [Tooltip("Host elenince oyundan çıksın ve hostluk başka oyuncuya geçsin")]
    [SerializeField] bool hostLeavesWhenEliminated = true;
    [Header("Altın")]
    [SerializeField] int startGold = 500;
    [Tooltip("Maç sırasında herkese saniyede eklenen altın (herkese eşit)")]
    [SerializeField] float goldPerSecond = 10f;

    [Tooltip("Maç bitince kaç saniye sonra menüye dönülür")]
    [SerializeField] float returnToMenuSeconds = 5f;

    [Networked] public MatchPhase Phase { get; set; }
    [Networked] public TickTimer Countdown { get; set; }
    [Networked] public int RequiredPlayers { get; set; }
    [Networked] public int WinnerSlot { get; set; }
    [Networked] public TickTimer ReconnectGrace { get; set; }
    [Networked, Capacity(HexArena.SlotCount)] public NetworkArray<PlayerRef> SlotOwners => default;   // şu anki bağlantı
    [Networked, Capacity(HexArena.SlotCount)] public NetworkArray<int> SlotOwnerIds => default;       // kalıcı kimlik (0 = boş)
    [Networked, Capacity(HexArena.SlotCount)] public NetworkArray<int> Lives => default;
    [Networked, Capacity(HexArena.SlotCount)] public NetworkArray<float> Gold => default;
    // Lanet: lanetli oyuncu bu aralıkla (sn) can kaybeder, 0 = lanet yok
    [Networked, Capacity(HexArena.SlotCount)] public NetworkArray<float> CurseInterval => default;
    [Networked, Capacity(HexArena.SlotCount)] public NetworkArray<TickTimer> CurseTimer => default;

    public bool IsCursed(int slot) => slot >= 0 && CurseInterval[slot] > 0f;

    /// Host (Arındır): laneti kaldırır.
    public void ServerRemoveCurse(int slot)
    {
        if (!HasStateAuthority || slot < 0) return;
        CurseInterval.Set(slot, 0f);
        CurseTimer.Set(slot, TickTimer.None);
    }

    // Sadece host'ta tutulan zamanlayıcılar (ağdan gitmez)
    readonly List<(PlayerRef player, float time)> _pendingKicks = new List<(PlayerRef, float)>();
    float _hostLeaveAt = -1f;
    float _finishedSeenAt = -1f;

    // Skor yazısını sadece değişince güncelle (her karede string üretme)
    readonly int[] _shownLives = new int[HexArena.SlotCount];
    static readonly string[] Numbers = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

    public override void Spawned()
    {
        Instance = this;
        for (int i = 0; i < _shownLives.Length; i++) _shownLives[i] = int.MinValue;

        if (!HasStateAuthority) return;

        if (NetworkBootstrap.IsResuming)
        {
            // Host değişti: durum eski host'un kaydından geldi. Bağlantılar yeniden kurulacak.
            for (int i = 0; i < HexArena.SlotCount; i++) SlotOwners.Set(i, PlayerRef.None);
            ReconnectGrace = TickTimer.CreateFromSeconds(Runner, reconnectGraceSeconds);
            if (Phase != MatchPhase.Waiting) Runner.SessionInfo.IsOpen = false;
        }
        else
        {
            RequiredPlayers = requiredPlayers;
            Phase = MatchPhase.Waiting;
            WinnerSlot = -1;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this) Instance = null;
    }

    // ---------------- Okuma yardımcıları ----------------

    public int PlayerCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < HexArena.SlotCount; i++)
                if (SlotOwnerIds[i] != 0) count++;
            return count;
        }
    }

    public bool IsAlive(int slot) => slot >= 0 && Lives[slot] > 0;

    public int GoldOf(int slot) => slot >= 0 ? Mathf.FloorToInt(Gold[slot]) : 0;

    /// Host: yeterli altın varsa düşer ve true döner.
    /// Host (Ruh Takası): iki oyuncunun canını değiştirir.
    public void ServerSwapLives(int a, int b)
    {
        if (!HasStateAuthority || !IsAlive(a) || !IsAlive(b)) return;
        int livesA = Lives[a];
        Lives.Set(a, Lives[b]);
        Lives.Set(b, livesA);
    }

    /// Host: oyuncuya altın ekler.
    public void ServerAddGold(int slot, int amount)
    {
        if (!HasStateAuthority || slot < 0 || amount <= 0 || !IsAlive(slot)) return;
        Gold.Set(slot, Gold[slot] + amount);
    }

    public bool ServerTrySpend(int slot, int amount)
    {
        if (!HasStateAuthority || slot < 0 || Gold[slot] < amount) return false;
        Gold.Set(slot, Gold[slot] - amount);
        return true;
    }

    public int CountdownRemaining => Mathf.CeilToInt(Countdown.RemainingTime(Runner) ?? 0f);

    // ---------------- Host: oyuncu giriş/çıkış ----------------

    public void ServerAddPlayer(PlayerRef player)
    {
        if (!HasStateAuthority) return;

        int id = NetworkBootstrap.GetPlayerId(Runner, player);

        // Geri dönen oyuncu (host değişimi sonrası): kendi keeper'ına yeniden bağla
        for (int s = 0; s < HexArena.SlotCount; s++)
        {
            if (SlotOwnerIds[s] != id) continue;
            KeeperController k = KeeperController.FindBySlot(s);
            if (k != null)
            {
                SlotOwners.Set(s, player);
                k.Object.AssignInputAuthority(player);
                Runner.SetPlayerObject(player, k.Object);
                return;
            }
        }

        int slot = FindFreeSlot();
        if (Phase != MatchPhase.Waiting || slot < 0)
        {
            Runner.Disconnect(player);
            return;
        }

        ArenaLayout arena = ArenaLayout.Instance;
        if (arena == null)
        {
            Debug.LogError("Sahnede ArenaLayout yok.");
            return;
        }

        SlotOwners.Set(slot, player);
        SlotOwnerIds.Set(slot, id);
        Lives.Set(slot, startLives);
        Gold.Set(slot, startGold);

        NetworkObject obj = Runner.Spawn(
            keeperPrefab,
            (Vector3)arena.SpawnPos(slot),
            Quaternion.Euler(0, 0, arena.ViewAngle(slot)),
            player,
            (runner, o) => o.GetComponent<KeeperController>().Init(slot));

        Runner.SetPlayerObject(player, obj);
    }

    public void ServerRemovePlayer(PlayerRef player)
    {
        if (!HasStateAuthority) return;

        int slot = SlotOf(player);
        if (slot < 0) return;

        if (Phase == MatchPhase.Playing)
            ServerEliminate(slot, kick: false); // maçın ortasında çıkan elenir
        else
            FreeSlot(slot);
    }

    // ---------------- Host: gol ve eleme ----------------

    /// Top bir kaleye çarptı (Ball çağırır).
    /// scorerSlot: topa en son vuran oyuncu (-1 = bilinmiyor). Can Çalma yeteneği buna göre can kazanır.
    /// kamikaze: Kamikaze Şut ise golü yiyen de atan da bu kadar can kaybeder (0 = normal gol).
    public void ServerGoal(int slot, int scorerSlot, int kamikaze = 0)
    {
        if (!HasStateAuthority || Phase != MatchPhase.Playing || !IsAlive(slot)) return;

        // Ateşkes: ateşkesli rakibine gol atan bozar, gol yemiş sayılır (golü yiyen can kaybetmez)
        if (CeasefireSkill.ServerTryBreak(slot, scorerSlot)) return;

        int damage = 1;
        if (scorerSlot >= 0 && scorerSlot != slot && IsAlive(scorerSlot))
        {
            KeeperController scorer = KeeperController.FindBySlot(scorerSlot);

            // Can Çalma: gol atan (kendi kalesine değilse) can kazanır
            LifestealSkill lifesteal = scorer != null ? scorer.GetSkill<LifestealSkill>() : null;
            if (lifesteal != null) ServerHeal(scorerSlot, lifesteal.LivesPerGoal);

            // Ödül Avcısı: ödüllü oyuncuya gol atan ödül kazanır
            BountySkill.ServerOnGoal(slot, scorerSlot);

            // Altın Top: gol fazladan can götürür
            GoldenBallSkill golden = scorer != null ? scorer.GetSkill<GoldenBallSkill>() : null;
            if (golden != null) damage = golden.ServerDamageForGoal();
        }

        bool isKamikaze = kamikaze > 0 && scorerSlot >= 0 && scorerSlot != slot;
        if (isKamikaze) damage = Mathf.Max(damage, kamikaze);

        ServerDamage(slot, damage, scorerSlot);
        if (isKamikaze) ServerDamage(scorerSlot, kamikaze); // Kamikaze: atan da aynı canı kaybeder
    }

    /// Host: oyuncu 'amount' can kaybeder; 0'a inerse elenir.
    /// source: canı alan oyuncu (Lanet için; -1 = bilinmiyor / kendisi).
    public void ServerDamage(int slot, int amount = 1, int source = -1)
    {
        if (!HasStateAuthority || Phase != MatchPhase.Playing || !IsAlive(slot) || amount <= 0) return;

        int lives = Lives[slot] - amount;
        if (lives < 1) // İkinci Şans: kalkan hazırsa can 1'de kalır
        {
            KeeperController k = KeeperController.FindBySlot(slot);
            SecondChanceSkill sc = k != null ? k.GetSkill<SecondChanceSkill>() : null;
            if (sc != null && sc.ServerTrySave(out int bonus)) lives = 1 + bonus;
        }
        lives = Mathf.Max(0, lives);
        Lives.Set(slot, lives);
        if (lives <= 0)
        {
            TryApplyCurse(slot, source); // keeper yok olmadan önce (Lanet yeteneği onun üstünde)
            ServerEliminate(slot, kick: true);
        }
    }

    /// Lanet: elenen oyuncunun Lanet yeteneği varsa onu eleyen lanetlenir.
    void TryApplyCurse(int victim, int source)
    {
        if (source < 0 || source == victim || !IsAlive(source)) return;

        KeeperController k = KeeperController.FindBySlot(victim);
        CurseSkill curse = k != null ? k.GetSkill<CurseSkill>() : null;
        float interval = curse != null ? curse.Interval : 0f;
        if (interval <= 0f) return;

        float current = CurseInterval[source];
        if (current > 0f && current <= interval) return; // zaten daha sık lanetli
        CurseInterval.Set(source, interval);
        CurseTimer.Set(source, TickTimer.CreateFromSeconds(Runner, interval));
    }

    /// Host: lanetliler zamanı gelince 1 can kaybeder; can 1 kalınca lanet biter.
    void TickCurses()
    {
        for (int s = 0; s < HexArena.SlotCount; s++)
        {
            float interval = CurseInterval[s];
            if (interval <= 0f) continue;

            if (!IsAlive(s) || Lives[s] <= 1) { ServerRemoveCurse(s); continue; }
            if (!CurseTimer[s].Expired(Runner)) continue;

            Lives.Set(s, Lives[s] - 1);
            if (Lives[s] <= 1) ServerRemoveCurse(s);
            else CurseTimer.Set(s, TickTimer.CreateFromSeconds(Runner, interval));
        }
    }

    /// Host: yaşayan oyuncuya can ekler.
    public void ServerHeal(int slot, int amount)
    {
        if (!HasStateAuthority || amount <= 0 || !IsAlive(slot)) return;
        Lives.Set(slot, Lives[slot] + amount);
    }

    void ServerEliminate(int slot, bool kick)
    {
        Lives.Set(slot, 0);

        KeeperController k = KeeperController.FindBySlot(slot);
        if (k != null) Runner.Despawn(k.Object);

        PlayerRef player = SlotOwners[slot];
        SlotOwners.Set(slot, PlayerRef.None);
        RPC_Eliminated(slot);

        if (kick && player != PlayerRef.None)
        {
            if (player == Runner.LocalPlayer)
            {
                if (hostLeavesWhenEliminated) _hostLeaveAt = Time.time + kickDelay;
            }
            else
            {
                _pendingKicks.Add((player, Time.time + kickDelay));
            }
        }

        CheckWinner();
    }

    void CheckWinner()
    {
        int alive = 0, last = -1;
        for (int s = 0; s < HexArena.SlotCount; s++)
            if (IsAlive(s)) { alive++; last = s; }

        if (alive > 1) return;

        Phase = MatchPhase.Finished; // toplar bir sonraki tick'te kaldırılır
        WinnerSlot = last;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_Eliminated(int slot)
    {
        if (slot == NetworkBootstrap.LocalSlot)
            NetworkBootstrap.LastMessage = "Elendin!";
    }

    // ---------------- Host: akış ----------------

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (ReconnectGrace.Expired(Runner))
        {
            ReconnectGrace = TickTimer.None;
            DropPlayersWhoDidNotReturn();
        }

        switch (Phase)
        {
            case MatchPhase.Waiting:
                if (PlayerCount >= RequiredPlayers)
                {
                    Phase = MatchPhase.Countdown;
                    Countdown = TickTimer.CreateFromSeconds(Runner, countdownSeconds);
                    Runner.SessionInfo.IsOpen = false; // yeni oyuncu alma
                }
                break;

            case MatchPhase.Countdown:
                if (PlayerCount < RequiredPlayers) // geri sayımda biri çıktı
                {
                    Phase = MatchPhase.Waiting;
                    Countdown = TickTimer.None;
                    Runner.SessionInfo.IsOpen = true;
                }
                else if (Countdown.Expired(Runner))
                {
                    for (int s = 0; s < HexArena.SlotCount; s++)
                    {
                        bool occupied = SlotOwnerIds[s] != 0;
                        Lives.Set(s, occupied ? startLives : 0);
                        Gold.Set(s, occupied ? startGold : 0);
                    }
                    Phase = MatchPhase.Playing;
                    SpawnBall();
                }
                break;

            case MatchPhase.Playing:
                // Altın herkese aynı hızda artar
                TickCurses(); // Lanet

                float add = goldPerSecond * Runner.DeltaTime;
                for (int s = 0; s < HexArena.SlotCount; s++)
                    if (IsAlive(s)) Gold.Set(s, Gold[s] + add);
                break;

            case MatchPhase.Finished:
                for (int i = Ball.All.Count - 1; i >= 0; i--)
                    Runner.Despawn(Ball.All[i].Object);
                break;
        }
    }

    void DropPlayersWhoDidNotReturn()
    {
        for (int s = 0; s < HexArena.SlotCount; s++)
        {
            if (SlotOwnerIds[s] == 0 || SlotOwners[s] != PlayerRef.None) continue;

            if (Phase == MatchPhase.Playing)
            {
                if (IsAlive(s)) ServerEliminate(s, kick: false);
            }
            else
            {
                FreeSlot(s);
            }
        }
    }

    /// Zamanlayıcılar gerçek zamanla çalışır (oyun mantığı değil), bu yüzden Update'te.
    void Update()
    {
        if (Object == null || !Object.IsValid) return;

        // Maç bitti: sonuç ekranı, sonra menüye dön (önce client'lar, en son host)
        if (Phase == MatchPhase.Finished)
        {
            if (_finishedSeenAt < 0f)
            {
                _finishedSeenAt = Time.time;
                NetworkBootstrap.LastMessage = WinnerSlot < 0 ? "Berabere!"
                    : WinnerSlot == NetworkBootstrap.LocalSlot ? "Kazandın!"
                    : "Kazanan: " + HexArena.SlotNames[WinnerSlot];
            }
            float wait = returnToMenuSeconds + (HasStateAuthority ? 2f : 0f);
            if (Time.time - _finishedSeenAt > wait) NetworkBootstrap.Instance.Leave();
            return;
        }

        if (!HasStateAuthority) return;

        for (int i = _pendingKicks.Count - 1; i >= 0; i--)
        {
            if (Time.time < _pendingKicks[i].time) continue;
            Runner.Disconnect(_pendingKicks[i].player);
            _pendingKicks.RemoveAt(i);
        }

        if (_hostLeaveAt > 0f && Time.time >= _hostLeaveAt)
        {
            _hostLeaveAt = -1f;
            NetworkBootstrap.Instance.Leave(); // hostluk başka oyuncuya geçer
        }
    }

    // ---------------- Görüntü (herkeste) ----------------

    public override void Render()
    {
        ArenaLayout arena = ArenaLayout.Instance;
        if (arena == null) return;

        for (int s = 0; s < HexArena.SlotCount; s++)
        {
            int value = SlotOwnerIds[s] == 0 ? -1 : Lives[s];
            if (value == _shownLives[s]) continue;
            _shownLives[s] = value;
            arena.SetScoreText(s, value < 0 ? "" : value < 10 ? Numbers[value] : value.ToString());
        }
    }

    // ---------------- Yardımcılar ----------------

    void SpawnBall()
    {
        if (Ball.All.Count > 0) return; // zaten top var (ör. host değişiminden geldi)

        ArenaLayout arena = ArenaLayout.Instance;
        Runner.Spawn(
            ballPrefab,
            (Vector3)arena.Center,
            Quaternion.identity,
            null,
            (runner, o) =>
            {
                Ball ball = o.GetComponent<Ball>();
                ball.Init(Ball.RandomServeVelocity(ball.ServeSpeed)); // merkezden rastgele yöne
            });
    }

    void FreeSlot(int slot)
    {
        KeeperController k = KeeperController.FindBySlot(slot);
        if (k != null) Runner.Despawn(k.Object);
        SlotOwners.Set(slot, PlayerRef.None);
        SlotOwnerIds.Set(slot, 0);
        Lives.Set(slot, 0);
        Gold.Set(slot, 0);
    }

    int SlotOf(PlayerRef player)
    {
        for (int s = 0; s < HexArena.SlotCount; s++)
            if (SlotOwners[s] == player) return s;
        return -1;
    }

    int FindFreeSlot()
    {
        for (int s = 0; s < HexArena.SlotCount; s++)
            if (SlotOwnerIds[s] == 0) return s;
        return -1;
    }
}
