using UnityEngine;

namespace SundaeDiver
{
    /// <summary>
    /// Generates simple placeholder sprites at runtime (circles, ellipses, rounded
    /// rects) so the prototype runs in a brand-new project with no art imported.
    /// Swap these for real sprites later by assigning them in the inspector and
    /// removing the factory calls in SceneBootstrap.
    /// </summary>
    public static class SpriteFactory
    {
        private const float PPU = 100f; // pixels per unit

        public static Sprite Circle(int diameter, Color fill, Color outline, int outlinePx = 4)
        {
            var tex = NewTex(diameter, diameter);
            float r = diameter * 0.5f;
            float cx = r, cy = r;
            for (int y = 0; y < diameter; y++)
            for (int x = 0; x < diameter; x++)
            {
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d <= r) tex.SetPixel(x, y, d > r - outlinePx ? outline : fill);
            }
            return Finish(tex);
        }

        public static Sprite Ellipse(int w, int h, Color fill, Color outline, int outlinePx = 4)
        {
            var tex = NewTex(w, h);
            float rx = w * 0.5f, ry = h * 0.5f;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float nx = (x + 0.5f - rx) / rx, ny = (y + 0.5f - ry) / ry;
                float d = nx * nx + ny * ny;
                if (d <= 1f) tex.SetPixel(x, y, d > 0.80f ? outline : fill);
            }
            return Finish(tex);
        }

        public static Sprite RoundRect(int w, int h, int radius, Color fill)
        {
            var tex = NewTex(w, h);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool inside = true;
                // round the four corners
                if (x < radius && y < radius) inside = Corner(x, y, radius, radius, radius);
                else if (x >= w - radius && y < radius) inside = Corner(x, y, w - radius, radius, radius);
                else if (x < radius && y >= h - radius) inside = Corner(x, y, radius, h - radius, radius);
                else if (x >= w - radius && y >= h - radius) inside = Corner(x, y, w - radius, h - radius, radius);
                if (inside) tex.SetPixel(x, y, fill);
            }
            return Finish(tex);
        }

        private static bool Corner(int x, int y, int cx, int cy, int r)
        {
            float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
            return dx * dx + dy * dy <= r * r;
        }

        private static Texture2D NewTex(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var clear = new Color32(0, 0, 0, 0);
            var px = new Color32[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = clear;
            tex.SetPixels32(px);
            return tex;
        }

        private static Sprite Finish(Texture2D tex)
        {
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                 new Vector2(0.5f, 0.5f), PPU);
        }

        /// <summary>Helper: make a GameObject with a SpriteRenderer at a sorting order.</summary>
        public static GameObject NewSpriteObject(string name, Sprite sprite, int sortingOrder, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            return go;
        }
    }
}
