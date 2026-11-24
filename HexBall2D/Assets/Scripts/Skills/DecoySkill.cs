using Fusion;
using UnityEngine;

/// <summary>
/// YEM TOP (butonla)
/// Butona basınca sıradaki şut 2 top olur: nişan açısı +10° ve -10°. Biri gerçek, biri sahte (rastgele).
/// Rakipler ikisini aynı görür; şutu atan sahteyi soluk görür.
/// Sahte top herhangi bir şeye (duvar, kale, keeper) değince yok olur.
/// Seviye 1: Sahte kaleye girerse gol sayılmaz. Bekleme 60 sn.
/// Seviye 2: Sahte kaleye girerse gol sayılır. Bekleme 30 sn.
/// Seviye 3: Sahte gol sayılır ve golü yiyen 1 sn hareketsiz kalır. Bekleme 15 sn.
/// </summary>
public class DecoySkill : SkillBase
{
    [Tooltip("1., 2. ve 3. seviyede bekleme süresi (saniye)")]
    [SerializeField] float[] cooldowns = { 60f, 30f, 15f };
    [Tooltip("İki topun nişan açısından sapması (derece). Biri +, biri -")]
    [SerializeField] float splitAngle = 10f;
    [Tooltip("Seviye 3: sahte gol olunca golü yiyenin hareketsiz kalma süresi (saniye)")]
    [SerializeField] float stunSeconds = 1f;

    [Networked] public NetworkBool Armed { get; set; }   // butona basıldı, sıradaki şut yem toplu
    [Networked] public TickTimer Cooldown { get; set; }

    public override SkillId Id => SkillId.Decoy;
    public override bool HasActiveAbility => Level >= 1;
    public override int ActionButtonCount => 1;
    public override float CooldownRemaining => Runner != null ? Cooldown.RemainingTime(Runner) ?? 0f : 0f;
    public override bool CanActivateNow => !Armed; // hazırken tekrar basılmaz

    int LevelIndex => Mathf.Clamp(Level - 1, 0, 2);
    public float SplitAngle => splitAngle;
    public bool FakeScores => Level >= 2;
    public float FakeStunSeconds => Level >= 3 ? stunSeconds : 0f;

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
