using Fusion;
using UnityEngine;

/// <summary>
/// GÜÇLÜ ŞUT
/// Seviye 1-2-3: Şut hızı normal hız × oran kadar artar (ilk çarpmadan sonra normal hıza döner).
/// Seviye 3 aktif: Butona basınca bir sonraki şut "zincirli şut" olur.
///   Gol olursa top tutulamaz hale gelip şutu atana geri döner, o yine şut çeker (en fazla 2 kez).
///   Duvara çarparsa ya da biri tutarsa zincir biter. Bekleme süresi var.
/// </summary>
public class PowerShotSkill : SkillBase
{
    [Tooltip("1., 2. ve 3. seviyede şut hızına eklenecek oran (0.5 = %50 daha hızlı)")]
    [SerializeField] float[] speedBonusRatio = { 0.5f, 1f, 1f };
    [SerializeField] float cooldownSeconds = 60f;
    [Tooltip("Gol olursa top kaç kez geri gelsin")]
    [SerializeField] int maxReturns = 2;
    [Tooltip("Topun geri dönüş hızı")]
    [SerializeField] float returnSpeed = 25f;

    [Networked] public NetworkBool Armed { get; set; }   // butona basıldı, sıradaki şut zincirli
    [Networked] public TickTimer Cooldown { get; set; }

    public override SkillId Id => SkillId.PowerShot;
    public override bool HasActiveAbility => Level >= MaxLevel;
    public override int ActionButtonCount => 1;
    public override float CooldownRemaining => Runner != null ? Cooldown.RemainingTime(Runner) ?? 0f : 0f;

    /// Şut hızına eklenecek oran: hız = normal hız + normal hız × oran
    public float ThrowSpeedBonusRatio => Level > 0 ? speedBonusRatio[Mathf.Min(Level, speedBonusRatio.Length) - 1] : 0f;
    public int MaxReturns => maxReturns;
    public float ReturnSpeed => returnSpeed;

    public override void ServerActivate(float aimX)
    {
        if (!HasStateAuthority || !HasActiveAbility || Armed) return;
        if (!Cooldown.ExpiredOrNotRunning(Runner)) return;

        Armed = true;
        Cooldown = TickTimer.CreateFromSeconds(Runner, cooldownSeconds);
    }

    /// Ball fırlatırken çağırır: hazır bekleyen zincirli şut varsa kullanır.
    public bool ConsumeArmed()
    {
        if (!Armed) return false;
        Armed = false;
        return true;
    }
}
