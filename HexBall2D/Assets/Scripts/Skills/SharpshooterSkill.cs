using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// KESKİN NİŞANCI (pasif)
/// Top elindeyken, şutun gideceği rotayı noktalı bir nişan çizgisiyle gösterir (sadece SEN görürsün).
/// Seviye 1: ilk sekmeye kadar + 1 sekme. Seviye 2: 2 sekme. Seviye 3: 3 sekme.
/// Klavyede sağ/sol tuşu, telefonda şut joystick'i çekilirken nişan anında güncellenir.
/// Not: Duvar/kale çarpmaları gösterilir; yetenek duvarları, aynalar, falso ve mıknatıs hesaba katılmaz.
/// </summary>
public class SharpshooterSkill : SkillBase
{
    [Tooltip("1., 2. ve 3. seviyede gösterilen sekme sayısı")]
    [SerializeField] int[] bounceCounts = { 1, 2, 3 };
    [Tooltip("Noktalar arası mesafe")]
    [SerializeField] float dotSpacing = 0.35f;
    [Tooltip("Nokta boyutu")]
    [SerializeField] float dotSize = 0.12f;
    [Tooltip("Her parçanın en fazla uzunluğu")]
    [SerializeField] float maxSegmentLength = 40f;

    const int MaxDots = 200;

    public override SkillId Id => SkillId.Sharpshooter;

    readonly List<SpriteRenderer> _dots = new List<SpriteRenderer>();
    readonly List<Vector2> _points = new List<Vector2>();
    int _shownDots;

    int Bounces => bounceCounts[Mathf.Clamp(Level - 1, 0, bounceCounts.Length - 1)];

    void LateUpdate()
    {
        if (Object == null || !Object.IsValid || !HasInputAuthority || Level <= 0) { ShowDots(0); return; }

        Ball ball = Ball.HeldBy(Keeper.Slot);
        ArenaLayout arena = ArenaLayout.Instance;
        if (ball == null || arena == null) { ShowDots(0); return; }

        Vector2 dir = CurrentAimDirection(arena, ball);
        BuildPath(ball.VisualPosition, dir, ball.R, arena);
        DrawPath();
    }

    /// Şut atılsa topun gideceği yön (Ball.ServerThrow ile aynı hesap).
    Vector2 CurrentAimDirection(ArenaLayout arena, Ball ball)
    {
        int slot = Keeper.Slot;
        bool reversed = Keeper.IsReversed;

        Vector2 joy = MobileControls.Instance != null ? MobileControls.Instance.ThrowAimPreview : Vector2.zero;
        if (joy.y > 0.01f)
        {
            if (reversed) joy.x = -joy.x;
            return (arena.Right(slot) * joy.x + arena.Inward(slot) * joy.y).normalized;
        }

        float aimX = Mathf.Clamp(NetworkBootstrap.LocalMove.x, -1f, 1f);
        if (reversed) aimX = -aimX;
        return (arena.Inward(slot) + arena.Right(slot) * (aimX * ball.AimInfluence)).normalized;
    }

    void BuildPath(Vector2 pos, Vector2 dir, float radius, ArenaLayout arena)
    {
        _points.Clear();
        _points.Add(pos);

        for (int i = 0; i <= Bounces; i++)
        {
            RaycastHit2D hit = Physics2D.CircleCast(pos, radius, dir, maxSegmentLength, arena.BallCastMask);
            if (!hit || hit.fraction <= 0f)
            {
                _points.Add(pos + dir * maxSegmentLength);
                break;
            }

            pos = hit.centroid;
            _points.Add(pos);
            dir = Vector2.Reflect(dir, hit.normal);
            pos += hit.normal * 0.01f; // duvardan hafif ayrıl
        }
    }

    void DrawPath()
    {
        int count = 0;
        float carry = 0f; // önceki parçadan kalan mesafe (noktalar düzgün aralıklı olsun)
        Color color = HexArena.SlotColors[Keeper.Slot];

        for (int i = 1; i < _points.Count && count < MaxDots; i++)
        {
            Vector2 a = _points[i - 1], b = _points[i];
            float len = Vector2.Distance(a, b);
            Vector2 step = len > 0.0001f ? (b - a) / len : Vector2.zero;

            for (float d = carry; d < len && count < MaxDots; d += dotSpacing)
            {
                SpriteRenderer dot = GetDot(count++);
                dot.transform.position = a + step * d;
                float fade = 1f - (float)(i - 1) / Mathf.Max(1, _points.Count - 1) * 0.6f; // sonraki sekmeler soluk
                dot.color = new Color(color.r, color.g, color.b, 0.85f * fade);
            }
            carry = Mathf.Repeat(carry - len, dotSpacing);
        }
        ShowDots(count);
    }

    SpriteRenderer GetDot(int i)
    {
        while (_dots.Count <= i)
        {
            var go = new GameObject("AimDot");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteUtil.Circle;
            sr.sortingOrder = 15;
            go.transform.localScale = new Vector3(dotSize, dotSize, 1f);
            _dots.Add(sr);
        }
        return _dots[i];
    }

    void ShowDots(int count)
    {
        if (count == _shownDots) return;
        for (int i = 0; i < _dots.Count; i++)
        {
            bool on = i < count;
            if (_dots[i].gameObject.activeSelf != on) _dots[i].gameObject.SetActive(on);
        }
        _shownDots = count;
    }

    public override void Despawned(Fusion.NetworkRunner runner, bool hasState)
    {
        foreach (SpriteRenderer sr in _dots)
            if (sr != null) Destroy(sr.gameObject);
        _dots.Clear();
    }
}
