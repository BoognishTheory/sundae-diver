using UnityEngine;

namespace SundaeDiver
{
    /// <summary>
    /// 8-segment kinematic-chain diving board with spring physics, translated from
    /// the "Diving Board Flex" design component.
    ///
    /// Behaviour:
    ///   Ready + no charge  — board sits flat (no idle bounce).
    ///   Ready + charging   — board is held bent down proportional to charge.
    ///   Launch             — spring releases, board whips up and slides left off-screen.
    ///
    /// The board anchor Y is fixed in world space; GameManager reads GetBananaY() so
    /// the banana rides the tip up and down as the player loads the board.
    /// </summary>
    public class DivingBoard : MonoBehaviour
    {
        [Header("References")]
        public GameManager gm;

        [Header("Segment sprites  (plank-seg-0 … plank-seg-7, left → right)")]
        [Tooltip("Assign 8 sprites in order. Leave empty to use the generated placeholder.")]
        public Sprite[] segmentSprites;

        [Header("Layout")]
        [Tooltip("X offset of the board anchor from the banana (board extends rightward). Negative = anchor left of banana.")]
        public float boardOffsetX = -3.5f;
        [Tooltip("Y of the board anchor in world space. Adjust until the board sits under the banana.")]
        public float boardOffsetY = -1.1f;
        [Tooltip("Z depth — positive puts the board behind the banana.")]
        public float boardDepth = 0.1f;
        [Tooltip("Extra Y so the banana center sits on top of the board rather than inside it. Increase if banana floats above, decrease if it sinks below.")]
        public float bananaSitOffsetY = 0.5f;

        [Header("Charge bend")]
        [Tooltip("Degrees the tip is held down at full charge.")]
        public float maxChargeBendDeg = 22f;

        [Header("Spring physics  (matches DC props)")]
        [Range(0.3f, 3f)]
        [Tooltip("How far the board flexes past horizontal on release. DC default: 2.5")]
        public float flexIntensity = 2.5f;
        [Range(0.5f, 1.6f)]
        [Tooltip("Spring stiffness / recovery speed. DC default: 1.0")]
        public float springiness = 1f;

        [Header("Launch slide")]
        [Tooltip("Seconds for the board to slide off-screen after launch.")]
        public float launchDuration = 0.9f;
        [Tooltip("World units traveled left. Should clear the screen.")]
        public float slideDistance = 8f;

        // ---- DC spring constants ----
        private const int SegCount = 8;
        private static readonly float[] Weights = { 0f, 0f, 0f, 5f, 4f, 3f, 2f, 1f };
        private const float WTotal = 15f;

        // ---- state ----
        private Transform[]      _segs;
        private SpriteRenderer[] _segSRs;
        private float _segWidth;           // world-unit width of one segment
        private Vector3 _anchorPos;        // scene-placed position captured in Awake

        private float _theta;              // current bend angle (degrees, + = tip down)
        private float _vel;               // angular velocity (deg/s)
        private bool  _isCharging;        // true while player is pulling down
        private float _chargeAmount;      // 0..1

        private GameState _lastState = GameState.Menu;
        private float _launchTimer = -1f;
        private Vector3 _launchStartPos;
        private bool _visible;

        // -----------------------------------------------------------------------
        private void Awake()
        {
            // Remember where the board was placed in the scene — Y and Z never change;
            // X shifts to follow the banana's horizontal position.
            _anchorPos = transform.position;

            _segs   = new Transform[SegCount];
            _segSRs = new SpriteRenderer[SegCount];

            bool hasCustom = segmentSprites != null && segmentSprites.Length == SegCount;
            Sprite[] sprites = hasCustom ? segmentSprites : BuildPlaceholderSprites();

            _segWidth = sprites[0] != null ? sprites[0].bounds.size.x : 0.5f;

            // Build kinematic chain: Seg[i] is child of Seg[i-1] (root for i=0).
            Transform parent = transform;
            for (int i = 0; i < SegCount; i++)
            {
                var go = new GameObject($"Seg_{i}");
                go.transform.SetParent(parent);
                go.transform.localPosition = i == 0 ? Vector3.zero : new Vector3(_segWidth, 0f, 0f);
                go.transform.localRotation  = Quaternion.identity;
                go.transform.localScale     = Vector3.one;

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite       = sprites[i];
                sr.sortingOrder = -1;

                _segs[i]   = go.transform;
                _segSRs[i] = sr;
                parent      = go.transform;
            }

            SetVisible(false);
        }

        // -----------------------------------------------------------------------
        // Spring / hold simulation — runs every frame so the chain stays animated
        // during the launch slide.
        private void Update()
        {
            if (!_visible && _launchTimer < 0f) return;

            float dt = Mathf.Min(Time.deltaTime, 0.05f);

            if (_isCharging)
            {
                // Hold the board at the loaded angle while player pulls down.
                // No spring physics — tip is locked to charge amount.
                _theta = _chargeAmount * maxChargeBendDeg;
                _vel   = 0f;
            }
            else
            {
                // Spring physics (direct translation from DC):
                // k = 95 * spr², c = 6.2
                float k     = 95f * springiness * springiness;
                const float c = 6.2f;
                float force = -k * _theta - c * _vel;
                _vel   += force * dt;
                _theta += _vel  * dt;
            }

            // Distribute bend across chain — rotations accumulate because each
            // segment is a child of the previous (same as DC's nested divs).
            for (int i = 0; i < SegCount; i++)
            {
                float d = _theta * (Weights[i] / WTotal);
                _segs[i].localRotation = Mathf.Abs(d) > 0.001f
                    ? Quaternion.Euler(0f, 0f, -d)  // negative Z → clockwise → tip dips DOWN
                    : Quaternion.identity;
            }
        }

