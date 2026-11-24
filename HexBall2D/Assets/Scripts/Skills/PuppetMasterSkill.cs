using Fusion;
using UnityEngine;

/// <summary>
/// KUKLA USTASI (butonla, rakibi sen seçersin)
/// Butona basınca rakip listesi çıkar, birine dokunursun. Süre boyunca o rakibin keeper'ını
/// SEN yönetirsin: senin hareket tuşların/joystick'in onu kendi ekranına göre yürütür, şut tuşun
/// onun elindeki topu atar. Bu sırada senin keeper'ın yerinde durur, kuklanın kendi tuşları çalışmaz
/// (sadece Arındır kullanabilir). Kukla, ustasının rengine bürünür.
/// Seviye 1: 1.5 sn, bekleme 120 sn.
/// Seviye 2: 2 sn, bekleme 90 sn.
/// Seviye 3: 3 sn, bekleme 60 sn.
/// </summary>
public class PuppetMasterSkill : SkillBase
{
    [Tooltip("1., 2. ve 3. seviyede kontrol süresi (saniye)")]
    [SerializeField] float[] durations = { 1.5f, 2f, 3f };
    [Tooltip("1., 2. ve 3. seviyede bekleme süresi (saniye)")]
    [SerializeField] float[] cooldowns = { 120f, 90f, 60f };

    [Networked] public TickTimer Cooldown { get; set; }

    bool _picking; // sadece bu bilgisayarda: rakip seçme listesi açık

    public override SkillId Id => SkillId.PuppetMaster;
    public override bool HasActiveAbility => Level >= 1;
    public override int ActionButtonCount => 1;
    public override float CooldownRemaining => Runner != null ? Cooldown.RemainingTime(Runner) ?? 0f : 0f;
    public override bool CanActivateNow => Keeper != null && !Keeper.IsPuppeteering && !Keeper.IsPuppeted && HasTarget();

    int LevelIndex => Mathf.Clamp(Level - 1, 0, 2);

    public override bool HandleLocalAction(int action)
    {
        if (!HasInputAuthority) return false;
        _picking = !_picking;
        return true;
    }

    bool IsValidTarget(int slot)
    {
        MatchManager mm = MatchManager.Instance;
        if (mm == null || Keeper == null || slot == Keeper.Slot || !mm.IsAlive(slot)) return false;
        KeeperController k = KeeperController.FindBySlot(slot);
        return k != null && !k.IsPuppeted;
    }

    bool HasTarget()
    {
        for (int s = 0; s < HexArena.SlotCount; s++)
            if (IsValidTarget(s)) return true;
        return false;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_Control(int targetSlot)
    {
        if (Level < 1 || !Cooldown.ExpiredOrNotRunning(Runner) || Keeper.IsSilenced || Keeper.IsPuppeted || Keeper.IsPuppeteering) return;

        MatchManager mm = MatchManager.Instance;
        if (mm == null || mm.Phase != MatchPhase.Playing || !IsValidTarget(targetSlot)) return;

        Cooldown = TickTimer.CreateFromSeconds(Runner, cooldowns[Mathf.Min(LevelIndex, cooldowns.Length - 1)]);
        KeeperController target = KeeperController.FindBySlot(targetSlot);
        target.ServerMakePuppet(Keeper, durations[Mathf.Min(LevelIndex, durations.Length - 1)]);
    }

    void OnGUI()
    {
        if (!_picking || Object == null || !Object.IsValid || !HasInputAuthority) return;

        MatchManager mm = MatchManager.Instance;
        if (mm == null || mm.Phase != MatchPhase.Playing || !CanActivateNow || CooldownRemaining > 0f)
        {
            _picking = false;
            return;
        }

        int picked = SlotPicker.Draw("Kukla Ustası: kimi?", IsValidTarget);
        if (picked == SlotPicker.Cancel) _picking = false;
        else if (picked >= 0) { _picking = false; RPC_Control(picked); }
    }
}
