using Fusion;
using UnityEngine;

/// <summary>
/// İKİNCİ ŞANS (pasif, bekleme süreli)
/// Canın 1'in altına düşecekken kalkan devreye girer: canın 1'de kalır (her engellenen gol 1 hak yer).
/// Seviye 1: 3 hak. Haklar bitince bekleme 120 sn.
/// Seviye 2: 5 hak. Haklar bitince +5 can, bekleme 90 sn.
/// Seviye 3: 10 hak. Haklar bitince +10 can, bekleme 60 sn.
/// Bekleme sırasında kalkan çalışmaz. Durum dükkan arayüzündeki yazıda görünür.
/// </summary>
public class SecondChanceSkill : SkillBase
{
    [Tooltip("1., 2. ve 3. seviyede kaç gol engellenir")]
    [SerializeField] int[] charges = { 3, 5, 10 };
    [Tooltip("1., 2. ve 3. seviyede haklar bitince eklenecek can")]
    [SerializeField] int[] bonusLives = { 0, 5, 10 };
    [Tooltip("1., 2. ve 3. seviyede haklar bitince bekleme süresi (saniye)")]
    [SerializeField] float[] cooldowns = { 120f, 90f, 60f };

    [Networked] public int Blocked { get; set; }          // bu turda engellenen gol sayısı
    [Networked] public TickTimer Cooldown { get; set; }

    public override SkillId Id => SkillId.SecondChance;
    public override float CooldownRemaining => Runner != null ? Cooldown.RemainingTime(Runner) ?? 0f : 0f;
    public override bool HasPassiveStatus => true;
    public override bool ShowsPassiveStatus => Level > 0;

    int LevelIndex => Mathf.Clamp(Level - 1, 0, 2);
    int Charges => charges[Mathf.Min(LevelIndex, charges.Length - 1)];

    /// Arayüz: kalan hak (5, 4, 3, 2, 1) ya da bekleme sırasında kalan saniye ("45 sn").
    public override string PassiveStatus
    {
        get
        {
            int cd = Mathf.CeilToInt(CooldownRemaining);
            if (cd > 0) return cd + " sn";
            return (Charges - Blocked).ToString();
        }
    }

    /// Host: canı 1'in altına düşecek. Kalkan hazırsa true döner (can 1'de kalır) ve eklenecek canı verir.
    public bool ServerTrySave(out int bonus)
    {
        bonus = 0;
        if (!HasStateAuthority || Level <= 0 || !Cooldown.ExpiredOrNotRunning(Runner)) return false;

        Blocked++;
        if (Blocked >= Charges) // haklar bitti: can eklenir, bekleme başlar
        {
            Blocked = 0;
            bonus = bonusLives[Mathf.Min(LevelIndex, bonusLives.Length - 1)];
            Cooldown = TickTimer.CreateFromSeconds(Runner, cooldowns[Mathf.Min(LevelIndex, cooldowns.Length - 1)]);
        }
        return true;
    }
}
