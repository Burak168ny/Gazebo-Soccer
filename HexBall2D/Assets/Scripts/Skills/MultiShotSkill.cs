using Fusion;
using UnityEngine;

/// <summary>
/// ÇOĞALAN ŞUT (butonla)
/// Butona basınca sıradaki şutun top yelpaze şeklinde çoğalır (1. seviye 3, 2. seviye 5, 3. seviye 10 top).
/// Gruptaki her top ilk temasında (duvar, kale, keeper) yok olur; kaleye değdiyse gol sayılır, keeper tutamaz.
/// Sona kalan top gerçek top olur ve normal davranır. İki top aynı anda çarparsa biri yok olur, diğeri kalır.
/// </summary>
public class MultiShotSkill : SkillBase
{
    [Tooltip("1., 2. ve 3. seviyede toplam top sayısı (çoğalınca)")]
    [SerializeField] int[] ballCounts = { 3, 5, 10 };
    [Tooltip("1., 2. ve 3. seviyede yelpazenin toplam açısı (derece)")]
    [SerializeField] float[] spreadDegrees = { 30f, 50f, 80f };
    [Tooltip("1., 2. ve 3. seviyede bekleme süresi (saniye)")]
    [SerializeField] float[] cooldowns = { 30f, 25f, 20f };

    [Networked] public NetworkBool Armed { get; set; }   // butona basıldı, sıradaki şut çoğalır
    [Networked] public TickTimer Cooldown { get; set; }

    public override SkillId Id => SkillId.MultiShot;
    public override bool HasActiveAbility => Level >= 1;
    public override int ActionButtonCount => 1;
    public override float CooldownRemaining => Runner != null ? Cooldown.RemainingTime(Runner) ?? 0f : 0f;
    public override bool CanActivateNow => !Armed; // hazırken tekrar basılmaz

    int LevelIndex => Mathf.Clamp(Level - 1, 0, 2);
    public int BallCount => ballCounts[Mathf.Min(LevelIndex, ballCounts.Length - 1)];
    public float SpreadDegrees => spreadDegrees[Mathf.Min(LevelIndex, spreadDegrees.Length - 1)];

    public override void ServerActivate(float aimX)
    {
        if (!HasStateAuthority || !HasActiveAbility || Armed || !Cooldown.ExpiredOrNotRunning(Runner)) return;

        Armed = true;
        Cooldown = TickTimer.CreateFromSeconds(Runner, cooldowns[Mathf.Min(LevelIndex, cooldowns.Length - 1)]);
    }

    /// Ball fırlatırken çağırır: hazırsa kullanır.
    public bool ConsumeArmed()
    {
        if (!Armed) return false;
        Armed = false;
        return true;
    }
}
