using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ekran Boyama yeteneğinin görüntüsü: her şeyin üstünde duran bir Canvas'a rengarenk lekeler çizer.
/// Kendi kendini oluşturur, sahneye eklemen gerekmez. Tıklamaları engellemez.
/// </summary>
public class PaintOverlay : MonoBehaviour
{
    static PaintOverlay _instance;

    const float FadeSeconds = 1f;

    readonly List<(CanvasGroup group, float endTime)> _splats = new List<(CanvasGroup, float)>();
    RectTransform _root;

    /// size: ekran yüksekliğine oran. duration: 0 ise kalıcı.
    public static void Splat(float size, float duration)
    {
        if (_instance == null) _instance = Create();
        _instance.AddSplat(size, duration);
    }

    /// Ekranda boya var mı? (Arındır butonu için)
    public static bool HasSplats => _instance != null && _instance._splats.Count > 0;

    /// Arındır: tüm boyaları siler.
    public static void ClearAll()
    {
        if (_instance == null) return;
        foreach ((CanvasGroup group, float _) in _instance._splats)
            if (group != null) Destroy(group.gameObject);
        _instance._splats.Clear();
    }

    static PaintOverlay Create()
    {
        var go = new GameObject("PaintOverlay");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500; // diğer arayüzlerin üstünde
        var overlay = go.AddComponent<PaintOverlay>();
        overlay._root = (RectTransform)go.transform;
        return overlay;
    }

    void AddSplat(float size, float duration)
    {
        float diameter = Screen.height * size;

        // Ekranın orta bölgesinde (kenarlardan %25 içeride) rastgele bir nokta
        Vector2 pos = new Vector2(
            Random.Range(Screen.width * 0.25f, Screen.width * 0.75f),
            Random.Range(Screen.height * 0.25f, Screen.height * 0.75f));

        var splatGo = new GameObject("Splat", typeof(RectTransform), typeof(CanvasGroup));
        var rt = (RectTransform)splatGo.transform;
        rt.SetParent(_root, false);
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.anchoredPosition = pos;
        rt.sizeDelta = Vector2.one * diameter;

        var group = splatGo.GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        // Ortada büyük bir leke + etrafına farklı renklerde damlalar
        AddBlob(rt, Vector2.zero, diameter, RandomBrightColor());
        int drops = Random.Range(6, 10);
        for (int i = 0; i < drops; i++)
        {
            Vector2 offset = Random.insideUnitCircle * diameter * 0.55f;
            float d = diameter * Random.Range(0.25f, 0.6f);
            AddBlob(rt, offset, d, RandomBrightColor());
        }

        _splats.Add((group, duration > 0f ? Time.time + duration : float.PositiveInfinity));
    }

    static void AddBlob(RectTransform parent, Vector2 offset, float diameter, Color color)
    {
        var go = new GameObject("Blob", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchoredPosition = offset;
        rt.sizeDelta = Vector2.one * diameter;

        var img = go.GetComponent<Image>();
        img.sprite = SpriteUtil.Circle;
        img.color = color;
        img.raycastTarget = false;
    }

    static Color RandomBrightColor() => Color.HSVToRGB(Random.value, Random.Range(0.7f, 1f), 1f);

    void Update()
    {
        for (int i = _splats.Count - 1; i >= 0; i--)
        {
            (CanvasGroup group, float endTime) = _splats[i];
            float left = endTime - Time.time;

            if (left <= 0f)
            {
                Destroy(group.gameObject);
                _splats.RemoveAt(i);
            }
            else if (left < FadeSeconds)
            {
                group.alpha = left / FadeSeconds; // son saniyede solar
            }
        }
    }

    void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }
}
