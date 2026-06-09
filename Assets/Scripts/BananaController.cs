using UnityEngine;

namespace SundaeDiver
{
    /// <summary>
    /// The player banana. Owns horizontal position (X), rotation, scale, chunks and
    /// i-frames. The GameManager owns the vertical fall (Y) and composes the final
    /// transform, so movement and rotation stay fully decoupled (Playtest 01).
    ///
    /// Rotation model (Playtest 01): STATIC rate while a scoop button is held — no
    /// momentum. Obstacle hits still impart a separate, decaying knock-spin, which
    /// players liked, so that is kept as its own term.
    /// </summary>
    public class BananaController : MonoBehaviour
    {
        public GameConfig config;

        [Header("Health visuals (optional — drag your per-state banana prefabs here)")]
        [Tooltip("Banana states ordered MOST DAMAGED -> FULL. The matching one is shown for the current chunk count. Leave empty to just use one sprite.")]
        public GameObject[] healthStatePrefabs;
        [Tooltip("Also shrink the banana as chunks are lost. Turn OFF if your state art already conveys damage.")]
        public bool shrinkWithChunks = true;

        public int Chunks { get; private set; }
        public float Scale { get; private set; } = 1f;
        public float AngleDeg { get; private set; }
        public bool IsInvulnerable => _invuln > 0f;
        public float CurrentRadius => config.bananaRadius * Scale;

        /// <summary>Raised when something enters the banana's trigger (routed to GameManager).</summary>
        public event System.Action<Collider2D> Triggered;

        private float _x;
        private float _vx;
        private float _knockSpin;   // deg/sec, decays
        private float _invuln;
        private Rigidbody2D _rb;
        private GameObject[] _stateInstances;   // instantiated once, toggled by health
        private int _shownState = -1;

        private static bool _buildingStates;   // recursion guard for health-state instancing

        private void Awake()
        {
            // If THIS banana is itself being instantiated as a health-state visual (because a
            // state prefab accidentally carries a BananaController), do nothing: no physics,
            // no further state-instancing. Without this, instancing recurses forever and Unity
            // throws StackOverflowException in Internal_CloneSingleWithParent.
            if (_buildingStates) return;

            // The banana must have a Rigidbody2D for Unity to fire trigger events, but
            // we drive movement ourselves, so force KINEMATIC (no gravity, no physics
            // fighting the code). This overrides a Dynamic setting on purpose.
            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null) _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.gravityScale = 0f;
            _rb.constraints = RigidbodyConstraints2D.None;

            // Ensure there is *some* collider so detection works even before art is added.
            if (GetComponent<Collider2D>() == null)
            {
                var c = gameObject.AddComponent<CircleCollider2D>();
                c.radius = 0.45f;
            }

            // Instantiate the health-state visuals once as children (visual only — the
            // gameplay collider/Rigidbody stay on this root). They are toggled by chunk count.
            if (healthStatePrefabs != null && healthStatePrefabs.Length > 0)
            {
                _buildingStates = true;
                _stateInstances = new GameObject[healthStatePrefabs.Length];
                for (int i = 0; i < healthStatePrefabs.Length; i++)
                {
                    if (healthStatePrefabs[i] == null) continue;
                    var inst = Instantiate(healthStatePrefabs[i], transform);
                    inst.transform.localPosition = Vector3.zero;
                    inst.transform.localRotation = Quaternion.identity;
                    // strip anything that could fight or duplicate the parent banana
                    var ctrl = inst.GetComponent<BananaController>(); if (ctrl != null) Destroy(ctrl);
                    var irb = inst.GetComponent<Rigidbody2D>();       if (irb != null)  Destroy(irb);
                    foreach (var col in inst.GetComponentsInChildren<Collider2D>()) Destroy(col);
                    inst.SetActive(false);
                    _stateInstances[i] = inst;
                }
                _buildingStates = false;
            }
        }

        private void OnTriggerEnter2D(Collider2D other) => Triggered?.Invoke(other);

