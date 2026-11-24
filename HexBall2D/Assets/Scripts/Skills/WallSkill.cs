using Fusion;
using UnityEngine;

/// <summary>
/// DUVAR ÖRME (butonla, sol / sağ)
/// Keeper'ın hareket alanının hemen arkasına, kaleyi koruyan ince bir duvar çıkar.
/// Top duvara çarpınca seker ve duvar yok olur. Keeper duvarın içinden geçebilir.
/// Seviye 1: Sadece SOL duvar (kalenin sol yarısı), bekleme 30 sn.
/// Seviye 2: SAĞ duvar da açılır (sağ yarı), bekleme 15 sn.
/// Seviye 3: Her iki buton da kalenin TAMAMINI kaplayan duvar çıkarır (ikisi birden = 2 kat koruma).
/// Sol ve sağın bekleme süreleri ayrıdır.
/// </summary>
public class WallSkill : SkillBase
{
    public const int ActionLeft = 0, ActionRight = 1;

    [Tooltip("1., 2. ve 3. seviyede bekleme süresi (saniye)")]
    [SerializeField] float[] cooldowns = { 30f, 15f, 15f };
    [Tooltip("Duvarın keeper hareket alanının arkasına uzaklığı")]
    [SerializeField] float backOffset = 0.55f;
    [Tooltip("Duvarın kalınlığı")]
    [SerializeField] float thickness = 0.15f;
    [Tooltip("Duvarın kafes genişliğinden her iki uçta taşma payı")]
    [SerializeField] float extraWidth = 0.45f;

    [Networked] public NetworkBool LeftUp { get; set; }
    [Networked] public NetworkBool RightUp { get; set; }
    [Networked] public TickTimer CooldownLeft { get; set; }
    [Networked] public TickTimer CooldownRight { get; set; }

    public override SkillId Id => SkillId.Wall;
    public float Thickness => thickness;
    public override int ActionButtonCount => 2;
    public override string ActionLabel(int action) => action == ActionLeft ? "Sol" : "Sağ";

    public override bool HasAction(int action) =>
        action == ActionLeft ? Level >= 1 :
        action == ActionRight && Level >= 2;

    /// Duvar zaten ayaktaysa tekrar basılamaz.
    public override bool CanUseAction(int action) => HasAction(action) && !IsUp(action);

    public override float CooldownRemainingFor(int action)
    {
        if (Runner == null) return 0f;
        TickTimer t = action == ActionLeft ? CooldownLeft : CooldownRight;
        return t.RemainingTime(Runner) ?? 0f;
    }

    public override float CooldownRemaining => CooldownRemainingFor(ActionLeft);

    public bool IsUp(int action) => action == ActionLeft ? LeftUp : RightUp;

    public override void UseAction(int action, float aimX)
    {
        if (!HasStateAuthority || !CanUseAction(action)) return;

        TickTimer cd = action == ActionLeft ? CooldownLeft : CooldownRight;
        if (!cd.ExpiredOrNotRunning(Runner)) return;

        cd = TickTimer.CreateFromSeconds(Runner, cooldowns[Mathf.Clamp(Level - 1, 0, cooldowns.Length - 1)]);
        if (action == ActionLeft) { LeftUp = true; CooldownLeft = cd; }
        else { RightUp = true; CooldownRight = cd; }
    }

    /// Host: top çarptı, duvar yok olur.
    public void ServerBreak(int action)
    {
        if (action == ActionLeft) LeftUp = false;
        else RightUp = false;
    }

    /// Duvarın dünya koordinatındaki iki ucu.
    public void GetWall(int action, out Vector2 a, out Vector2 b)
    {
        ArenaLayout arena = ArenaLayout.Instance;
        int slot = Keeper.Slot;
        float hw = arena.CageHalfWidth + extraWidth;
        float y = -arena.CageBack - backOffset;
        bool full = Level >= MaxLevel;

        float xMin = action == ActionLeft ? -hw : (full ? -hw : 0f);
        float xMax = action == ActionLeft ? (full ? hw : 0f) : hw;
        a = arena.LocalToWorld(slot, new Vector2(xMin, y));
        b = arena.LocalToWorld(slot, new Vector2(xMax, y));
    }

    // ---------------- Görüntü ----------------

    SpriteRenderer[] _visuals;

    public override void Spawned()
    {
        _visuals = new SpriteRenderer[2];
        for (int i = 0; i < 2; i++)
        {
            var go = new GameObject(i == 0 ? "Wall_Left" : "Wall_Right"); // keeper'a bağlı değil, yerinde durur
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteUtil.Square;
            sr.sortingOrder = 5;
            go.SetActive(false);
            _visuals[i] = sr;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_visuals == null) return;
        foreach (SpriteRenderer sr in _visuals)
            if (sr != null) Destroy(sr.gameObject);
    }

    public override void Render()
    {
        if (_visuals == null || ArenaLayout.Instance == null) return;

        for (int i = 0; i < 2; i++)
        {
            SpriteRenderer sr = _visuals[i];
            bool up = IsUp(i);
            if (sr.gameObject.activeSelf != up) sr.gameObject.SetActive(up);
            if (!up) continue;

            GetWall(i, out Vector2 a, out Vector2 b);
            Vector2 d = b - a;
            sr.transform.SetPositionAndRotation((a + b) * 0.5f, Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg));
            sr.transform.localScale = new Vector3(d.magnitude, thickness, 1f);
            sr.color = HexArena.SlotColors[Keeper.Slot];
        }
    }
}
