using Fusion;
using UnityEngine;

/// <summary>
/// GÖRÜNMEZ ŞUT (her seviyede butonla kullanılır, top sendeyken)
/// Seviye 1: Butona basınca top görünmez olarak fırlar. İlk çarpmada (duvar/gol) ya da tutulunca görünür olur.
/// Seviye 2: Aynısı, ama top 1 çarpmaya dayanır; ikinci çarpmada görünür olur. Tutulursa hemen görünür.
/// Seviye 3: Butona basınca keeper ve top birlikte görünmez olur. Fırlatınca (Space) keeper görünür olur,
///           top ilk çarpmaya kadar görünmez kalır.
/// Görünmezlik sadece RAKİPLER için; sahibi kendini ve topu yarı saydam görür.
/// </summary>
public class InvisibleShotSkill : SkillBase
{
    [SerializeField] float cooldownSeconds = 60f;
    [Tooltip("1., 2. ve 3. seviyede top görünmezken kaç çarpmaya dayanır (0 = ilk çarpmada görünür)")]
    [SerializeField] int[] hitsThrough = { 0, 1, 0 };
    [Tooltip("Seviye 3: keeper en fazla kaç saniye görünmez kalabilir (fırlatmazsa)")]
    [SerializeField] float maxKeeperHiddenSeconds = 5f;

    [Networked] public TickTimer Cooldown { get; set; }
    [Networked] public NetworkBool KeeperHidden { get; set; }
    [Networked] public TickTimer KeeperHiddenTimer { get; set; }

    public override SkillId Id => SkillId.InvisibleShot;
    public override bool HasActiveAbility => Level >= 1;
    public override int ActionButtonCount => 1;
    public override float CooldownRemaining => Runner != null ? Cooldown.RemainingTime(Runner) ?? 0f : 0f;

    /// Sadece top sendeyken kullanılabilir.
    public override bool CanActivateNow => Keeper != null && Ball.HeldBy(Keeper.Slot) != null && !KeeperHidden;

    int HitsThrough => hitsThrough[Mathf.Clamp(Level - 1, 0, hitsThrough.Length - 1)];

    public override void ServerActivate(float aimX)
    {
        if (!HasStateAuthority || Level < 1 || !Cooldown.ExpiredOrNotRunning(Runner)) return;

        Ball ball = Ball.HeldBy(Keeper.Slot);
        if (ball == null) return;

        Cooldown = TickTimer.CreateFromSeconds(Runner, cooldownSeconds);

        if (Level >= MaxLevel)
        {
            // Keeper (ve elindeki top) görünmez olur; fırlatınca keeper görünür, top görünmez gider
            KeeperHidden = true;
            KeeperHiddenTimer = TickTimer.CreateFromSeconds(Runner, maxKeeperHiddenSeconds);
        }
        else
        {
            ball.ServerThrow(Keeper, aimX);           // hemen fırlat
            ball.MakeInvisible(Keeper.Slot, HitsThrough);
        }
    }

    /// Ball fırlatılınca çağırır (Seviye 3).
    public void ServerOnThrow(Ball ball)
    {
        if (!KeeperHidden) return;
        KeeperHidden = false;
        ball.MakeInvisible(Keeper.Slot, HitsThrough);
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority && KeeperHidden && KeeperHiddenTimer.Expired(Runner))
            KeeperHidden = false; // süre doldu
    }
}
