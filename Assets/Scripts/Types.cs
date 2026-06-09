using UnityEngine;

namespace SundaeDiver
{
    // ---- Game flow ----
    public enum GameState { Menu, Ready, Diving, Landing, Result }

    // ---- Dish / landing ----
    // Single horizontal dish: the banana lands flat. (Tall glass / vertical removed.)

    // ---- Pickups & hazards ----
    public enum ToppingType { Peanut, Whip, Cherry, Fudge }
    public enum ObstacleType { Whisk, RollingPin, Fork }

    /// <summary>
    /// Static lookup for topping values / sizes / colours.
    /// In a full build these would live on a ScriptableObject per topping;
    /// kept inline here so the prototype is drop-in with no asset authoring.
    /// </summary>
    public static class Toppings
    {
        public static readonly ToppingType[] All =
            { ToppingType.Peanut, ToppingType.Whip, ToppingType.Cherry, ToppingType.Fudge };

        public static int Value(ToppingType t)
        {
            switch (t)
            {
                case ToppingType.Whip:   return 50;   // x3 = 150
                case ToppingType.Cherry: return 70;   // x3 = 210
                case ToppingType.Peanut: return 80;   // x2 = 160
                case ToppingType.Fudge:  return 90;   // x2 = 180  -> total 700
            }
            return 0;
        }

        // collision radius in world units
        public static float Radius(ToppingType t)
        {
            switch (t)
            {
                case ToppingType.Peanut: return 0.20f;
                case ToppingType.Whip:   return 0.26f;
                case ToppingType.Cherry: return 0.22f;
                case ToppingType.Fudge:  return 0.25f;
            }
            return 0.22f;
        }

        public static Color Tint(ToppingType t)
        {
            switch (t)
            {
                case ToppingType.Peanut: return new Color(0.75f, 0.54f, 0.29f);
                case ToppingType.Whip:   return Color.white;
                case ToppingType.Cherry: return new Color(0.89f, 0.23f, 0.31f);
                case ToppingType.Fudge:  return new Color(0.35f, 0.20f, 0.12f);
            }
            return Color.white;
        }
    }

    /// <summary>One spawned obstacle or topping in a generated level.</summary>
    public class LevelItem
    {
        public bool IsObstacle;
        public ToppingType Topping;
        public ObstacleType Obstacle;
        public Vector2 Pos;          // world position
        public float Radius;         // collision radius (world units)
        public GameObject View;      // spawned sprite GameObject
        public bool Dead;
    }
}
