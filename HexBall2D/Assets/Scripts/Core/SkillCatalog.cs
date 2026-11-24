using System;
using System.Collections.Generic;

/// <summary>
/// Yeteneklerin görünen adları. Deste menüsündeki yazılar (DeckUI) bu adların üstüne yazar,
/// böylece dükkanda ve butonlarda senin menüde yazdığın isimler görünür.
/// </summary>
public static class SkillCatalog
{
    static readonly Dictionary<SkillId, string> _names = new Dictionary<SkillId, string>
    {
        { SkillId.PowerShot, "Hızlı Şut" },
        { SkillId.InvisibleShot, "Görünmez Şut" },
        { SkillId.Speed, "Hızlı Hareket" },
        { SkillId.Teleport, "Işınlanma" },
        { SkillId.Wall, "Duvar Koyma" },
        { SkillId.Lifesteal, "Can Çalma" },
        { SkillId.Paint, "Ekran Boyama" },
        { SkillId.Disc, "Sabitleyen Disk" },
        { SkillId.MultiShot, "Çoğalan Şut" },
        { SkillId.BallSize, "Top Boyutu" },
        { SkillId.Curve, "Falso" },
        { SkillId.GoldenBall, "Altın Top" },
        { SkillId.Decoy, "Yem Top" },
        { SkillId.Mirror, "Ayna" },
        { SkillId.Magnet, "Mıknatıs Eldiven" },
        { SkillId.SecondChance, "İkinci Şans" },
        { SkillId.Reverse, "Ters Kontrol" },
        { SkillId.Silence, "Susturma" },
        { SkillId.Cleanse, "Arındır" },
        { SkillId.Telekinesis, "Telekinezi" },
        { SkillId.Bounty, "Ödül Avcısı" },
        { SkillId.Ceasefire, "Ateşkes" },
        { SkillId.SoulSwap, "Ruh Takası" },
        { SkillId.Kamikaze, "Kamikaze Şut" },
        { SkillId.UpsideDown, "Ters Dünya" },
        { SkillId.Curse, "Lanet" },
        { SkillId.PuppetMaster, "Kukla Ustası" },
        { SkillId.HotBomb, "Sıcak Bomba" },
        { SkillId.Sharpshooter, "Keskin Nişancı" },
        { SkillId.Gift, "Tuzaklı Hediye" },
        { SkillId.Push, "Shinra Tensei" },
    };

    public static string NameOf(SkillId id) => _names.TryGetValue(id, out string n) ? n : id.ToString();

    public static void SetName(SkillId id, string name)
    {
        if (!string.IsNullOrWhiteSpace(name)) _names[id] = name.Trim().Trim('"');
    }

    public static bool IsValid(int id) => Enum.IsDefined(typeof(SkillId), id);
}
