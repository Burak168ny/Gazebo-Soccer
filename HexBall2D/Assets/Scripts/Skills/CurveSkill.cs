using UnityEngine;

/// <summary>
/// FALSO (pasif)
/// Sola meyilli şut sola, sağa meyilli şut sağa kavis alır. Düz şut kavis almaz, rastgele hafif sağa ya da sola çapraz (dümdüz) gider.
/// Seviye 1: hafif kavis. Seviye 2: güçlü kavis.
/// Seviye 3: top neredeyse daire çizer ama dönüş gittikçe azalır, çember büyür; sonunda bir yere çarpar.
/// Kavis top bir şeye çarpınca (duvar, kale, keeper) biter.
/// </summary>
public class CurveSkill : SkillBase
{
    [Tooltip("1., 2. ve 3. seviyede başlangıç dönüş hızı (derece/saniye)")]
    [SerializeField] float[] turnRates = { 25f, 70f, 360f };
    [Tooltip("1., 2. ve 3. seviyede dönüşün azalma hızı (saniyede). Büyük = çember daha hızlı büyür. 0 = hiç azalmaz")]
    [SerializeField] float[] decays = { 0.3f, 0.3f, 1.2f };
    [Tooltip("Şut ne kadar yan olursa kavis tam güç olur (0-1). Joystick'te az yan = az kavis")]
    [SerializeField, Range(0.05f, 1f)] float fullSideAt = 0.5f;
    [Tooltip("Bundan daha düz şutlar 'düz şut' sayılır: kavis almaz, hafif çapraz gider")]
    [SerializeField, Range(0f, 0.5f)] float deadZone = 0.15f;
    [Tooltip("Düz şut rastgele sağa/sola kaç derece çapraz gider (0 = tam düz)")]
    [SerializeField, Range(0f, 45f)] float straightTiltDegrees = 8f;

    public override SkillId Id => SkillId.Curve;

    int LevelIndex => Mathf.Clamp(Level - 1, 0, 2);

    /// Host, şut anında çağırır. side: -1 sol ... +1 sağ (oyuncunun ekranına göre).
    /// turnSide: kavisin yönü (-1 sol, +1 sağ), rate: dönüş hızı (derece/sn), decay: azalma.
    /// Host: düz şutsa rastgele sağa/sola sapma açısı (derece, + = saat yönünün tersi), değilse 0.
    public float StraightTilt(float side)
    {
        if (Level <= 0 || Mathf.Abs(side) >= deadZone) return 0f;
        return Random.value < 0.5f ? -straightTiltDegrees : straightTiltDegrees;
    }

    public bool GetCurve(float side, out float turnSide, out float rate, out float decay)
    {
        turnSide = 0f;
        rate = 0f;
        decay = 0f;
        if (Level <= 0) return false;

        if (Mathf.Abs(side) < deadZone) return false; // düz şut kavis almaz (StraightTilt'e bak)

        turnSide = Mathf.Sign(side);
        float strength = Mathf.Clamp01(Mathf.Abs(side) / fullSideAt);
        rate = turnRates[Mathf.Min(LevelIndex, turnRates.Length - 1)] * strength;
        decay = decays[Mathf.Min(LevelIndex, decays.Length - 1)];
        return rate > 0f;
    }
}
