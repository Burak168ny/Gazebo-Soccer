using Fusion;
using UnityEngine;

/// <summary>
/// Geçici arayüz (Unity IMGUI): ana menü, eşleşme, bekleme ekranı, geri sayım, ping.
/// Oynanış oturunca gerçek UI (UI Toolkit/Canvas) ile değiştirilecek.
/// </summary>
public class GameUI : MonoBehaviour
{
    GUIStyle _title, _big, _text, _center, _button;

    void OnGUI()
    {
        const float refHeight = 720f;
        float scale = Screen.height / refHeight;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        float w = Screen.width / scale;
        float h = refHeight;
        InitStyles();

        NetworkBootstrap boot = NetworkBootstrap.Instance;
        if (boot == null) return;

        NetworkRunner runner = boot.Runner;
        MatchManager mm = MatchManager.Instance;

        // Host değişiyor
        if (NetworkBootstrap.IsResuming)
        {
            GUI.Label(new Rect(0, h * 0.4f, w, 50), "Host değişiyor...", _big);
            return;
        }

        // Ana menü
        if (runner == null)
        {
            if (DeckUI.Instance != null && DeckUI.Instance.IsOpen) // deste menüsü açıkken sadece kapatma butonu
            {
                if (GUI.Button(new Rect(w / 2 - 100, h - 70, 200, 52), "TAMAM", _button)) DeckUI.Instance.Close();
                return;
            }

            GUI.Label(new Rect(0, h * 0.25f, w, 80), "HEX BALL", _title);
            if (GUI.Button(new Rect(w / 2 - 120, h * 0.5f, 240, 64), "OYNA", _button))
                boot.Play();
            if (DeckUI.Instance != null && GUI.Button(new Rect(w / 2 - 120, h * 0.5f + 74, 240, 56), "DESTE", _button))
                DeckUI.Instance.Open();
            if (!string.IsNullOrEmpty(NetworkBootstrap.LastMessage))
                GUI.Label(new Rect(0, h * 0.5f + 150, w, 40), NetworkBootstrap.LastMessage, _center);
            return;
        }

        // Bağlanıyor / oda aranıyor
        if (boot.IsConnecting || mm == null)
        {
            GUI.Label(new Rect(0, h * 0.4f, w, 50), "Eşleşme aranıyor...", _big);
            if (GUI.Button(new Rect(w / 2 - 90, h * 0.55f, 180, 50), "İptal", _button)) boot.Leave();
            return;
        }

        switch (mm.Phase)
        {
            case MatchPhase.Waiting:
                GUI.Label(new Rect(0, h * 0.35f, w, 50), "Oyuncular bekleniyor", _big);
                GUI.Label(new Rect(0, h * 0.35f + 60, w, 60), $"{mm.PlayerCount} / {mm.RequiredPlayers}", _title);
                if (GUI.Button(new Rect(w / 2 - 90, h * 0.62f, 180, 50), "Ayrıl", _button)) boot.Leave();
                break;

            case MatchPhase.Countdown:
                GUI.Label(new Rect(0, h * 0.35f, w, 120), mm.CountdownRemaining.ToString(), _title);
                break;

            case MatchPhase.Playing:
                int me = NetworkBootstrap.LocalSlot;
                if (me >= 0 && !mm.IsAlive(me))
                    GUI.Label(new Rect(0, h * 0.4f, w, 120), "ELENDİN", _title);
                break;

            case MatchPhase.Finished:
                GUI.Label(new Rect(0, h * 0.35f, w, 120), NetworkBootstrap.LastMessage, _title);
                if (GUI.Button(new Rect(w / 2 - 110, h * 0.6f, 220, 50), "Menüye dön", _button)) boot.Leave();
                break;
        }

        // Ping (gecikme testi için)
        if (runner.IsRunning)
        {
            string role = runner.IsServer ? "HOST" : "CLIENT";
            int ping = runner.IsServer ? 0 : Mathf.RoundToInt((float)(runner.GetPlayerRtt(runner.LocalPlayer) * 1000.0));
            GUI.Label(new Rect(12, 8, 300, 30), $"{role}  |  ping: {ping} ms", _text);
        }
    }

    void InitStyles()
    {
        if (_title != null) return;
        _title = new GUIStyle(GUI.skin.label) { fontSize = 64, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        _big = new GUIStyle(GUI.skin.label) { fontSize = 34, alignment = TextAnchor.MiddleCenter };
        _text = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleLeft };
        _center = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
        _button = new GUIStyle(GUI.skin.button) { fontSize = 28 };
        _title.normal.textColor = _big.normal.textColor = _text.normal.textColor = Color.white;
        _center.normal.textColor = new Color(1f, 0.6f, 0.5f);
    }
}
