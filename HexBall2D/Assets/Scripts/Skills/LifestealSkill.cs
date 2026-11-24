using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// CAN ÇALMA (Lifesteal)
/// Seviye 1: Senin attığın her golde +1 can.
/// Seviye 2: Senin attığın her golde +2 can.
/// Seviye 3: (+2 can devam eder) Buton: hayattaki TÜM rakiplerin 1 can kaybeder,
///           sen de can kaybettirdiğin rakip sayısı kadar can kazanırsın. Bekleme 2 dk.
/// "Gol atan" = topa en son vuran (fırlatan) oyuncu. Kendi kalene atarsan sayılmaz.
/// </summary>
public class LifestealSkill : SkillBase
{
    [Tooltip("1., 2. ve 3. seviyede attığın her golde kazanacağın can")]
    [SerializeField] int[] livesPerGoal = { 1, 2, 2 };
    [SerializeField] float cooldownSeconds = 120f;

    [Networked] public TickTimer Cooldown { get; set; }

    public override SkillId Id => SkillId.Lifesteal;
    public override bool HasActiveAbility => Level >= MaxLevel;
    public override int ActionButtonCount => 1;
    public override float CooldownRemaining => Runner != null ? Cooldown.RemainingTime(Runner) ?? 0f : 0f;

    public int LivesPerGoal => Level > 0 ? livesPerGoal[Mathf.Min(Level, livesPerGoal.Length) - 1] : 0;

    static readonly List<int> _targets = new List<int>();

    /// Seviye 3 butonu: tüm rakiplerden 1'er can çal.
    public override void ServerActivate(float aimX)
    {
        if (!HasStateAuthority || !HasActiveAbility || !Cooldown.ExpiredOrNotRunning(Runner)) return;

        MatchManager mm = MatchManager.Instance;
        if (mm == null || mm.Phase != MatchPhase.Playing) return;

        int me = Keeper.Slot;
        _targets.Clear();
        for (int s = 0; s < HexArena.SlotCount; s++)
            if (s != me && mm.IsAlive(s)) _targets.Add(s);
        if (_targets.Count == 0) return;

        Cooldown = TickTimer.CreateFromSeconds(Runner, cooldownSeconds);

        mm.ServerHeal(me, _targets.Count);              // önce kendini doldur
        foreach (int s in _targets) mm.ServerDamage(s, 1, me); // sonra herkes 1 can kaybeder (elenen olabilir)
    }
}
