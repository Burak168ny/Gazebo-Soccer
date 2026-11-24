using UnityEngine;

/// <summary>
/// HIZLI HAREKET (pasif, buton yok)
/// Keeper'ın hareket hızı = normal hız + normal hız × oran.
/// Varsayılan: Seviye 1 +%30, Seviye 2 +%50, Seviye 3 +%100 (2 katı). Üst seviye alttakinin yerine geçer.
/// </summary>
public class SpeedSkill : SkillBase
{
    [Tooltip("1., 2. ve 3. seviyede hıza eklenecek oran (0.3 = %30 daha hızlı, 1 = 2 katı)")]
    [SerializeField] float[] speedBonusRatio = { 0.3f, 0.5f, 1f };

    public override SkillId Id => SkillId.Speed;

    /// Hareket hızının çarpanı (alınmadıysa 1).
    public float SpeedMultiplier => Level > 0 ? 1f + speedBonusRatio[Mathf.Min(Level, speedBonusRatio.Length) - 1] : 1f;
}
