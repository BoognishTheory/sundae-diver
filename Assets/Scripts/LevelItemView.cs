using UnityEngine;

namespace SundaeDiver
{
    /// <summary>
    /// Lightweight data tag added to every spawned obstacle/topping. The banana's
    /// trigger reads this to know whether it grabbed a topping or hit a hazard.
    /// (Real-art prefabs don't need this added by hand — the generator attaches it
    /// at spawn time. But you can add it manually if you place items in a scene.)
    /// </summary>
    public class LevelItemView : MonoBehaviour
    {
        public bool IsObstacle;
        public ToppingType Topping;
        public ObstacleType Obstacle;
        [HideInInspector] public bool Dead;
    }
}
