using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SundaeDiver
{
    /// <summary>Everything the reveal needs to know about the finished run.</summary>
    public struct SundaeResult
    {
        public DishType dish;
        public int scoops;
        public int chunks;
        public int maxChunks;
        public int finalScore;
        public bool failed;
        public Dictionary<ToppingType, int> toppings;
    }

    /// <summary>
    /// Ali's idea: when the banana lands, an ice-cream splat covers the camera. While
    /// hidden, this builds the player's finished sundae from their run — scoops earned
    /// become stacked ice-cream scoops, collected toppings become garnish, all sitting
    /// on the level's dish. Then the splat slides off to reveal it.
    ///
    /// Fully prefab-driven, with generated placeholders so it runs before any art is in.
    /// Put this on the GameManager object and assign it in the GameManager's "Presenter"
    /// slot (or leave it — GameManager will find it on the same object).
    /// </summary>
    public class SundaePresenter : MonoBehaviour
    {
        [Header("Splat overlay")]
        public Sprite splatSprite;                       // full-screen ice-cream splat (PNG w/ alpha)
        public Color splatTint = new Color(1f, 0.95f, 0.9f);
        public float splatInTime = 0.18f;
        public float holdTime = 0.5f;
        public float slideOffTime = 0.55f;

        [Header("Sundae pieces")]
        public GameObject scoopPrefab;                   // one scoop; stacked by scoops earned
        public float scoopSpacing = 0.55f;               // vertical gap between scoops (world units)
        public float baseOffset = 0.6f;                  // first scoop offset above the dish origin
        public PrefabLibrary garnishLibrary;             // reuse in-level topping prefabs as garnish (optional)
        public int maxGarnish = 8;                       // cap so the top doesn't clutter

        [Header("Optional: dedicated sundae art")]
        [Tooltip("Per-type garnish prefabs made FOR the sundae (overrides reusing the collectible prefabs).")]
        public PrefabLibrary.ToppingPrefab[] garnishOverride;
        [Tooltip("Whole pre-built sundae prefabs by scoop rating: index 0=fail/none .. 3=three scoops. If the entry for the earned scoops is set, it is used INSTEAD of composing scoops+garnish.")]
        public GameObject[] prebuiltByScoops;

        private Transform _root;                         // parent for built pieces
        private GameObject _splat;
        private Camera _cam;
        private bool _placeholdersBuilt;
        private Sprite _phSplat, _phScoop;
        private readonly Dictionary<ToppingType, Sprite> _phGarnish = new Dictionary<ToppingType, Sprite>();

        public void Play(SundaeResult result, GameObject dishView, Action onComplete)
            => StartCoroutine(Sequence(result, dishView, onComplete));

        public void ResetPresentation()
        {
            StopAllCoroutines();
            if (_root != null) Destroy(_root.gameObject);
            if (_splat != null) Destroy(_splat);
            _root = null; _splat = null;
        }

        private IEnumerator Sequence(SundaeResult result, GameObject dishView, Action onComplete)
        {
            EnsurePlaceholders();
            _cam = Camera.main;

            // 1) splat covers the camera
            _splat = MakeSplat();
            yield return Animate(splatInTime, t => SetSplatCover(EaseOutBack(t)));

            // 2) build the sundae behind the splat (the "determine the prefabs" step)
            BuildSundae(result, dishView);
            yield return new WaitForSeconds(holdTime);

            // 3) splat slides off upward, revealing the sundae
            float viewH = _cam != null ? _cam.orthographicSize * 2f : 10f;
            Vector3 start = _splat.transform.localPosition;
            Vector3 end = start + Vector3.up * (viewH * 1.6f);
            yield return Animate(slideOffTime, t =>
                _splat.transform.localPosition = Vector3.Lerp(start, end, EaseInCubic(t)));

            Destroy(_splat); _splat = null;
            onComplete?.Invoke();
        }

        // ---- splat ----
        private GameObject MakeSplat()
        {
            var go = new GameObject("SplatOverlay");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = splatSprite != null ? splatSprite : _phSplat;
            sr.color = splatTint;
            sr.sortingOrder = 32000;                      // above everything
            if (_cam != null) go.transform.SetParent(_cam.transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, 5f); // in front of world, inside the frustum
            SetSplatCover(0f);
            return go;
        }

        private void SetSplatCover(float k)
        {
            if (_splat == null) return;
            var sr = _splat.GetComponent<SpriteRenderer>();
            float spriteH = sr.sprite.bounds.size.y;
            float spriteW = sr.sprite.bounds.size.x;
            float viewH = _cam != null ? _cam.orthographicSize * 2f : 10f;
            float aspect = _cam != null ? _cam.aspect : 0.5f;
            float scale = Mathf.Max(viewH / spriteH, (viewH * aspect) / spriteW) * 1.25f;
            _splat.transform.localScale = Vector3.one * scale * Mathf.Max(0.0001f, k);
        }

        // ---- sundae builder ----
        private void BuildSundae(SundaeResult result, GameObject dishView)
        {
            _root = new GameObject("BuiltSundae").transform;
            _root.position = dishView != null ? dishView.transform.position : Vector3.zero;

            // Mode 1: a whole pre-built sundae prefab keyed by scoop rating.
            if (prebuiltByScoops != null && result.scoops >= 0 && result.scoops < prebuiltByScoops.Length
                && prebuiltByScoops[result.scoops] != null)
            {
                var whole = Instantiate(prebuiltByScoops[result.scoops], _root);
                whole.transform.localPosition = Vector3.zero;
                foreach (var col in whole.GetComponentsInChildren<Collider2D>()) col.enabled = false;
                return;
            }

            // Mode 2: compose from scoops + collected toppings.
            int scoops = Mathf.Max(1, result.scoops);
            float topY = baseOffset;
            for (int i = 0; i < scoops; i++)
            {
                topY = baseOffset + i * scoopSpacing;
                SpawnPiece(scoopPrefab, _phScoop, new Vector3(0f, topY, 0f), 50 + i);
            }

            if (result.toppings == null) return;
            int total = 0;
            foreach (var kv in result.toppings) total += kv.Value;
            if (total <= 0) return;

            int placed = 0;
            foreach (var kv in result.toppings)
            {
                int show = Mathf.RoundToInt(maxGarnish * (kv.Value / (float)total));
                for (int j = 0; j < show && placed < maxGarnish; j++, placed++)
                {
                    float ang = placed * 2.39996f;        // golden-angle scatter
                    float rad = 0.18f + 0.10f * (placed % 3);
                    var pos = new Vector3(Mathf.Cos(ang) * rad,
                                          topY + scoopSpacing * 0.4f + Mathf.Sin(ang) * rad * 0.5f,
                                          -0.01f);
                    SpawnPiece(GarnishPrefabFor(kv.Key), _phGarnish[kv.Key], pos, 60 + placed);
                }
            }
        }

        private GameObject GarnishPrefabFor(ToppingType t)
        {
            if (garnishOverride != null)
                foreach (var g in garnishOverride)
                    if (g.type == t && g.prefab != null) return g.prefab;
            return garnishLibrary != null ? garnishLibrary.ToppingFor(t) : null;
        }

        private void SpawnPiece(GameObject prefab, Sprite placeholder, Vector3 localPos, int sorting)
        {
            GameObject go;
            if (prefab != null)
            {
                go = Instantiate(prefab, _root);
                go.transform.localPosition = localPos;
            }
            else
            {
                go = new GameObject("Piece");
                go.transform.SetParent(_root, false);
                go.transform.localPosition = localPos;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = placeholder;
                sr.sortingOrder = sorting;
            }
            // never let a reused topping prefab's trigger interfere here
            var col = go.GetComponentInChildren<Collider2D>();
            if (col != null) col.enabled = false;
        }

        // ---- placeholders + easing ----
        private void EnsurePlaceholders()
        {
            if (_placeholdersBuilt) return;
            _placeholdersBuilt = true;
            _phSplat = SpriteFactory.Circle(256, new Color(1f, 0.93f, 0.86f), new Color(0.95f, 0.8f, 0.85f), 10);
            _phScoop = SpriteFactory.Circle(120, new Color(1f, 0.86f, 0.7f), new Color(0.9f, 0.72f, 0.55f), 6);
            foreach (var t in Toppings.All)
                _phGarnish[t] = SpriteFactory.Circle(40, Toppings.Tint(t), new Color(0, 0, 0, 0.25f), 3);
        }

        private static IEnumerator Animate(float dur, Action<float> step)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                step(Mathf.Clamp01(t / dur));
                yield return null;
            }
            step(1f);
        }

        private static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f, c3 = 2.70158f;
            float xm = x - 1f;
            return 1f + c3 * xm * xm * xm + c1 * xm * xm;
        }

        private static float EaseInCubic(float x) => x * x * x;
    }
}
