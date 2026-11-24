using UnityEngine;

/// <summary>
/// LANET (pasif)
/// Seni ELEYEN oyuncu (son golü atan ya da Can Çalma ile son canını alan) lanetlenir:
/// canı 1 kalana kadar düzenli aralıklarla 1'er 1'er azalır, sonra lanet biter.
/// Seviye 1: her 20 sn'de 1 can. Seviye 2: her 10 sn'de. Seviye 3: her 5 sn'de.
/// Lanetli oyuncu koyu mor görünür. Arındır laneti kaldırır.
/// Lanet durumu MatchManager'da tutulur (sen elenince de devam etsin diye).
/// </summary>
public class CurseSkill : SkillBase
{
    [Tooltip("1., 2. ve 3. seviyede lanetlinin kaç saniyede bir 1 can kaybedeceği")]
    [SerializeField] float[] intervals = { 20f, 10f, 5f };

    public override SkillId Id => SkillId.Curse;

    /// Lanet aralığı (0 = yetenek alınmamış).
    public float Interval => Level > 0 ? intervals[Mathf.Clamp(Level - 1, 0, intervals.Length - 1)] : 0f;
}
