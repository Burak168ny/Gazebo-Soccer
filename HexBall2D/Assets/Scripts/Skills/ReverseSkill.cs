using Fusion;
using UnityEngine;

/// <summary>
/// TERS KONTROL (butonla)
/// Butona basınca hayattaki TÜM rakiplerin kontrolleri ters döner: sağ-sol ve ileri-geri yer değiştirir
/// (klavyede ve joystick'te nişanın sağ-solu da ters). Ters dönen keeper mor renkte görünür.
/// Seviye 1: 2 sn, bekleme 120 sn.
/// Seviye 2: 5 sn, bekleme 90 sn.
/// Seviye 3: 20 sn, bekleme 60 sn.
/// </summary>
public class ReverseSkill : SkillBase
{
    [Tooltip("1., 2. ve 3. seviyede ters kontrol süresi (saniye)")]
    [SerializeField] float[] durations = { 2f, 5f, 20f };
    [Tooltip("1., 2. ve 3. seviyede bekleme süresi (saniye)")]
    [SerializeField] float[] cooldowns = { 120f, 90f, 60f };

    [Networked] public TickTimer Cooldown { get; set; }

    public override SkillId Id => SkillId.Reverse;
    public override bool HasActiveAbility => Level >= 1;
    public override int ActionButtonCount => 1;
    public override float CooldownRemaining => Runner != null ? Cooldown.RemainingTime(Runner) ?? 0f : 0f;

    int LevelIndex => Mathf.Clamp(Level - 1, 0, 2);

    public override void ServerActivate(float aimX)
    {
        if (!HasStateAuthority || !HasActiveAbility || !Cooldown.ExpiredOrNotRunning(Runner)) return;

        Cooldown = TickTimer.CreateFromSeconds(Runner, cooldowns[Mathf.Min(LevelIndex, cooldowns.Length - 1)]);
        float seconds = durations[Mathf.Min(LevelIndex, durations.Length - 1)];

        foreach (KeeperController k in KeeperController.All)
            if (k != Keeper) k.ServerReverse(seconds);
    }
}
