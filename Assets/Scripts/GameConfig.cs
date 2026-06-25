using UnityEngine;

namespace SundaeDiver
{
    /// <summary>
    /// All tunable gameplay values, mirroring the HTML prototype's CONFIG block.
    /// Create one via Assets > Create > SundaeDiver > Game Config, or let the
    /// GameManager build a default at runtime if none is assigned.
    /// Units are WORLD units / seconds (the prototype used pixels).
    /// </summary>
    [CreateAssetMenu(menuName = "SundaeDiver/Game Config", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Fall (scaled per level by LevelData.speedMul)")]
        public float fallStart = 6f;      // initial fall speed (units/sec)
        public float fallAccel = 1.2f;    // gravity-ish acceleration during the dive
        public float fallMax   = 12f;     // terminal velocity

        [Header("Launch (diving-board spring)")]
        public float launchSpringSpeed = 20f; // upward pop speed at release (units/sec)
        public float launchGravity     = 14f; // stronger gravity during the pop (snappy arc)

        [Header("Horizontal movement")]
        public float moveSpeed     = 55f; // drag -> horizontal velocity sensitivity
        public float moveFriction  = 8f;  // exponential damping (higher = snappier)
        public float keyMoveAccel  = 38f; // keyboard accel (editor testing)
        public float playHalfWidth = 7f;// clamp banana to +/- this (world units)

        [Header("Rotation (PLAYTEST 01: static, no momentum)")]
        public float rotateRate    = 150f;// player rotation while a scoop button is held (deg/sec)
        public float knockSpinDamp = 6f;  // decay rate of obstacle knock-spin (retained from playtest)
        public float knockSpinKick = 220f;// deg/sec imparted by an obstacle hit
        public float knockMoveKick = 2.2f;// sideways velocity kick from a hit

        [Header("Banana")]
        public int   maxChunks    = 5;
        public float bananaRadius = 0.45f;// collision radius at full size
        public float minScale     = 0.34f;// smallest visual/hitbox scale at low chunks
        public float invulnTime   = 0.8f; // i-frames after a hit (sec)
        [Tooltip("Set if your banana sprite's flat/horizontal pose isn't at 0°. e.g. art drawn at 45° -> 45.")]
        public float bananaFlatOffsetDeg = 0f;

        [Header("Scoring")]
        public int   hitPenalty = 120;    // live score lost per obstacle hit
        public int   intactMax  = 600;    // points for a fully intact banana
        public int   landingMax = 500;    // points for a perfect-orientation landing
        public float[] scoopThresholds = { 0.45f, 0.68f, 0.88f }; // % of level max -> 1/2/3 scoops

        [Header("Camera")]
        [Range(0f, 0.9f)] public float bananaScreenYFactor = 0.34f; // banana sits this far from top
        public float orthoSize = 14f;      // half view height (world units)

        public static GameConfig CreateDefault() => CreateInstance<GameConfig>();
    }
}
