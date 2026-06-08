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
        public bool  LaunchRequested;   // swipe-down (or Space/Down) on the board

        // Set by PrototypeUI each frame (reset on GUI Layout)
        public bool uiCW, uiCCW;

        public bool HoldCW  => uiCW  || Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.X);
        public bool HoldCCW => uiCCW || Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.Z);

        private const float LaunchSwipePx = 60f;
        private bool _down;
        private Vector2 _downPos, _lastPos;
        private bool _startedInControlZone;

        // Bottom-right zone (screen px, origin bottom-left) reserved for rotate buttons.
        public static bool InControlZone(Vector2 p)
            => p.x > Screen.width - Screen.height * 0.34f && p.y < Screen.height * 0.30f;

        public void Poll(GameState state)
        {
            MoveDeltaPixels = 0f;
            LaunchRequested = false;

            // keyboard horizontal (editor testing)
            KeyMoveDir = 0;
            if (Input.GetKey(KeyCode.LeftArrow))  KeyMoveDir -= 1;
            if (Input.GetKey(KeyCode.RightArrow)) KeyMoveDir += 1;

            // keyboard launch
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
                _startedInControlZone = InControlZone(pos);
            }

            if (_down && pointerDown)
            {
                if (state == GameState.Ready && !_startedInControlZone)
                {
                    if (_downPos.y - pos.y > LaunchSwipePx) LaunchRequested = true; // swipe down
                }
                else if (state == GameState.Diving && !_startedInControlZone)
                {
                    MoveDeltaPixels = pos.x - _lastPos.x;
                }
                _lastPos = pos;
            }

            if (!pointerDown) _down = false;
        }
    }
}
