using Fusion;
using UnityEngine;

/// <summary>
/// ARINDIR (butonla)
/// Basınca üstündeki tüm kötü etkiler anında kalkar: hareketsiz kalma (disk / yem top),
/// susturma, ters kontrol, ters dünya (kamera), lanet, kukla olma ve ekrandaki boya. Susturulmuşken ve donmuşken de kullanılabilir.
/// Buton sadece üstünde kötü bir etki varken açılır (boşa harcanmasın).
/// Seviye 1: 1 kullanım, bekleme 120 sn.
/// Seviye 2: 1 kullanım, bekleme 90 sn.
/// Seviye 3: art arda 2 kullanım, ikisi de bitince bekleme 60 sn.
/// </summary>
public class CleanseSkill : SkillBase
{
    [Tooltip("1., 2. ve 3. seviyede bekleme süresi (saniye)")]
    [SerializeField] float[] cooldowns = { 120f, 90f, 60f };
    [Tooltip("1., 2. ve 3. seviyede bekleme başlamadan önce kaç kez kullanılabilir")]
    [SerializeField] int[] uses = { 1, 1, 2 };

    [Networked] public TickTimer Cooldown { get; set; }
    [Networked] public int Used { get; set; }   // bu turda kaç kez kullanıldı

    public override SkillId Id => SkillId.Cleanse;
    public override bool HasActiveAbility => Level >= 1;
    public override int ActionButtonCount => 1;
    public override float CooldownRemaining => Runner != null ? Cooldown.RemainingTime(Runner) ?? 0f : 0f;
    public override bool UsableWhileSilenced => true;

    int LevelIndex => Mathf.Clamp(Level - 1, 0, 2);
    int MaxUses => Mathf.Max(1, uses[Mathf.Min(LevelIndex, uses.Length - 1)]);

    /// Arayüz: sadece kötü bir etki varken basılabilir.
    public override bool CanActivateNow =>
        Keeper != null && (Keeper.IsStunned || Keeper.IsSilenced || Keeper.IsReversed || Keeper.IsFlipped || Keeper.IsPuppeted ||
                           (MatchManager.Instance != null && MatchManager.Instance.IsCursed(Keeper.Slot)) ||
                           (Keeper.HasInputAuthority && PaintOverlay.HasSplats));

    public override void ServerActivate(float aimX)
    {
        if (!HasStateAuthority || !HasActiveAbility || !Cooldown.ExpiredOrNotRunning(Runner)) return;

        Used++;
        if (Used >= MaxUses) // haklar bitti: bekleme başlar
        {
            Used = 0;
            Cooldown = TickTimer.CreateFromSeconds(Runner, cooldowns[Mathf.Min(LevelIndex, cooldowns.Length - 1)]);
        }
        Keeper.ServerCleanse();
    }
}