        public void ResetState()
        {
            Chunks = config.maxChunks;
            _x = 0f;
            _vx = 0f;
            AngleDeg = 0f;
            _knockSpin = 0f;
            _invuln = 0f;
            Scale = 1f;
            UpdateHealthVisual();
        }

        // Show the state prefab matching the current chunk count. Order your prefabs
        // MOST DAMAGED -> FULL, so the last element is the healthy banana.
        //  - if you supply (maxChunks + 1) states: one per level incl. 0  -> index = chunks
        //  - if you supply (maxChunks) states:      one per live level     -> index = chunks - 1
        //  - any other count: spread proportionally, full health = last
        private void UpdateHealthVisual()
        {
            if (_stateInstances == null || _stateInstances.Length == 0) return;
            int len = _stateInstances.Length;
            int idx;
            if (len == config.maxChunks + 1)      idx = Chunks;
            else if (len == config.maxChunks)     idx = Chunks - 1;
            else                                  idx = Mathf.RoundToInt(
                                                      (config.maxChunks > 0 ? (float)Chunks / config.maxChunks : 0f) * (len - 1));
            idx = Mathf.Clamp(idx, 0, len - 1);
            if (idx == _shownState) return;
            _shownState = idx;
            for (int i = 0; i < len; i++)
                if (_stateInstances[i] != null) _stateInstances[i].SetActive(i == idx);
        }

        /// <summary>Advance one frame. GameManager calls this, then sets Y.</summary>
        public void Tick(float dt, DiveInput input)
        {
            // --- horizontal (decoupled from rotation) ---
            _vx += (input.MoveDeltaPixels / Mathf.Max(1, Screen.width)) * config.moveSpeed;
            _vx += input.KeyMoveDir * config.keyMoveAccel * dt;
            _vx *= Mathf.Exp(-config.moveFriction * dt);
            _x += _vx * dt;

            float lim = config.playHalfWidth;
            if (_x < -lim) { _x = -lim; _vx *= -0.3f; }
            if (_x >  lim) { _x =  lim; _vx *= -0.3f; }

            // --- rotation: static player rate + decaying knock-spin ---
            float playerRate = 0f;
            if (input.HoldCW)  playerRate += config.rotateRate;
            if (input.HoldCCW) playerRate -= config.rotateRate;
            AngleDeg += (playerRate + _knockSpin) * dt;
            _knockSpin *= Mathf.Exp(-config.knockSpinDamp * dt);

            // --- timers & visual scale ---
            if (_invuln > 0f) _invuln -= dt;
            float targetScale = shrinkWithChunks
                ? Mathf.Max(config.minScale, (float)Chunks / config.maxChunks)
                : 1f;
            Scale = Mathf.Lerp(Scale, targetScale, 1f - Mathf.Exp(-12f * dt));

            // apply X / rotation / scale (Y is set by GameManager)
            var p = transform.position;
            p.x = _x;
            transform.position = p;
            transform.rotation = Quaternion.Euler(0f, 0f, AngleDeg);
            transform.localScale = Vector3.one * Scale;
        }

        /// <summary>Returns true if this hit emptied the banana (auto-fail).</summary>
        public bool ApplyHit(float dirSign)
        {
            if (_invuln > 0f) return false;
            Chunks = Mathf.Max(0, Chunks - 1);
            _invuln = config.invulnTime;
            _knockSpin += -dirSign * config.knockSpinKick; // spin away from the impact
            _vx += -dirSign * config.knockMoveKick;
            UpdateHealthVisual();
            return Chunks <= 0;
        }

        /// <summary>How flat the banana is on landing, 0..1 (1 = perfectly horizontal).</summary>
        public float LandingAccuracy()
        {
            // Fold to 0..180 (the banana sprite has 180° symmetry). bananaFlatOffsetDeg
            // lets real art whose "flat" pose isn't at 0° be compensated without re-rotating.
            float a = Mathf.Repeat(AngleDeg - config.bananaFlatOffsetDeg, 180f);
            float d = Mathf.Min(a, 180f - a);   // distance to flat (0 or 180)
            return Mathf.Max(0f, 1f - d / 90f);
        }
    }
}
