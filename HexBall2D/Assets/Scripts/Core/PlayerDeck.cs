using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Oyuncunun destesi (en fazla 6 yetenek, sırası önemli). Cihaza kaydedilir.
/// Ağdan tek bir sayı (int) olarak gönderilir: her yetenek 5 bit.
/// </summary>
public static class PlayerDeck
{
    public const int Size = 6;
    const string PrefKey = "HexBall.Deck";
    const int BitsPerSlot = 5;
    public const int SetFlag = 1 << 30; // "deste gönderildi" işareti (boş deste ile ayırmak için)

    static readonly List<SkillId> _skills = new List<SkillId>();
    static bool _loaded;

    /// Hiç deste kaydedilmemişse başlangıç destesi.
    static readonly SkillId[] DefaultDeck =
    {
        SkillId.PowerShot, SkillId.InvisibleShot, SkillId.Speed,
        SkillId.Teleport, SkillId.Wall, SkillId.Lifesteal,
    };

    public static IReadOnlyList<SkillId> Skills { get { Load(); return _skills; } }
    public static int Count { get { Load(); return _skills.Count; } }
    public static bool IsFull => Count >= Size;
    public static bool Contains(SkillId id) { Load(); return _skills.Contains(id); }

    public static bool Add(SkillId id)
    {
        Load();
        if (_skills.Count >= Size || _skills.Contains(id)) return false;
        _skills.Add(id);
        Save();
        return true;
    }

    public static void RemoveAt(int index)
    {
        Load();
        if (index < 0 || index >= _skills.Count) return;
        _skills.RemoveAt(index);
        Save();
    }

    // ---------------- Ağ için kodlama ----------------

    public static int Encode()
    {
        Load();
        int code = SetFlag;
        for (int i = 0; i < _skills.Count && i < Size; i++)
            code |= ((int)_skills[i] + 1) << (i * BitsPerSlot);
        return code;
    }

    /// Kodun i. slotundaki yetenek (-1 = boş).
    public static int SlotOf(int code, int index) => ((code >> (index * BitsPerSlot)) & 31) - 1;

    /// Host tarafında: gelen kod geçerli mi (bilinmeyen ya da tekrar eden yetenek yok)?
    public static bool IsValidCode(int code)
    {
        if ((code & SetFlag) == 0) return false;
        int seen = 0;
        for (int i = 0; i < Size; i++)
        {
            int id = SlotOf(code, i);
            if (id < 0) continue;
            if (!SkillCatalog.IsValid(id) || (seen & (1 << id)) != 0) return false;
            seen |= 1 << id;
        }
        return true;
    }

    // ---------------- Kayıt ----------------

    static void Load()
    {
        if (_loaded) return;
        _loaded = true;
        _skills.Clear();

        string saved = PlayerPrefs.GetString(PrefKey, null);
        if (string.IsNullOrEmpty(saved))
        {
            _skills.AddRange(DefaultDeck);
            return;
        }
        if (saved == "-") return; // bilerek boş bırakılmış deste

        foreach (string part in saved.Split(','))
            if (int.TryParse(part, out int id) && SkillCatalog.IsValid(id) && !_skills.Contains((SkillId)id) && _skills.Count < Size)
                _skills.Add((SkillId)id);
    }

    static void Save()
    {
        if (_skills.Count == 0)
        {
            PlayerPrefs.SetString(PrefKey, "-");
        }
        else
        {
            var parts = new string[_skills.Count];
            for (int i = 0; i < _skills.Count; i++) parts[i] = ((int)_skills[i]).ToString();
            PlayerPrefs.SetString(PrefKey, string.Join(",", parts));
        }
        PlayerPrefs.Save();
    }
}
