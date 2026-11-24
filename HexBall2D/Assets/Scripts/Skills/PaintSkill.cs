using Fusion;
using UnityEngine;

/// <summary>
/// EKRAN BOYAMA (butonla)
/// Basınca tüm rakiplerin ekranının orta bölgesinde rastgele bir yere rengarenk bir boya lekesi çıkar.
/// Seviye 1: küçük leke. Seviye 2: 2 katı. Seviye 3: 4 katı. Bekleme 3 dk.
/// Boya sadece rakiplerin kendi ekranında; oyunu etkilemez, görüşü kapatır.
/// </summary>
public class PaintSkill : SkillBase
{
    [SerializeField] float cooldownSeconds = 180f;
    [Tooltip("1., 2. ve 3. seviyede lekenin boyutu (ekran yüksekliğine oran: 0.06 = %6)")]
    [SerializeField] float[] sizes = { 0.06f, 0.12f, 0.24f };
    [Tooltip("Boya ekranda kaç saniye kalır (son 1 saniyede solar). 0 = maç bitene kadar kalır")]
    [SerializeField] float durationSeconds = 8f;

    [Networked] public TickTimer Cooldown { get; set; }

    public override SkillId Id => SkillId.Paint;
    public override bool HasActiveAbility => Level >= 1;
    public override int ActionButtonCount => 1;
    public override float CooldownRemaining => Runner != null ? Cooldown.RemainingTime(Runner) ?? 0f : 0f;

    public override void ServerActivate(float aimX)
    {
        if (!HasStateAuthority || !HasActiveAbility || !Cooldown.ExpiredOrNotRunning(Runner)) return;

        Cooldown = TickTimer.CreateFromSeconds(Runner, cooldownSeconds);
        float size = sizes[Mathf.Clamp(Level - 1, 0, sizes.Length - 1)];
        RPC_Splat(Keeper.Slot, size, durationSeconds);
    }

    /// Herkese gider; sadece hayattaki rakipler boyayı görür.
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_Splat(int casterSlot, float size, float duration)
    {
        int me = NetworkBootstrap.LocalSlot;
        if (me < 0 || me == casterSlot) return;

        MatchManager mm = MatchManager.Instance;
        if (mm != null && !mm.IsAlive(me)) return;

        PaintOverlay.Splat(size, duration);
    }
}
