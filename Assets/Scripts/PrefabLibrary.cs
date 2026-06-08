using UnityEngine;

namespace SundaeDiver
{
    /// <summary>
    /// Drag your real-art prefabs here. The LevelGenerator instantiates these instead
    /// of the runtime placeholders. Any slot left empty falls back to a generated
    /// placeholder sprite, so the game keeps running while you build art incrementally.
    ///
    /// Put this component on the GameManager object and assign it in the GameManager.
    /// </summary>
    public class PrefabLibrary : MonoBehaviour
    {
        [System.Serializable]
        public struct ToppingPrefab
        {
            public ToppingType type;
            public GameObject prefab;
        }

        [Header("Obstacles (one is picked at random per hazard row)")]
        public GameObject[] obstaclePrefabs;

        [Header("Toppings (map each type to a prefab)")]
        public ToppingPrefab[] toppingPrefabs;

        [Header("Dishes / containers")]
        public GameObject longDishPrefab;
        public GameObject tallGlassPrefab;

        public GameObject ObstacleFor(DeterministicRng rng)
        {
            if (obstaclePrefabs == null || obstaclePrefabs.Length == 0) return null;
            return obstaclePrefabs[rng.RangeInt(0, obstaclePrefabs.Length)];
        }

        public GameObject ToppingFor(ToppingType t)
        {
            if (toppingPrefabs != null)
                foreach (var tp in toppingPrefabs)
                    if (tp.type == t) return tp.prefab;
            return null;
        }

        public GameObject DishFor(DishType d)
            => d == DishType.LongDish ? longDishPrefab : tallGlassPrefab;
    }
}
