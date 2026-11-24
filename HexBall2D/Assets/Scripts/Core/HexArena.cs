using UnityEngine;

/// Oyun genelindeki sabitler. Arenanın geometrisi sahnedeki ArenaLayout'tan gelir.
public static class HexArena
{
    public const int SlotCount = 6;

    public static readonly Color[] SlotColors =
    {
        new Color(0.93f, 0.26f, 0.26f), // 0 kırmızı
        new Color(0.98f, 0.60f, 0.15f), // 1 turuncu
        new Color(0.97f, 0.87f, 0.22f), // 2 sarı
        new Color(0.30f, 0.80f, 0.35f), // 3 yeşil
        new Color(0.25f, 0.55f, 0.95f), // 4 mavi
        new Color(0.68f, 0.38f, 0.92f), // 5 mor
    };

    public static readonly string[] SlotNames = { "Kırmızı", "Turuncu", "Sarı", "Yeşil", "Mavi", "Mor" };
}