        // -----------------------------------------------------------------------
        // Position, visibility, and game-state reactions
        private void LateUpdate()
        {
            if (gm == null || gm.banana == null) return;

            var state = gm.State;

            if (state == GameState.Ready)
            {
                if (!_visible)
                {
                    SetVisible(true);
                    SetAlpha(1f);
                    _theta        = 0f;
                    _vel          = 0f;
                    _isCharging   = false;
                    _chargeAmount = 0f;
                    _launchTimer  = -1f;
                }

                // Follow banana X; Y and Z come from the Inspector offset fields.
                float bx = gm.banana.transform.position.x;
                transform.position = new Vector3(bx + boardOffsetX, boardOffsetY, boardDepth);

                // Update charge state for Update() to consume
                float charge  = gm.Input != null ? gm.Input.LaunchCharge : 0f;
                _isCharging   = charge > 0.01f;
                _chargeAmount = charge;
            }
            else if (state == GameState.Diving)
            {
                // On the first Diving frame: release spring and kick off slide
                if (_lastState == GameState.Ready)
                {
                    _isCharging = false;
                    // Upward impulse — board whips above flat before sliding away
                    _vel += 95f * flexIntensity * 1.2f;

                    _launchTimer    = 0f;
                    _launchStartPos = transform.position;
                }

                if (_launchTimer >= 0f)
                {
                    _launchTimer += Time.deltaTime;
                    float t = Mathf.Clamp01(_launchTimer / launchDuration);

                    float slide = Mathf.Pow(t, 1.5f) * slideDistance;
                    transform.position = _launchStartPos + Vector3.left * slide;

                    // Fade out in the second half
                    float alpha = 1f - Mathf.Clamp01((t - 0.5f) / 0.5f);
                    SetAlpha(alpha);

                    if (t >= 1f)
                    {
                        SetVisible(false);
                        _launchTimer = -1f;
                    }
                }
            }
            else
            {
                if (_visible) SetVisible(false);
            }

            _lastState = state;
        }

        // -----------------------------------------------------------------------
        /// <summary>
        /// World Y the banana center should sit at (board tip + sit offset).
        /// Called by GameManager.ApplyReadyCrouch when this board is assigned.
        /// </summary>
        public float GetBananaY()
        {
            // Tip = right edge of the last segment in world space
            Vector3 tip = _segs[SegCount - 1].TransformPoint(new Vector3(_segWidth, 0f, 0f));
            return tip.y + bananaSitOffsetY;
        }

        // -----------------------------------------------------------------------
        private void SetVisible(bool v)
        {
            _visible = v;
            foreach (var sr in _segSRs) if (sr) sr.enabled = v;
        }

        private void SetAlpha(float a)
        {
            var col = new Color(1f, 1f, 1f, a);
            foreach (var sr in _segSRs) if (sr) sr.color = col;
        }

        // -----------------------------------------------------------------------
        private static Sprite[] BuildPlaceholderSprites()
        {
            const int SegPx = 80;
            const int W     = SegPx * SegCount;
            const int H     = 18;
            const float PPU = 160f; // → each seg = 0.5 world units, board = 4 total

            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode   = TextureWrapMode.Clamp
            };

            var pixels = new Color32[W * H];
            for (int y = 0; y < H; y++)
            {
                float gy = Mathf.Sin((float)y / (H - 1) * Mathf.PI);
                for (int x = 0; x < W; x++)
                {
                    float grain = Mathf.PerlinNoise(x * 0.06f, y * 0.5f) * 0.15f;
                    byte r = (byte)Mathf.RoundToInt(Mathf.Lerp(100, 190, gy + grain));
                    byte g = (byte)Mathf.RoundToInt(Mathf.Lerp( 60, 125, gy + grain));
                    byte b = (byte)Mathf.RoundToInt(Mathf.Lerp( 20,  55, gy + grain));
                    pixels[y * W + x] = new Color32(r, g, b, 255);
                }
            }
            for (int y = 0; y < H; y++)
                for (int x = 0; x < 8; x++)
                {
                    float a  = (float)x / 8f;
                    var lc   = pixels[y * W + x];       lc.a = (byte)(lc.a * a); pixels[y * W + x]       = lc;
                    var rc   = pixels[y * W + W-1-x];   rc.a = (byte)(rc.a * a); pixels[y * W + (W-1-x)] = rc;
                }
            tex.SetPixels32(pixels);
            tex.Apply();

            var result = new Sprite[SegCount];
            for (int i = 0; i < SegCount; i++)
                result[i] = Sprite.Create(
                    tex,
                    new Rect(i * SegPx, 0, SegPx, H),
                    new Vector2(0f, 0.5f), // left-edge pivot → chain bends from each joint
                    PPU);
            return result;
        }
    }
}
