using System.Collections.Generic;
using UnityEngine;

namespace SundaeDiver
{
    /// <summary>
    /// Seeded procedural level generation.
    ///
    /// STRATEGY (keeps levels varied but never mundane or unfair):
    ///  1. Deterministic seed  -> a level is reproducible (testing / daily / sharing).
    ///  2. Difficulty + pacing  -> a pacing curve eases the intro, peaks mid-level,
    ///     and calms before the dish so the player can set their landing orientation.
    ///  3. Reachability guarantee -> the horizontal step between consecutive rows is
    ///     capped to what the banana can physically cross, so there is always a path.
    ///  4. Anti-repetition       -> consecutive items avoid the same lane.
    ///  5. Risk / reward          -> higher-value toppings are biased to the edges /
    ///     off the safe centre line; safe central pickups are low value.
    ///  6. Landing clearance      -> the last stretch before the dish is left empty.
    ///
    /// PRODUCTION UPGRADE PATH (documented for the Unity build):
    ///  Replace per-row random placement with an authored *pattern library*
    ///  (small ScriptableObject "beats" tagged by difficulty + dish), assembled with
    ///  a shuffled "bag" so every pattern appears before any repeats. That gives
    ///  hand-crafted feel with procedural variety. The hooks below (PickPattern point,
    ///  pacing, reachability) are where that slots in.
    /// </summary>
    public class LevelGenerator
    {
        private readonly GameConfig _config;
        private readonly Transform _root;
        private readonly PrefabLibrary _prefabs;

        // cached placeholder sprites
        private Sprite _obstacleSprite;
        private readonly Dictionary<ToppingType, Sprite> _toppingSprites = new Dictionary<ToppingType, Sprite>();
        private Sprite _dishSprite;

        private readonly List<GameObject> _spawned = new List<GameObject>();

        private const float IntroClearance = 5f;     // calm launch zone
        private const float LandingClearance = 8f;    // calm approach to the dish
        private const float MaxLateralStep = 2.0f;    // fairness: max x change per row

        public GameObject DishView { get; private set; }

        public LevelGenerator(GameConfig config, Transform itemsRoot, PrefabLibrary prefabs)
        {
            _config = config;
            _root = itemsRoot;
            _prefabs = prefabs;
        }

        public List<LevelItem> Generate(LevelData level, int levelIndex)
        {
            Clear();
            BuildBankIfNeeded();

            var rng = new DeterministicRng(level.seed + (uint)levelIndex * 97u);
            var items = new List<LevelItem>();

            // 1) Lay out the row depths down the playable span.
            //    Each step is forced to advance by a minimum, and the loop is capped, so a
            //    mis-set Gap (e.g. 0,0) can NEVER spin forever and freeze the editor.
            var rows = new List<float>();
            float d = IntroClearance;
            float end = level.depth - LandingClearance;
            const float MinRowStep = 0.5f;
            int safety = 0;
            while (d < end && safety++ < 2000)
            {
                float pacing = PacingCurve(d / Mathf.Max(1f, level.depth));
                float step = rng.Range(level.gap.x, level.gap.y) * Mathf.Lerp(1.2f, 0.9f, pacing);
                d += Mathf.Max(MinRowStep, step);   // always move down, even if Gap is bad
                if (d >= end) break;
                rows.Add(d);
            }
            int R = rows.Count;

            // 2) Take the EXACT official collectible set, shuffle it, and assign it to
            //    rows spread evenly down the level (so toppings aren't clumped).
            var budget = level.ToppingBudget();
            Shuffle(budget, rng);
            int T = Mathf.Min(budget.Count, R);
            var toppingRow = new Dictionary<int, ToppingType>();
            if (R > 0 && T > 0)
            {
                float stride = R / (float)T;
                var used = new HashSet<int>();
                for (int k = 0; k < T; k++)
                {
                    int idx = Mathf.Clamp(Mathf.FloorToInt(k * stride + rng.Next() * stride * 0.6f), 0, R - 1);
                    int guard = 0;
                    while (used.Contains(idx) && guard++ < R) idx = (idx + 1) % R;
                    used.Add(idx);
                    toppingRow[idx] = budget[k];
                }
            }

            // 3) Walk the rows: place the assigned topping, otherwise maybe a hazard.
            //    Toppings use risk/reward edge bias; hazards stay within reach so there
            //    is always a path through.
            float lastObstacleX = 0f;
            for (int i = 0; i < R; i++)
            {
                float worldY = -rows[i];
                if (toppingRow.TryGetValue(i, out var type))
                {
                    items.Add(SpawnTopping(type, ToppingLaneX(rng, type), worldY));
                }
                else
                {
                    float pacing = PacingCurve(rows[i] / level.depth);
                    if (rng.Next() < level.obstacleRate * Mathf.Lerp(0.6f, 1f, pacing))
                    {
                        lastObstacleX = ReachableLaneX(rng, lastObstacleX);
                        items.Add(SpawnObstacle(lastObstacleX, worldY, rng));
                    }
                    // else: a breather row (intentionally empty)
                }
            }

            DishView = SpawnDish(level);
            return items;
        }

        // ---- placement helpers ----

