using Fusion;
using UnityEngine;

/// <summary>
/// DONDURAN DİSK (joystick ile)
/// 1. seviye alınınca joystick görünür. Joystick'i İLERİ yarıma doğru çekip bırakınca
/// o yöne top gibi seken bir disk fırlar. Değdiği rakipler bir süre hareket edemez.
/// Seviye 1: 4 sekme, 1 sn donma, bekleme 60 sn.
/// Seviye 2: 5 sekme, 2 sn donma, bekleme 30 sn.
/// Seviye 3: 8 sekme, 3 sn donma, bekleme 15 sn.
/// Disk rakiplerin içinden geçer (her rakibi bir kez dondurur); son sekmeden sonra yok olur.
/// </summary>
public class DiscSkill : SkillBase
{
    [Tooltip("Disk prefab'ı (NetworkObject + NetworkTransform + Disc)")]
    [SerializeField] NetworkPrefabRef discPrefab;
    [SerializeField] float speed = 14f; // topun normal şut hızıyla aynı
    [SerializeField] int[] bounces = { 4, 5, 8 };
    [SerializeField] float[] stunSeconds = { 1f, 2f, 3f };
    [SerializeField] float[] cooldowns = { 60f, 30f, 15f };

    [Networked] public TickTimer Cooldown { get; set; }

    public override SkillId Id => SkillId.Disc;
    public override float CooldownRemaining => Runner != null ? Cooldown.RemainingTime(Runner) ?? 0f : 0f;
    public bool CanFire => Level >= 1 && CooldownRemaining <= 0f;

    // Deste sistemi bu yeteneğe bir joystick atar
    public override bool UsesJoystick => true;
    public override bool JoystickActive => Level >= 1;
    public override bool CanUseJoystick => CanFire;
    public override void OnJoystickReleased(Vector2 localDir) => RPC_Fire(localDir);

    int LevelIndex => Mathf.Clamp(Level - 1, 0, 2);

    /// Joystick bırakılınca client çağırır. localDir: oyuncunun ekranına göre yön (y > 0 = ileri).
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_Fire(Vector2 localDir)
    {
        if (Level < 1 || !Cooldown.ExpiredOrNotRunning(Runner)) return;
        if (Keeper.IsSilenced) return; // Susturma
        if (localDir.y <= 0f || localDir.sqrMagnitude < 0.0001f) return; // sadece ileri yarım

        MatchManager mm = MatchManager.Instance;
        ArenaLayout arena = ArenaLayout.Instance;
        if (mm == null || arena == null || mm.Phase != MatchPhase.Playing) return;

        localDir.Normalize();
        int slot = Keeper.Slot;
        Vector2 dir = (arena.Right(slot) * localDir.x + arena.Inward(slot) * localDir.y).normalized;
        Vector2 start = Keeper.WorldPosition + dir * (Keeper.Radius + 0.3f);

        Cooldown = TickTimer.CreateFromSeconds(Runner, cooldowns[Mathf.Min(LevelIndex, cooldowns.Length - 1)]);

        int b = bounces[Mathf.Min(LevelIndex, bounces.Length - 1)];
        float stun = stunSeconds[Mathf.Min(LevelIndex, stunSeconds.Length - 1)];
        Runner.Spawn(discPrefab, (Vector3)start, Quaternion.identity, null,
            (r, o) => o.GetComponent<Disc>().Init(slot, dir * speed, b, stun));
    }
}
