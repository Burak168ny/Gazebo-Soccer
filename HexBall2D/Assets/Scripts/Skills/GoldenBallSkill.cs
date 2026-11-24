using Fusion;
using UnityEngine;

/// <summary>
/// ALTIN TOP (pasif)
/// Seviye 1: Attığın her 2 golden biri 2 can götürür (2., 4., 6. gol...).
/// Seviye 2: Attığın her gol 2 can götürür.
/// Seviye 3: Attığın her gol 3 can götürür.
/// "Gol atan" = topa en son vuran (fırlatan) oyuncu. Kendi kalene atarsan sayılmaz.
/// </summary>
public class GoldenBallSkill : SkillBase
{
    [Tooltip("1., 2. ve 3. seviyede güçlü golün götürdüğü can")]
    [SerializeField] int[] damagePerGoal = { 2, 2, 3 };
    [Tooltip("1., 2. ve 3. seviyede kaç golde bir güçlü gol olur (1 = her gol)")]
    [SerializeField] int[] everyNthGoal = { 2, 1, 1 };

    [Networked] public int GoalsScored { get; set; } // bu yetenek alındıktan sonra atılan goller

    public override SkillId Id => SkillId.GoldenBall;

    int LevelIndex => Mathf.Clamp(Level - 1, 0, 2);

    /// Host: gol attın, rakip kaç can kaybetsin?
    public int ServerDamageForGoal()
    {
        if (Level <= 0) return 1;
        GoalsScored++;
        int n = Mathf.Max(1, everyNthGoal[Mathf.Min(LevelIndex, everyNthGoal.Length - 1)]);
        return GoalsScored % n == 0 ? damagePerGoal[Mathf.Min(LevelIndex, damagePerGoal.Length - 1)] : 1;
    }
}
