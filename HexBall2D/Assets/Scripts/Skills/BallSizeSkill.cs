using Fusion;
using UnityEngine;

/// <summary>
/// TOP BOYUTU (2 buton: Büyüt / Küçült)
/// Sahadaki tüm topların boyutunu bir süreliğine değiştirir (çarpışma da görüntü de değişir).
/// Seviye 1: Büyüt butonu açılır, bekleme 60 sn.
/// Seviye 2: Küçült butonu da açılır, bekleme 30 sn.
/// Seviye 3: Bekleme 15 sn. İki butonun bekleme süresi ayrıdır.
/// </summary>
public class BallSizeSkill : SkillBase
{
    public const int ActionGrow = 0, ActionShrink = 1;

    [Tooltip("1., 2. ve 3. seviyede bekleme süresi (saniye)")]
    [SerializeField] float[] cooldowns = { 60f, 30f, 15f };
    [Tooltip("Büyütünce topun boyut çarpanı")]
    [SerializeField] float growScale = 2f;
    [Tooltip("Küçültünce topun boyut çarpanı")]
    [SerializeField] float shrinkScale = 0.5f;
    [Tooltip("Boyut değişikliği kaç saniye sürer")]
    [SerializeField] float durationSeconds = 8f;

    [Networked] public TickTimer CooldownGrow { get; set; }
    [Networked] public TickTimer CooldownShrink { get; set; }

    public override SkillId Id => SkillId.BallSize;
    public override int ActionButtonCount => 2;
    public override string ActionLabel(int action) => action == ActionGrow ? "Büyüt" : "Küçült";

    public override bool HasAction(int action) =>
        action == ActionGrow ? Level >= 1 :
        action == ActionShrink && Level >= 2;

    public override bool CanUseAction(int action) => HasAction(action);

    public override float CooldownRemainingFor(int action)
    {
        if (Runner == null) return 0f;
        TickTimer t = action == ActionGrow ? CooldownGrow : CooldownShrink;
        return t.RemainingTime(Runner) ?? 0f;
    }

    public override float CooldownRemaining => CooldownRemainingFor(ActionGrow);

    public override void UseAction(int action, float aimX)
    {
        if (!HasStateAuthority || !HasAction(action)) return;

        TickTimer cd = action == ActionGrow ? CooldownGrow : CooldownShrink;
        if (!cd.ExpiredOrNotRunning(Runner)) return;

        cd = TickTimer.CreateFromSeconds(Runner, cooldowns[Mathf.Clamp(Level - 1, 0, cooldowns.Length - 1)]);
        if (action == ActionGrow) CooldownGrow = cd;
        else CooldownShrink = cd;

        float scale = action == ActionGrow ? growScale : shrinkScale;
        foreach (Ball ball in Ball.All)
            ball.ServerSetSize(scale, durationSeconds);
    }
}
