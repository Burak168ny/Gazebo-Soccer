using Fusion;
using UnityEngine;

/// <summary>
/// IŞINLANMA (butonla)
/// Keeper kafesinin en soluna / en sağına anında ışınlanır (derinliği değişmez).
/// Seviye 1: Sadece SOL, bekleme 30 sn.
/// Seviye 2: Sadece SOL, bekleme 15 sn.
/// Seviye 3: SOL ve SAĞ, bekleme 3 sn. Sol ve sağın bekleme süreleri ayrıdır.
/// Client'ta da tahmin edilir: bastığın an ışınlanırsın, host sonucu onaylar.
/// </summary>
public class TeleportSkill : SkillBase
{
    public const int ActionLeft = 0, ActionRight = 1;

    [Tooltip("1., 2. ve 3. seviyede bekleme süresi (saniye)")]
    [SerializeField] float[] cooldowns = { 30f, 15f, 3f };

    [Networked] public TickTimer CooldownLeft { get; set; }
    [Networked] public TickTimer CooldownRight { get; set; }

    public override SkillId Id => SkillId.Teleport;
    public override bool PredictOnClient => true;
    public override int ActionButtonCount => 2;
    public override string ActionLabel(int action) => action == ActionLeft ? "Sol" : "Sağ";

    public override float CooldownRemainingFor(int action)
    {
        if (Runner == null) return 0f;
        TickTimer t = action == ActionLeft ? CooldownLeft : CooldownRight;
        return t.RemainingTime(Runner) ?? 0f;
    }

    public override float CooldownRemaining => CooldownRemainingFor(ActionLeft);

    public override bool HasAction(int action) =>
        action == ActionLeft ? Level >= 1 :
        action == ActionRight && Level >= MaxLevel;

    public override bool CanUseAction(int action) => HasAction(action);

    public override void UseAction(int action, float aimX)
    {
        if (!HasAction(action)) return;

        TickTimer cd = action == ActionLeft ? CooldownLeft : CooldownRight;
        if (!cd.ExpiredOrNotRunning(Runner)) return;

        cd = TickTimer.CreateFromSeconds(Runner, cooldowns[Mathf.Clamp(Level - 1, 0, cooldowns.Length - 1)]);
        if (action == ActionLeft) CooldownLeft = cd;
        else CooldownRight = cd;

        Keeper.TeleportToEdge(action == ActionRight ? 1 : -1);
    }
}
