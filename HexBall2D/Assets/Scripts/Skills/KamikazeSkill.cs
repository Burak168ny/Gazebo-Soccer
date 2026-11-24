using Fusion;
using UnityEngine;

/// <summary>
/// KAMİKAZE ŞUT (butonla)
/// Butona basınca sıradaki şutun kamikaze olur (top kırmızımsı görünür).
/// O top gol olursa golü yiyen de SEN de aynı sayıda can kaybedersiniz:
/// Seviye 1: 3 can, bekleme 60 sn.
/// Seviye 2: 5 can, bekleme 30 sn.
/// Seviye 3: 10 can, bekleme 15 sn.
/// Top sekse de kamikaze kalır; biri tutarsa, aynaya çarparsa ya da gol olursa biter.
/// Kendi kalene girerse normal (1 can) sayılır.
/// </summary>
public class KamikazeSkill : SkillBase
{
    [Tooltip("1., 2. ve 3. seviyede gol olunca iki tarafın da kaybettiği can")]
    [SerializeField] int[] damages = { 3, 5, 10 };
    [Tooltip("1., 2. ve 3. seviyede bekleme süresi (saniye)")]
    [SerializeField] float[] cooldowns = { 60f, 30f, 15f };

    [Networked] public NetworkBool Armed { get; set; }
    [Networked] public TickTimer Cooldown { get; set; }

    public override SkillId Id => SkillId.Kamikaze;
    public override bool HasActiveAbility => Level >= 1;
    public override int ActionButtonCount => 1;
    public override float CooldownRemaining => Runner != null ? Cooldown.RemainingTime(Runner) ?? 0f : 0f;
    public override bool CanActivateNow => !Armed;

    int LevelIndex => Mathf.Clamp(Level - 1, 0, 2);
    public int Damage => damages[Mathf.Min(LevelIndex, damages.Length - 1)];

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
