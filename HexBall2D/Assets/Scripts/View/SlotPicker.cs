using System;
using UnityEngine;

/// <summary>
/// Ekranın ortasında oyuncu seçme listesi (IMGUI). Ateşkes, Ruh Takası gibi "rakip seç" yetenekleri kullanır.
/// Her karede OnGUI içinden çağrılır.
/// </summary>
public static class SlotPicker
{
    public const int None = -1, Cancel = -2;

    static GUIStyle _title, _button;

    /// Listeyi çizer. Seçilen oyuncunun numarasını, "Vazgeç"e basılırsa Cancel, hiçbir şeye basılmadıysa None döner.
    /// eligible: listede gösterilecek oyuncular.
    public static int Draw(string title, Func<int, bool> eligible)
    {
        MatchManager mm = MatchManager.Instance;
        if (mm == null) return Cancel;

        const float refHeight = 1080f;
        float scale = Screen.height / refHeight;
        Matrix4x4 oldMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        float w = Screen.width / scale;

        if (_title == null)
        {
            _title = new GUIStyle(GUI.skin.label) { fontSize = 44, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _button = new GUIStyle(GUI.skin.button) { fontSize = 40 };
        }

        float bw = 440f, bh = 90f, gap = 16f;
        int count = 0;
        for (int s = 0; s < HexArena.SlotCount; s++) if (eligible(s)) count++;
        float totalH = 80f + (count + 1) * (bh + gap);
        float x = (w - bw) * 0.5f, y = (refHeight - totalH) * 0.5f;

        GUI.Box(new Rect(x - 30f, y - 20f, bw + 60f, totalH + 30f), GUIContent.none);
        GUI.Label(new Rect(x, y, bw, 70f), title, _title);
        y += 80f;

        int result = None;
        Color old = GUI.backgroundColor;
        for (int s = 0; s < HexArena.SlotCount; s++)
        {
            if (!eligible(s)) continue;
            GUI.backgroundColor = HexArena.SlotColors[s];
            if (GUI.Button(new Rect(x, y, bw, bh), HexArena.SlotNames[s] + "  (" + mm.Lives[s] + " can)", _button))
                result = s;
            y += bh + gap;
        }
        GUI.backgroundColor = old;

        if (GUI.Button(new Rect(x, y, bw, bh), "Vazgeç", _button)) result = Cancel;

        GUI.matrix = oldMatrix;
        return result;
    }
}
