using Fusion;
using UnityEngine;

/// <summary>
/// MIKNATIS ELDİVEN
/// Seviye 1 (pasif): Yakınından geçen top hafifçe sana doğru kıvrılır.
/// Seviye 2 (pasif): Güçlü çeker.
/// Seviye 3: Buton açılır. Sahadaki gerçek top (bir keeper'ın elinde olsa bile) ondan koparılır,
///           tutulamaz halde sana uçar ve senin kontrolüne geçer. Elindekinin şut hakkı tamamen biter.
/// Çekim sadece sana doğru gelen toplara uygulanır (kendi şutunu geri çekmez).
/// </summary>
public class MagnetSkill : SkillBase
{
    [Tooltip("1., 2. ve 3. seviyede çekim gücü (top yönünü saniyede en fazla kaç derece sana çevirir)")]
    [SerializeField] float[] pullDegreesPerSecond = { 90f, 250f, 250f };
    [Tooltip("Çekimin başladığı mesafe (keeper merkezinden)")]
    [SerializeField] float pullRadius = 3f;
    [Tooltip("Seviye 3 butonunun bekleme süresi (saniye)")]
    [SerializeField] float stealCooldown = 30f;

    [Networked] public TickTimer Cooldown { get; set; }

    public override SkillId Id => SkillId.Magnet;
    public override bool HasActiveAbility => Level >= MaxLevel;
    public override int ActionButtonCount => 1;
    public override float CooldownRemaining => Runner != null ? Cooldown.RemainingTime(Runner) ?? 0f : 0f;
    /// Top zaten sendeyse basılmaz.
    public override bool CanActivateNow => Keeper != null && Ball.HeldBy(Keeper.Slot) == null;

    public float PullRadius => pullRadius;
    public float PullDegreesPerSecond => Level > 0 ? pullDegreesPerSecond[Mathf.Clamp(Level - 1, 0, pullDegreesPerSecond.Length - 1)] : 0f;

    public override void ServerActivate(float aimX)
    {
        if (!HasStateAuthority || !HasActiveAbility || !Cooldown.ExpiredOrNotRunning(Runner)) return;

        int me = Keeper.Slot;
        if (Ball.HeldBy(me) != null) return;

        // En yakın gerçek top (sahte / çoğalan kopya değil, zaten bana gelmiyor)
        Ball target = null;
        float best = float.MaxValue;
        Vector2 myPos = Keeper.WorldPosition;
        foreach (Ball b in Ball.All)
        {
            if (b.IsDecoy || b.IsSplitCopy || b.ReturnToSlot == me) continue;
            float d = ((Vector2)b.transform.position - myPos).sqrMagnitude;
            if (d < best) { best = d; target = b; }
        }
        if (target == null) return;

        Cooldown = TickTimer.CreateFromSeconds(Runner, stealCooldown);
        target.ServerStealTo(me);
    }
}
