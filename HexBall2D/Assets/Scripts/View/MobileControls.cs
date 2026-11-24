using UnityEngine;

/// <summary>
/// Mobil kontroller (Joystick Pack). Klavyeye EK olarak çalışır.
/// - Hareket joystick'i: keeper'ı hareket ettirir.
/// - Şut joystick'i: İLERİ yarıma doğru çekip bırakınca top o yöne fırlar (top elindeyse).
/// Joystick yönleri oyuncunun kendi ekranına göredir (yukarı = ileri).
/// </summary>
public class MobileControls : MonoBehaviour
{
    public static MobileControls Instance { get; private set; }

    [SerializeField] Joystick moveJoystick;
    [SerializeField] Joystick throwJoystick;
    [Tooltip("Şut joystick'i en az bu kadar çekilmiş olmalı (0-1)")]
    [SerializeField, Range(0f, 1f)] float minThrowPull = 0.3f;
    [Tooltip("Kapalıysa joystick'ler sadece telefonda görünür")]
    [SerializeField] bool showOnDesktop = true;

    bool _visible;
    Vector2 _lastThrowDir;
    bool _throwDragging;

    /// Şut joystick'i çekiliyorsa o anki yönü (ileri yarımdaysa), değilse sıfır. Nişan çizgisi için.
    public Vector2 ThrowAimPreview => _visible && _throwDragging && _lastThrowDir.y > 0.05f ? _lastThrowDir.normalized : Vector2.zero;

    /// Hareket joystick'inin yönü (görünmüyorsa sıfır).
    public Vector2 Move => _visible && moveJoystick != null ? moveJoystick.Direction : Vector2.zero;

    void Awake()
    {
        Instance = this;
        SetVisible(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        KeeperController k = KeeperController.Local;
        MatchManager mm = MatchManager.Instance;
        bool playing = k != null && k.Object != null && k.Object.IsValid
                       && mm != null && mm.Phase == MatchPhase.Playing && mm.IsAlive(k.Slot);
        bool platformOk = showOnDesktop || Application.isMobilePlatform;
        SetVisible(playing && platformOk);
        if (!_visible || throwJoystick == null) return;

        Vector2 d = throwJoystick.Direction;
        if (d.sqrMagnitude > 0.0001f)
        {
            _lastThrowDir = d;      // çekiliyor
            _throwDragging = true;
        }
        else if (_throwDragging)    // bırakıldı
        {
            _throwDragging = false;
            bool forward = _lastThrowDir.y > 0.05f; // arka yarım sayılmaz
            if (forward && _lastThrowDir.magnitude >= minThrowPull && NetworkBootstrap.Instance != null)
                NetworkBootstrap.Instance.QueueThrow(_lastThrowDir.normalized);
            _lastThrowDir = Vector2.zero;
        }
    }

    void SetVisible(bool visible)
    {
        if (visible == _visible && moveJoystick != null && moveJoystick.gameObject.activeSelf == visible) return;
        _visible = visible;
        if (moveJoystick != null) moveJoystick.gameObject.SetActive(visible);
        if (throwJoystick != null) throwJoystick.gameObject.SetActive(visible);
        if (!visible) _throwDragging = false;
    }
}
