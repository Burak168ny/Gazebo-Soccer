using UnityEngine;

/// Asset gerektirmeden placeholder sprite üretir. Sonra gerçek çizimlerle değiştirilebilir.
public static class SpriteUtil
{
    static Sprite _square, _circle;

    /// 1x1 birim kare.
    public static Sprite Square
    {
        get
        {
            if (_square != null) return _square;
            var tex = new Texture2D(8, 8) { filterMode = FilterMode.Point };
            var pixels = new Color[64];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            _square = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8f);
            return _square;
        }
    }

    /// Çapı 1 birim olan yumuşak kenarlı daire.
    public static Sprite Circle
    {
        get
        {
            if (_circle != null) return _circle;
            const int size = 128;
            var tex = new Texture2D(size, size) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - r, dy = y + 0.5f - r;
                float alpha = Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy)); // kenar yumuşatma
                pixels[y * size + x] = new Color(1, 1, 1, alpha);
            }
            tex.SetPixels(pixels);
            tex.Apply();
            _circle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return _circle;
        }
    }
}
