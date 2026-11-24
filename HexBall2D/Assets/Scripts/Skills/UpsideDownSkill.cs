using Fusion;
using UnityEngine;

/// <summary>
/// TERS DÜNYA (butonla)
/// Basınca hayattaki TÜM rakiplerin kamerası baş aşağı döner (180°). Kontroller değişmez,
/// sadece ekran ters görünür. Arındır ile temizlenebilir.
/// Seviye 1: 3 sn, bekleme 90 sn.
/// Seviye 2: 5 sn, bekleme 60 sn.
/// Seviye 3: 7 sn, bekleme 30 sn.
/// </summary>
public class UpsideDownSkill : SkillBase
{
    [Tooltip("1., 2. ve 3. seviyede kameranın ters kalma süresi (saniye)")]
    [SerializeField] float[] durations = { 3f, 5f, 7f };
    [Tooltip("1., 2. ve 3. seviyede bekleme süresi (saniye)")]
    [SerializeField] float[] cooldowns = { 90f, 60f, 30f };

    [Networked] public TickTimer Cooldown { get; set; }

    public override SkillId Id => SkillId.UpsideDown;
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
            if (k != Keeper) k.ServerFlipCamera(seconds);
    }
}
