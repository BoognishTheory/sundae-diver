using System.Collections.Generic;
using UnityEngine;

namespace SundaeDiver
{
    /// <summary>
    /// Owns the run lifecycle and the per-frame dive loop. Deliberately drives the
    /// sub-steps in a fixed order (fall -> banana tick -> set Y -> collisions ->
    /// landing) so there are no Update-order surprises. Collisions are simple
    /// circle-overlap checks, exactly like the HTML prototype — predictable and
    /// physics-engine-free, which keeps the tuned arcade feel intact.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Wire these in the inspector (or via SceneBootstrap)")]
        public GameConfig config;
        public List<LevelData> levels;
        public BananaController banana;
        public Transform itemsRoot;
        public PrefabLibrary prefabs;
        [Tooltip("Optional. Handles Ali's splat-wipe + sundae reveal on landing.")]
        public SundaePresenter presenter;

        public GameState State { get; private set; } = GameState.Menu;
        public DiveInput Input { get; private set; }
        public ScoreSystem Score { get; private set; }
        public LevelData Current { get; private set; }
        public int CurrentIndex { get; private set; }
        public float Progress => Current != null ? Mathf.Clamp01(_distance / Current.depth) : 0f;

        // Debug readouts (shown by PrototypeUI when its Show Debug is on)
        public float Distance => _distance;
        public float FallSpeed => _fallSpeed;
        public int ItemCount => _items != null ? _items.Count : 0;
        public float BananaY => banana != null ? banana.transform.position.y : 0f;

        private LevelGenerator _gen;
        private List<LevelItem> _items;
        private float _distance, _fallSpeed, _stateTimer;
        private bool _ready;
        private bool _awaitingPresenter;

        // Auto-initialise when placed directly in a scene (SceneBootstrap calls Init() first,
        // in which case this is a no-op).
        private void Start() { if (!_ready) Init(); }

        public void Init()
        {
            if (_ready) return;
            if (config == null) config = GameConfig.CreateDefault();
            if (levels == null || levels.Count == 0) levels = LevelData.CreateDefaults();
            if (prefabs == null) prefabs = GetComponent<PrefabLibrary>();
            if (presenter == null) presenter = GetComponent<SundaePresenter>();
            if (itemsRoot == null) itemsRoot = new GameObject("Items").transform;
            banana.config = config;
            banana.Triggered += HandleBananaTrigger;
            Input = new DiveInput();
            Score = new ScoreSystem(config);
            _gen = new LevelGenerator(config, itemsRoot, prefabs);
            State = GameState.Menu;
            _ready = true;
        }

        private void OnDestroy()
        {
            if (banana != null) banana.Triggered -= HandleBananaTrigger;
        }

        // ---- public API (called by the UI) ----
        public void SelectLevel(int index) { CurrentIndex = index; StartLevel(); }
        public void Retry() { StartLevel(); }
        public void ToMenu() { State = GameState.Menu; _gen.Clear(); }
        public void Launch()
        {
            if (State != GameState.Ready) return;
            State = GameState.Diving;
            // Diving-board spring: start with an UPWARD velocity so the banana pops off
            // the board, reaches an apex, then arcs down into the dive.
            _fallSpeed = -config.launchSpringSpeed * Mathf.Max(0.1f, Current.speedMul);
        }

        // Anticipation: while the player pulls down, dip + squash the banana so the board
        // looks loaded. Released, Launch() springs it upward.
        private void ApplyReadyCrouch(float charge)
        {
            var p = banana.transform.position;
            p.x = 0f;
            p.y = -0.15f * charge;
            banana.transform.position = p;
            banana.transform.localScale = new Vector3(1f + 0.08f * charge, 1f - 0.12f * charge, 1f);
        }

