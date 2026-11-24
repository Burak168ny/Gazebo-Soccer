using Fusion;
using UnityEngine;

/// <summary>
/// AYNA (butonla)
/// Butona basınca keeper alanının ön kenarında rastgele bir noktada yuvarlak bir ayna çıkar, 10 sn durur.
/// Kalene doğru gelen top aynaya değerse geldiği yöne geri döner (gol artık senin golün sayılır).
/// Kendi attığın / kalenden uzaklaşan toplara dokunmaz. Süre boyunca birden çok topu geri yollayabilir.
/// Seviye 1: Aynı hızla geri yollar.
/// Seviye 2: 1.5 kat hızla geri yollar (top ilk çarpmada normal hıza döner).
/// Seviye 3: 1.5 kat hızla ve GÖRÜNMEZ geri yollar; top bir yere çarpınca görünür olur.
/// </summary>
public class MirrorSkill : SkillBase
{
    [Tooltip("1., 2. ve 3. seviyede bekleme süresi (saniye)")]
    [SerializeField] float[] cooldowns = { 30f, 30f, 30f };
    [Tooltip("1., 2. ve 3. seviyede geri yollama hız çarpanı")]
    [SerializeField] float[] speedMultipliers = { 1f, 1.5f, 1.5f };
    [Tooltip("Ayna kaç saniye durur")]
    [SerializeField] float durationSeconds = 10f;
    [Tooltip("Aynanın yarıçapı")]
    [SerializeField] float mirrorRadius = 0.5f;
    [Tooltip("Aynanın keeper alanının ön kenarından ileri uzaklığı")]
    [SerializeField] float frontOffset = 0.6f;

    [Networked] public TickTimer Cooldown { get; set; }
    [Networked] public TickTimer ActiveTimer { get; set; }
    [Networked] public Vector2 LocalPos { get; set; }   // aynanın yeri (oyuncunun yerel koordinatında)

    public override SkillId Id => SkillId.Mirror;
    public override bool HasActiveAbility => Level >= 1;
    public override int ActionButtonCount => 1;
    public override float CooldownRemaining => Runner != null ? Cooldown.RemainingTime(Runner) ?? 0f : 0f;
    public override bool CanActivateNow => !IsUp; // ayna dururken tekrar basılmaz

    int LevelIndex => Mathf.Clamp(Level - 1, 0, 2);
    public bool IsUp => Runner != null && !ActiveTimer.ExpiredOrNotRunning(Runner);
    public float MirrorRadius => mirrorRadius;
    public float SpeedMultiplier => speedMultipliers[Mathf.Min(LevelIndex, speedMultipliers.Length - 1)];
    public bool SendsInvisible => Level >= 3;
    public int OwnerSlot => Keeper.Slot;
    public Vector2 WorldPos => ArenaLayout.Instance.LocalToWorld(Keeper.Slot, LocalPos);

    public override void ServerActivate(float aimX)
    {
        if (!HasStateAuthority || !HasActiveAbility || IsUp || !Cooldown.ExpiredOrNotRunning(Runner)) return;

        ArenaLayout arena = ArenaLayout.Instance;
        float hw = arena.CageHalfWidth;
        LocalPos = new Vector2(Random.Range(-hw, hw), arena.CageForward + frontOffset);
        ActiveTimer = TickTimer.CreateFromSeconds(Runner, durationSeconds);
        Cooldown = TickTimer.CreateFromSeconds(Runner, cooldowns[Mathf.Min(LevelIndex, cooldowns.Length - 1)]);
    }

    /// Bu yöndeki top aynaya takılır mı? (sadece aynanın sahibinin kalesine doğru gelen toplar)
    public bool Catches(Vector2 ballVelocity) =>
        Vector2.Dot(ballVelocity, ArenaLayout.Instance.Inward(Keeper.Slot)) < 0f;

    // ---------------- Görüntü ----------------

    SpriteRenderer _visual;

    public override void Spawned()
    {
        var go = new GameObject("Mirror"); // keeper'a bağlı değil, yerinde durur
        _visual = go.AddComponent<SpriteRenderer>();
        _visual.sprite = SpriteUtil.Circle;
        _visual.sortingOrder = 6;
        go.SetActive(false);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_visual != null) Destroy(_visual.gameObject);
    }

    public override void Render()
    {
        if (_visual == null || ArenaLayout.Instance == null) return;

        bool up = IsUp;
        if (_visual.gameObject.activeSelf != up) _visual.gameObject.SetActive(up);
        if (!up) return;

        _visual.transform.position = WorldPos;
        float d = mirrorRadius * 2f;
        _visual.transform.localScale = new Vector3(d, d, 1f);
        Color c = Color.Lerp(new Color(0.75f, 0.95f, 1f), HexArena.SlotColors[Keeper.Slot], 0.25f);
        float left = ActiveTimer.RemainingTime(Runner) ?? 0f;
        c.a = left < 2f && Mathf.Repeat(left * 4f, 1f) < 0.5f ? 0.35f : 0.85f; // bitmeden önce yanıp söner
        _visual.color = c;
    }
}
