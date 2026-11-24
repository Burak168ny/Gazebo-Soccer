using Fusion;
using UnityEngine;

/// <summary>
/// SICAK BOMBA (butonla)
/// Basınca sahadaki gerçek topa bomba kurulur, topun üstünde geri sayım görünür (herkes görür).
/// Sayaç 0 olduğu an top kimin elindeyse, kimsede değilse topa EN SON kim vurduysa
/// (şut atan / aynadan döndüren / telekinezi yapan) o kişi gol yemiş sayılır:
/// Seviye 1: 5 can, bekleme 90 sn.
/// Seviye 2: 10 can, bekleme 60 sn.
/// Seviye 3: 15 can, bekleme 30 sn.
/// Bombayı kuran da dahil herkes patlayabilir! Topa henüz kimse dokunmadıysa kimse yanmaz.
/// Topta zaten bomba varsa yenisi kurulamaz.
/// </summary>
public class HotBombSkill : SkillBase
{
    [Tooltip("1., 2. ve 3. seviyede patlayınca kaybedilen can")]
    [SerializeField] int[] damages = { 5, 10, 15 };
    [Tooltip("1., 2. ve 3. seviyede bekleme süresi (saniye)")]
    [SerializeField] float[] cooldowns = { 90f, 60f, 30f };
    [Tooltip("Bombanın geri sayım süresi (saniye)")]
    [SerializeField] float fuseSeconds = 10f;

    [Networked] public TickTimer Cooldown { get; set; }

    public override SkillId Id => SkillId.HotBomb;
    public override bool HasActiveAbility => Level >= 1;
    public override int ActionButtonCount => 1;
    public override float CooldownRemaining => Runner != null ? Cooldown.RemainingTime(Runner) ?? 0f : 0f;
    public override bool CanActivateNow => FindBall() != null;

    int LevelIndex => Mathf.Clamp(Level - 1, 0, 2);

    public override void ServerActivate(float aimX)
    {
        if (!HasStateAuthority || !HasActiveAbility || !Cooldown.ExpiredOrNotRunning(Runner)) return;

        MatchManager mm = MatchManager.Instance;
        if (mm == null || mm.Phase != MatchPhase.Playing) return;

        Ball ball = FindBall();
        if (ball == null) return;

        Cooldown = TickTimer.CreateFromSeconds(Runner, cooldowns[Mathf.Min(LevelIndex, cooldowns.Length - 1)]);
        ball.ServerArmBomb(Keeper.Slot, damages[Mathf.Min(LevelIndex, damages.Length - 1)], fuseSeconds);
    }

    /// Bombasız gerçek top (sahte / çoğalan kopya değil).
    static Ball FindBall()
    {
        foreach (Ball b in Ball.All)
            if (!b.IsDecoy && !b.IsSplitCopy && !b.HasBomb) return b;
        return null;
    }
}
