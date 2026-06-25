using UnityEngine;

namespace SundaeDiver
{
    /// <summary>
    /// Recycles a pool of background panels as the camera scrolls down during a dive,
    /// creating the illusion the banana is falling through a tiled space.
    ///
    /// Setup:
    ///   1. Create an empty GameObject and attach this script.
    ///   2. Assign your tile image to Bg Sprite.
    ///   3. In the sprite's Import Settings set:
    ///        Mesh Type  = Full Rect
    ///        Wrap Mode  = Repeat
    ///      This allows Unity to tile the pattern across each panel.
    ///   4. Tune the sprite's Pixels Per Unit to control how large each tile
    ///      appears on screen (higher PPU = smaller tiles).
    ///   5. Leave Camera null — the script finds the main camera automatically.
    /// </summary>
    public class BackgroundScroller : MonoBehaviour
    {
        [Header("References")]
        public Camera cam;
        [Tooltip("The repeating tile image. Must be imported with Mesh Type = Full Rect and Wrap Mode = Repeat.")]
        public Sprite bgSprite;

        [Header("Settings")]
        [Tooltip("Height of each panel in world units. Match or exceed orthoSize * 2 (screen height).")]
        public float panelHeight = 16f;
        [Tooltip("Number of panels in the recycle pool. 3 handles any fall speed.")]
        public int panelCount = 3;
        [Tooltip("Sorting order — keep this well below all other sprites.")]
        public int sortingOrder = -10;

        private SpriteRenderer[] _panels;
        private float _panelWidth;
        private float _lastCamY = float.NaN; // NaN = not yet initialized

        // -----------------------------------------------------------------------
        private void Awake()
        {
            if (cam == null) cam = Camera.main;

            _panelWidth = cam != null
                ? cam.orthographicSize * 2f * cam.aspect
                : 8f;

            _panels = new SpriteRenderer[panelCount];
            for (int i = 0; i < panelCount; i++)
            {
                var go = new GameObject($"BG_Panel_{i}");
                go.transform.SetParent(transform);
                go.transform.position = Vector3.zero; // RepositionPanels sets real Y in LateUpdate

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = sortingOrder;

                if (bgSprite != null)
                {
                    sr.sprite   = bgSprite;
                    sr.drawMode = SpriteDrawMode.Tiled;
                    sr.size     = new Vector2(_panelWidth, panelHeight);
                }
                else
                {
                    // Placeholder so the scene isn't empty while art isn't assigned
                    sr.sprite = BuildFallback();
                    sr.color  = new Color(0.94f, 0.92f, 0.89f);
                    sr.transform.localScale = new Vector3(_panelWidth, panelHeight, 1f);
                }

                _panels[i] = sr;
            }
        }

        // -----------------------------------------------------------------------
        private void LateUpdate()
        {
            if (cam == null || _panels == null) return;

            float camX = cam.transform.position.x;
            float camY = cam.transform.position.y;

            // If the camera has jumped more than one panel height (scene transition,
            // game start, level reset, etc.), snap all panels around the new position
            // so there's no gap in coverage.
            if (float.IsNaN(_lastCamY) || Mathf.Abs(camY - _lastCamY) > panelHeight)
                RepositionPanels(camX, camY);

            _lastCamY = camY;

            float camTop = camY + cam.orthographicSize;

            // Pass 1: keep panels centred on camera X, find the lowest panel Y
            float lowestY = float.MaxValue;
            foreach (var p in _panels)
            {
                var pos = p.transform.position;
                pos.x = camX;
                p.transform.position = pos;
                if (pos.y < lowestY) lowestY = pos.y;
            }

            // Pass 2: recycle any panel that has scrolled fully above the camera view
            foreach (var p in _panels)
            {
                float panelBottom = p.transform.position.y - panelHeight * 0.5f;
                if (panelBottom > camTop)
                {
                    var pos = p.transform.position;
                    pos.y   = lowestY - panelHeight;
                    p.transform.position = pos;
                    lowestY = pos.y;
                }
            }
        }

        // Places panels so they cover the current camera position.
        // With 3 panels: one above, one at, one below the camera centre.
        private void RepositionPanels(float camX, float camY)
        {
            int half = _panels.Length / 2;
            for (int i = 0; i < _panels.Length; i++)
            {
                float y = camY + (half - i) * panelHeight;
                _panels[i].transform.position = new Vector3(camX, y, 0f);
            }
        }

        // -----------------------------------------------------------------------
        private static Sprite BuildFallback()
        {
            var tex = new Texture2D(2, 2);
            tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}
