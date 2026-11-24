using Fusion;
using UnityEngine;

/// <summary>
/// SHINRA TENSEI (İtme, butonla)
/// Basınca keeper'ının etrafında bir şok dalgası çıkar. Menzildeki, SANA DOĞRU gelen her şey
/// (toplar, sahte/çoğalan toplar, donduran diskler) geldiği yöne geri, daha hızlı fırlatılır.
/// Geri itilen top artık senin şutun sayılır; geri itilen disk artık senin diskin olur (sana değmez,
/// atanı dondurabilir).
/// Seviye 1: menzil 3, 1.3 kat hız, bekleme 60 sn.
/// Seviye 2: menzil 4.5, 1.6 kat hız, bekleme 40 sn.
/// Seviye 3: menzil 6, 2 kat hız, bekleme 20 sn.
/// </summary>
public class PushSkill : SkillBase
{
    [Tooltip("1., 2. ve 3. seviyede itme menzili (keeper merkezinden)")]
    [SerializeField] float[] radii = { 3f, 4.5f, 6f };
    [Tooltip("1., 2. ve 3. seviyede geri itilen şeylerin hız çarpanı")]
    [SerializeField] float[] speedMultipliers = { 1.3f, 1.6f, 2f };
    [Tooltip("1., 2. ve 3. seviyede bekleme süresi (saniye)")]
    [SerializeField] float[] cooldowns = { 60f, 40f, 20f };

    [Networked] public TickTimer Cooldown { get; set; }

    public override SkillId Id => SkillId.Push;
    public override bool HasActiveAbility => Level >= 1;
    public override int ActionButtonCount => 1;
    public override float CooldownRemaining => Runner != null ? Cooldown.RemainingTime(Runner) ?? 0f : 0f;

    int LevelIndex => Mathf.Clamp(Level - 1, 0, 2);

    public override void ServerActivate(float aimX)
    {
        if (!HasStateAuthority || !HasActiveAbility || !Cooldown.ExpiredOrNotRunning(Runner)) return;

        MatchManager mm = MatchManager.Instance;
        if (mm == null || mm.Phase != MatchPhase.Playing) return;

        int i = LevelIndex;
        float radius = radii[Mathf.Min(i, radii.Length - 1)];
        float mult = speedMultipliers[Mathf.Min(i, speedMultipliers.Length - 1)];
        Vector2 center = Keeper.WorldPosition;

        Cooldown = TickTimer.CreateFromSeconds(Runner, cooldowns[Mathf.Min(i, cooldowns.Length - 1)]);

        foreach (Ball b in Ball.All)
            if (IsIncoming(b.transform.position, b.Velocity, center, radius) && !b.IsHeld && b.ReturnToSlot < 0)
                b.ServerPushBack(Keeper, mult);

        foreach (Disc d in Disc.All)
            if (IsIncoming(d.transform.position, d.Velocity, center, radius))
                d.ServerPushBack(Keeper.Slot, mult);

        RPC_Wave(center, radius);
    }

    static bool IsIncoming(Vector2 pos, Vector2 vel, Vector2 center, float radius)
    {
        Vector2 toMe = center - pos;
        return toMe.sqrMagnitude <= radius * radius && Vector2.Dot(vel, toMe) > 0f;
    }

    // ---------------- Görüntü: genişleyen halka (herkes görür) ----------------

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_Wave(Vector2 center, float radius)
    {
        var go = new GameObject("PushWave");
        go.transform.position = center;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteUtil.Circle;
        sr.sortingOrder = 3;
        Color c = Keeper != null ? HexArena.SlotColors[Keeper.Slot] : Color.white;
        go.AddComponent<PushWaveFx>().Init(radius, c);
    }
}

/// Şok dalgası efekti: büyüyerek solar, sonra kendini siler.
public class PushWaveFx : MonoBehaviour
{
    const float Duration = 0.35f;
    float _radius, _t;
    SpriteRenderer _sr;
    Color _color;

    public void Init(float radius, Color color)
    {
        _radius = radius;
        _color = color;
        _sr = GetComponent<SpriteRenderer>();
        Update();
    }

    void Update()
    {
        _t += Time.deltaTime;
        float k = Mathf.Clamp01(_t / Duration);
        float d = Mathf.Lerp(0.3f, _radius * 2f, k);
        transform.localScale = new Vector3(d, d, 1f);
        Color c = _color;
        c.a = 0.45f * (1f - k);
        if (_sr != null) _sr.color = c;
        if (k >= 1f) Destroy(gameObject);
    }
}