        private static void Shuffle(List<ToppingType> list, DeterministicRng rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.RangeInt(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // Toppings: higher value biases toward the edges (risk/reward). These are
        // optional pickups, so they are NOT reachability-capped — edge ones are a gamble.
        private float ToppingLaneX(DeterministicRng rng, ToppingType type)
        {
            float lim = _config.playHalfWidth - 0.2f;
            float vmin = float.MaxValue, vmax = float.MinValue;
            foreach (var tt in Toppings.All)
            {
                float v = Toppings.Value(tt);
                if (v < vmin) vmin = v;
                if (v > vmax) vmax = v;
            }
            float norm = vmax > vmin ? (Toppings.Value(type) - vmin) / (vmax - vmin) : 0f;
            float mag = Mathf.Clamp(Mathf.Lerp(0.25f, lim, norm) + rng.Range(-0.25f, 0.25f), 0f, lim);
            return (rng.Next() < 0.5f ? -1f : 1f) * mag;
        }

        // Hazards: capped horizontal step from the previous hazard so a dodge path
        // always exists, plus anti-repetition of the same lane.
        private float ReachableLaneX(DeterministicRng rng, float lastX)
        {
            float lim = _config.playHalfWidth - 0.2f;
            float min = Mathf.Max(-lim, lastX - MaxLateralStep);
            float max = Mathf.Min( lim, lastX + MaxLateralStep);
            float x = rng.Range(min, max);
            if (Mathf.Abs(x - lastX) < 0.5f)
                x += (x >= lastX ? 1f : -1f) * 0.8f;
            return Mathf.Clamp(x, -lim, lim);
        }

        // Pacing 0..1: ease in, full through the middle, calm just before landing.
        private static float PacingCurve(float t)
        {
            if (t < 0.15f) return Mathf.Clamp01(t / 0.15f);
            if (t > 0.85f) return Mathf.Max(0.2f, (1f - t) / 0.15f);
            return 1f;
        }

        private LevelItem SpawnObstacle(float x, float y, DeterministicRng rng)
        {
            var type = (ObstacleType)rng.RangeInt(0, 3);
            var item = new LevelItem
            {
                IsObstacle = true,
                Obstacle = type,
                Pos = new Vector2(x, y),
                Radius = 0.42f,
            };

            GameObject prefab = _prefabs != null ? _prefabs.ObstacleFor(rng) : null;
            GameObject go = prefab != null
                ? Object.Instantiate(prefab, new Vector3(x, y, 0f), Quaternion.Euler(0, 0, rng.Range(0, 360)), _root)
                : Placeholder("Obstacle_" + type, _obstacleSprite, x, y, rng.Range(0, 360));

            var view = ConfigureItem(go, item.Radius);
            view.IsObstacle = true;
            view.Obstacle = type;

            item.View = go;
            _spawned.Add(go);
            return item;
        }

        private LevelItem SpawnTopping(ToppingType type, float x, float y)
        {
            var item = new LevelItem
            {
                IsObstacle = false,
                Topping = type,
                Pos = new Vector2(x, y),
                Radius = Toppings.Radius(type),
            };

            GameObject prefab = _prefabs != null ? _prefabs.ToppingFor(type) : null;
            GameObject go = prefab != null
                ? Object.Instantiate(prefab, new Vector3(x, y, 0f), Quaternion.identity, _root)
                : Placeholder("Topping_" + type, _toppingSprites[type], x, y, 0f);

            var view = ConfigureItem(go, item.Radius);
            view.IsObstacle = false;
            view.Topping = type;

            item.View = go;
            _spawned.Add(go);
            return item;
        }

        private GameObject SpawnDish(LevelData level)
        {
            GameObject prefab = _prefabs != null ? _prefabs.dishPrefab : null;
            GameObject go = prefab != null
                ? Object.Instantiate(prefab, new Vector3(0f, -level.depth, 0f), Quaternion.identity, _root)
                : Placeholder("Dish", _dishSprite, 0f, -level.depth, 0f, 1);
            // The dish already contains its 3 scoops. It is purely visual; landing is
            // detected by depth, so it needs no collider/tag.
            _spawned.Add(go);
            return go;
        }

        // --- spawn helpers ---

        private GameObject Placeholder(string name, Sprite sprite, float x, float y, float rotZ, int sorting = 3)
        {
            var go = SpriteFactory.NewSpriteObject(name, sprite, sorting, _root);
            go.transform.position = new Vector3(x, y, 0f);
            go.transform.rotation = Quaternion.Euler(0, 0, rotZ);
            return go;
        }

        // Ensures the spawned item has a trigger collider + a LevelItemView the banana can read.
        private LevelItemView ConfigureItem(GameObject go, float fallbackRadius)
        {
            var col = go.GetComponentInChildren<Collider2D>();
            if (col == null)
            {
                var cc = go.AddComponent<CircleCollider2D>();
                cc.radius = fallbackRadius;
                col = cc;
            }
            col.isTrigger = true;

            var view = go.GetComponent<LevelItemView>();
            if (view == null) view = go.AddComponent<LevelItemView>();
            view.Dead = false;
            return view;
        }

        public void Clear()
        {
            foreach (var go in _spawned) if (go != null) Object.Destroy(go);
            _spawned.Clear();
            DishView = null;
        }

        private void BuildBankIfNeeded()
        {
            if (_obstacleSprite != null) return;
            _obstacleSprite = SpriteFactory.Circle(84, new Color(0.62f, 0.66f, 0.71f), new Color(0.45f, 0.49f, 0.55f));
            foreach (var t in Toppings.All)
            {
                int d = Mathf.RoundToInt(Toppings.Radius(t) * 2f * 100f);
                _toppingSprites[t] = SpriteFactory.Circle(d, Toppings.Tint(t), new Color(0, 0, 0, 0.25f), 3);
            }
            _dishSprite = SpriteFactory.RoundRect(560, 150, 40, new Color(0.93f, 0.95f, 0.96f));
        }
    }
}
