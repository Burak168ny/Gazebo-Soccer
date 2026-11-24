using Fusion;
using UnityEngine;

/// <summary>
/// TELEKİNEZİ (buton + joystick)
/// Top kimsenin elinde değilken butona basınca yetenek joystick'i açılır (tekrar basınca kapanır).
/// Joystick'i bir yöne çekip bırakınca top, sahada nerede olursa olsun, hızı ve etkileri sıfırlanıp
/// OLDUĞU YERDEN gösterdiğin yöne fırlar (her yön olur). Gol olursa senin golün sayılır.
/// Seviye 1: Normal şut hızında, bekleme 120 sn.
/// Seviye 2: 1.5 kat hızlı, bekleme 90 sn.
/// Seviye 3: 1.5 kat hızlı + şut yeteneklerin de uygulanır (Güçlü Şut zinciri, Çoğalan Şut, Yem Top,
///           Görünmez Şut, Falso...), bekleme 60 sn.
/// </summary>
public class TelekinesisSkill : SkillBase
{
    [Tooltip("1., 2. ve 3. seviyede bekleme süresi (saniye)")]
    [SerializeField] float[] cooldowns = { 120f, 90f, 60f };
    [Tooltip("1., 2. ve 3. seviyede fırlatma hız çarpanı (normal şut hızına göre)")]
    [SerializeField] float[] speedMultipliers = { 1f, 1.5f, 1.5f };

    [Networked] public TickTimer Cooldown { get; set; }

    bool _aiming; // sadece bu bilgisayarda: butona basıldı, joystick açık

    public override SkillId Id => SkillId.Telekinesis;
    public override bool HasActiveAbility => Level >= 1;
    public override int ActionButtonCount => 1;
    public override float CooldownRemaining => Runner != null ? Cooldown.RemainingTime(Runner) ?? 0f : 0f;
    /// Buton sadece serbest top varken açılır.
    public override bool CanActivateNow => Keeper != null && FindTarget(Keeper.WorldPosition) != null;

    // Joystick: butona basınca açılır
    public override bool UsesJoystick => true;
    public override bool JoystickAnyDirection => true;
    public override bool JoystickActive => _aiming && Level >= 1 && CooldownRemaining <= 0f;
    public override bool CanUseJoystick => CanActivateNow && (Keeper == null || !Keeper.IsSilenced);

    int LevelIndex => Mathf.Clamp(Level - 1, 0, 2);

    public override bool HandleLocalAction(int action)
    {
        if (!HasInputAuthority) return false;
        _aiming = !_aiming;
        return true;
    }

    public override void OnJoystickReleased(Vector2 localDir)
    {
        _aiming = false;
        RPC_Fling(localDir);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_Fling(Vector2 localDir)
    {
        if (Level < 1 || !Cooldown.ExpiredOrNotRunning(Runner) || Keeper.IsSilenced) return;
        if (localDir.sqrMagnitude < 0.0001f) return;

        MatchManager mm = MatchManager.Instance;
        ArenaLayout arena = ArenaLayout.Instance;
        if (mm == null || arena == null || mm.Phase != MatchPhase.Playing) return;

        Ball target = FindTarget(Keeper.WorldPosition);
        if (target == null) return; // top birinin elinde: hak harcanmaz

        localDir.Normalize();
        int slot = Keeper.Slot;
        Vector2 dir = arena.Right(slot) * localDir.x + arena.Inward(slot) * localDir.y;

        Cooldown = TickTimer.CreateFromSeconds(Runner, cooldowns[Mathf.Min(LevelIndex, cooldowns.Length - 1)]);
        float mult = speedMultipliers[Mathf.Min(LevelIndex, speedMultipliers.Length - 1)];
        target.ServerTelekinesis(Keeper, dir, localDir.x, mult, Level >= MaxLevel);
    }

    /// Kimsenin elinde olmayan en yakın gerçek top (sahte / çoğalan kopya / geri dönen değil).
    public static Ball FindTarget(Vector2 from)
    {
        Ball best = null;
        float bestDist = float.MaxValue;
        foreach (Ball b in Ball.All)
        {
            if (b.IsHeld || b.IsDecoy || b.IsSplitCopy || b.ReturnToSlot >= 0) continue;
            float d = ((Vector2)b.transform.position - from).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = b; }
        }
        return best;
    }
}
