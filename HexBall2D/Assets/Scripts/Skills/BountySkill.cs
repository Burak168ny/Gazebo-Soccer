using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// ÖDÜL AVCISI (butonla)
/// Basınca senin dışındaki oyunculardan CANI EN FAZLA olanın başına ödül konur (eşitlikte rastgele biri).
/// Ödüllü oyuncunun kalesine gol atan HER oyuncu ödül kazanır:
/// Seviye 1: 20 altın. Bekleme 120 sn.
/// Seviye 2: 50 altın + tüm yeteneklerinin bekleme süresi 5 sn azalır. Bekleme 90 sn.
/// Seviye 3: 100 altın + bekleme süreleri 10 sn azalır. Bekleme 60 sn.
/// Ödül, o oyuncu birinciliği kaptırana (başka biri daha fazla cana çıkana) ya da elenene kadar sürer.
/// Ödüllü oyuncunun etrafında altın bir halka görünür. Bir anda sadece bir ödül koyabilirsin.
/// </summary>
public class BountySkill : SkillBase
{
    [Tooltip("1., 2. ve 3. seviyede ödüllü oyuncuya atılan her golde kazanılan altın")]
    [SerializeField] int[] goldRewards = { 20, 50, 100 };
    [Tooltip("1., 2. ve 3. seviyede ödüllü oyuncuya atılan her golde yeteneklerin bekleme süresinden düşülen saniye")]
    [SerializeField] float[] cooldownRewards = { 0f, 5f, 10f };
    [Tooltip("1., 2. ve 3. seviyede bekleme süresi (saniye)")]
    [SerializeField] float[] cooldowns = { 120f, 90f, 60f };

    [Networked] public TickTimer Cooldown { get; set; }
    [Networked] public int TargetSlot { get; set; } // ödüllü oyuncu (-1 = yok)

    public override SkillId Id => SkillId.Bounty;
    public override bool HasActiveAbility => Level >= 1;
    public override int ActionButtonCount => 1;
    public override float CooldownRemaining => Runner != null ? Cooldown.RemainingTime(Runner) ?? 0f : 0f;
    public override bool CanActivateNow => TargetSlot < 0; // ödül sürerken yenisi konmaz

    int LevelIndex => Mathf.Clamp(Level - 1, 0, 2);

    static readonly List<int> _best = new List<int>();

    public override void Spawned()
    {
        if (HasStateAuthority) TargetSlot = -1;
        CreateMarker();
    }

    public override void ServerActivate(float aimX)
    {
        if (!HasStateAuthority || !HasActiveAbility || TargetSlot >= 0 || !Cooldown.ExpiredOrNotRunning(Runner)) return;

        MatchManager mm = MatchManager.Instance;
        if (mm == null || mm.Phase != MatchPhase.Playing) return;

        // Benim dışımda canı en fazla olan(lar)
        _best.Clear();
        int bestLives = 0;
        for (int s = 0; s < HexArena.SlotCount; s++)
        {
            if (s == Keeper.Slot || !mm.IsAlive(s)) continue;
            int lives = mm.Lives[s];
            if (lives > bestLives) { bestLives = lives; _best.Clear(); }
            if (lives == bestLives) _best.Add(s);
        }
        if (_best.Count == 0) return;

        TargetSlot = _best[Random.Range(0, _best.Count)];
        Cooldown = TickTimer.CreateFromSeconds(Runner, cooldowns[Mathf.Min(LevelIndex, cooldowns.Length - 1)]);
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || TargetSlot < 0) return;

        MatchManager mm = MatchManager.Instance;
        if (mm == null) return;
        if (!mm.IsAlive(TargetSlot)) { TargetSlot = -1; return; }

        int targetLives = mm.Lives[TargetSlot];
        for (int s = 0; s < HexArena.SlotCount; s++)
        {
            if (s != TargetSlot && mm.IsAlive(s) && mm.Lives[s] > targetLives)
            {
                TargetSlot = -1; // birinciliği kaptırdı: ödül biter
                return;
            }
        }
    }

    /// Host (MatchManager.ServerGoal çağırır): 'goalSlot'a 'scorerSlot' gol attı. Ödül varsa verilir.
    public static void ServerOnGoal(int goalSlot, int scorerSlot)
    {
        MatchManager mm = MatchManager.Instance;
        if (mm == null) return;

        foreach (KeeperController k in KeeperController.All)
        {
            BountySkill b = k.GetSkill<BountySkill>();
            if (b == null || b.Level <= 0 || b.TargetSlot != goalSlot) continue;

            int i = b.LevelIndex;
            mm.ServerAddGold(scorerSlot, b.goldRewards[Mathf.Min(i, b.goldRewards.Length - 1)]);

            float cut = b.cooldownRewards[Mathf.Min(i, b.cooldownRewards.Length - 1)];
            KeeperController scorer = KeeperController.FindBySlot(scorerSlot);
            if (cut > 0f && scorer != null) scorer.ServerReduceCooldowns(cut);
        }
    }

    // ---------------- Görüntü: ödüllü oyuncunun etrafında altın halka ----------------

    static readonly Color BountyColor = new Color(1f, 0.82f, 0.1f, 0.9f);
    SpriteRenderer _marker;

    void CreateMarker()
    {
        var go = new GameObject("BountyMarker"); // keeper'a bağlı değil, hedefi takip eder
        _marker = go.AddComponent<SpriteRenderer>();
        _marker.sprite = SpriteUtil.Circle;
        _marker.color = BountyColor;
        _marker.sortingOrder = 8; // keeper'ın arkasında
        go.SetActive(false);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_marker != null) Destroy(_marker.gameObject);
    }

    public override void Render()
    {
        if (_marker == null) return;

        KeeperController target = TargetSlot >= 0 ? KeeperController.FindBySlot(TargetSlot) : null;
        bool show = target != null;
        if (_marker.gameObject.activeSelf != show) _marker.gameObject.SetActive(show);
        if (!show) return;

        _marker.transform.position = target.transform.position;
        float d = target.Radius * 2f + 0.45f + Mathf.Sin(Time.time * 6f) * 0.06f; // hafif nabız
        _marker.transform.localScale = new Vector3(d, d, 1f);
    }
}
