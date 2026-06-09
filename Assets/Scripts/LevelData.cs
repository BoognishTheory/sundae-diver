using System.Collections.Generic;
using UnityEngine;

namespace SundaeDiver
{
    /// <summary>
    /// One level in the "level tree". The container/dish shape sets the landing
    /// target orientation. Difficulty is expressed through speedMul, obstacleRate
    /// and the row-gap range so earlier levels are slower & sparser (Playtest 01).
    /// Create assets via Assets > Create > SundaeDiver > Level, or use
    /// LevelData.CreateDefaults() for the two built-in demo levels.
    /// </summary>
    [CreateAssetMenu(menuName = "SundaeDiver/Level", fileName = "Level")]
    public class LevelData : ScriptableObject
    {
        public string levelName = "Banana Split";
        public float depth = 90f;                    // world units to fall (longer = more time for 10 items)

        [Range(0f, 1f)] public float obstacleRate = 0.34f; // chance a *free* row is a hazard
        public Vector2 gap = new Vector2(2.4f, 3.4f);      // min/max vertical gap between rows
        public float speedMul = 0.7f;                      // scales fall speed (difficulty)
        public uint seed = 1234;                           // deterministic layout

        [Header("Official collectibles (exact count placed per level)")]
        public int whippedCream = 3;
        public int cherries = 3;
        public int peanuts = 2;
        public int hotFudge = 2;

        /// <summary>The exact multiset of toppings to place this level.</summary>
        public List<ToppingType> ToppingBudget()
        {
            var list = new List<ToppingType>();
            for (int i = 0; i < whippedCream; i++) list.Add(ToppingType.Whip);
            for (int i = 0; i < cherries; i++)     list.Add(ToppingType.Cherry);
            for (int i = 0; i < peanuts; i++)      list.Add(ToppingType.Peanut);
            for (int i = 0; i < hotFudge; i++)     list.Add(ToppingType.Fudge);
            return list;
        }

        public static List<LevelData> CreateDefaults()
        {
            var split = CreateInstance<LevelData>();
            split.levelName = "Banana Split";
            split.depth = 90f;
            split.obstacleRate = 0.34f;
            split.gap = new Vector2(2.4f, 3.4f);
            split.speedMul = 0.6f;
            split.seed = 1234;
            return new List<LevelData> { split };
        }
    }
}
