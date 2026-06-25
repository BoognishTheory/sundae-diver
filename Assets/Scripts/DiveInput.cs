using UnityEngine;

namespace SundaeDiver
{
    /// <summary>
    /// Pointer/keyboard input for the dive. Uses the legacy Input class so it works
    /// in a fresh project (set Player Settings > Active Input Handling to "Both" or
    /// "Input Manager (Old)"). Rotation buttons (uiCW/uiCCW) are driven by PrototypeUI.
    /// </summary>
    public class DiveInput
    {
        // Outputs read by the game each frame
        public float MoveDeltaPixels;   // horizontal drag this frame (screen px)
        public int   KeyMoveDir;        // -1/0/+1 from arrow keys (editor)
        public bool  LaunchRequested;   // fires on RELEASE of a downward swipe (or Space/Down)
        public float LaunchCharge;      // 0..1, how far the player has pulled down in Ready

        // Set by PrototypeUI each frame (reset on GUI Layout)
        public bool uiCW, uiCCW;

        public bool HoldCW  => uiCW  || Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.X) || Input.GetKey(KeyCode.K);
        public bool HoldCCW => uiCCW || Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.J);

        private const float LaunchSwipePx = 60f;     // min downward drag to count as a launch
        private const float ChargeDistancePx = 220f; // drag distance for a full "loaded board"
        private bool _down;
        private Vector2 _downPos, _lastPos;
        private bool _startedInControlZone;
        private float _peakDown;                      // peak downward drag since press

        // Bottom-right zone (screen px, origin bottom-left) reserved for rotate buttons.
        public static bool InControlZone(Vector2 p)
            => p.x > Screen.width - Screen.height * 0.34f && p.y < Screen.height * 0.30f;

        public void Poll(GameState state)
        {
            MoveDeltaPixels = 0f;
            LaunchRequested = false;
            LaunchCharge = 0f;

            // keyboard horizontal (editor testing)
            KeyMoveDir = 0;
            if (Input.GetKey(KeyCode.LeftArrow)  || Input.GetKey(KeyCode.A)) KeyMoveDir -= 1;
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) KeyMoveDir += 1;

            // keyboard launch (immediate)
            if (state == GameState.Ready &&
                (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.DownArrow)))
                LaunchRequested = true;

            // unified pointer (touch first, else mouse)
            bool hasTouch = Input.touchCount > 0;
            Vector2 pos = hasTouch ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;
            bool pointerDown = hasTouch
                ? (Input.GetTouch(0).phase != TouchPhase.Ended && Input.GetTouch(0).phase != TouchPhase.Canceled)
                : Input.GetMouseButton(0);
            bool pressedThisFrame = hasTouch
                ? Input.GetTouch(0).phase == TouchPhase.Began
                : Input.GetMouseButtonDown(0);

            if (pressedThisFrame)
            {
                _down = true;
                _downPos = pos;
                _lastPos = pos;
                _peakDown = 0f;
                _startedInControlZone = InControlZone(pos);
            }

            bool released = _down && !pointerDown;

            if (_down && pointerDown)
            {
                if (state == GameState.Ready && !_startedInControlZone)
                {
                    // pulling down "loads the board" — track the peak pull for the spring
                    _peakDown = Mathf.Max(_peakDown, _downPos.y - pos.y);
                    LaunchCharge = Mathf.Clamp01(_peakDown / ChargeDistancePx);
                }
                else if (state == GameState.Diving && !_startedInControlZone)
                {
                    MoveDeltaPixels = pos.x - _lastPos.x;
                }
                _lastPos = pos;
            }

            if (released)
            {
                // launch fires on RELEASE (the board springs back)
                if (state == GameState.Ready && !_startedInControlZone && _peakDown > LaunchSwipePx)
                    LaunchRequested = true;
                _down = false;
            }
        }
    }
}