        private void StartLevel()
        {
            Current = levels[Mathf.Clamp(CurrentIndex, 0, levels.Count - 1)];
            _distance = 0f;
            _fallSpeed = 0f;            // stationary on the board until Launch springs it
            _items = _gen.Generate(Current, CurrentIndex);
            Score.BeginLevel(_items);
            banana.ResetState();
            banana.transform.position = Vector3.zero;
            banana.transform.rotation = Quaternion.identity;
            banana.transform.localScale = Vector3.one;
            var sr = banana.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = true;
            if (presenter != null) presenter.ResetPresentation();
            _awaitingPresenter = false;
            State = GameState.Ready;
        }

        private void Update()
        {
            if (!_ready) return;
            float dt = Time.deltaTime;
            Input.Poll(State);

            switch (State)
            {
                case GameState.Ready:
                    ApplyReadyCrouch(Input.LaunchCharge);
                    if (Input.LaunchRequested) Launch();
                    break;
                case GameState.Diving:
                    TickDiving(dt);
                    break;
                case GameState.Landing:
                    if (_awaitingPresenter) break;     // the presenter drives the transition
                    _stateTimer -= dt;
                    if (_stateTimer <= 0f) State = GameState.Result;
                    break;
            }
        }

        private void TickDiving(float dt)
        {
            float spd = Mathf.Max(0.1f, Current.speedMul);  // guard against an unset (0) speedMul
            // While the banana is above the board (distance < 0) it's in the spring arc —
            // use the snappier launch gravity. Once it crosses back down, normal dive fall.
            float accel = (_distance < 0f ? config.launchGravity : config.fallAccel) * spd;
            _fallSpeed = Mathf.Min(config.fallMax * spd, _fallSpeed + accel * dt);
            _distance += _fallSpeed * dt;

            banana.Tick(dt, Input);
            var p = banana.transform.position;
            p.y = -_distance;
            banana.transform.position = p;

            if (_distance >= Current.depth) DoLanding();
        }

        // Collision is trigger-based now: the banana's collider fires this per item.
        private void HandleBananaTrigger(Collider2D other)
        {
            if (State != GameState.Diving) return;
            var view = other.GetComponentInParent<LevelItemView>();
            if (view == null || view.Dead) return;

            if (!view.IsObstacle)
            {
                view.Dead = true;
                Score.Collect(view.Topping);
                Destroy(view.gameObject);
            }
            else if (!banana.IsInvulnerable)
            {
                view.Dead = true;
                Score.Penalty();
                float dirSign = Mathf.Sign(view.transform.position.x - banana.transform.position.x);
                bool emptied = banana.ApplyHit(dirSign);
                Destroy(view.gameObject);
                if (emptied) DoFail();
            }
        }

        private void DoLanding()
        {
            if (banana.Chunks <= 0) { DoFail(); return; }
            float acc = banana.LandingAccuracy();
            Score.Finalize(banana.Chunks, acc);
            PresentResult(BuildResult());
        }

        private void DoFail()
        {
            Score.Fail();
            EnterLanding(0.7f);   // a fail has no sundae to build, so keep it quick
        }

        private SundaeResult BuildResult()
        {
            return new SundaeResult
            {
                scoops = Score.Scoops,
                chunks = banana.Chunks,
                maxChunks = config.maxChunks,
                finalScore = Score.FinalScore,
                failed = Score.Failed,
                toppings = new Dictionary<ToppingType, int>(Score.Collected),
            };
        }

        // Ali's splat-wipe reveal: cover the camera, build the sundae behind it, slide off.
        private void PresentResult(SundaeResult result)
        {
            State = GameState.Landing;
            if (presenter != null)
            {
                _awaitingPresenter = true;
                var sr = banana.GetComponent<SpriteRenderer>();
                if (sr != null) sr.enabled = false;        // the banana "becomes" the sundae
                presenter.Play(result, _gen.DishView, () => State = GameState.Result);
            }
            else
            {
                EnterLanding(0.9f);
            }
        }

        private void EnterLanding(float settle)
        {
            State = GameState.Landing;
            _awaitingPresenter = false;
            _stateTimer = settle;
        }
    }
}
