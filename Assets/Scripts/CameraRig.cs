using UnityEngine;

namespace SundaeDiver
{
    /// <summary>
    /// Locks the camera to the banana on Y (X stays centred). This produces the
    /// "camera tracks the banana, world scrolls past" feel from the brief without
    /// any faux-scrolling: the banana really falls, the camera follows.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public Transform target;
        public GameConfig config;
        [Tooltip("Turn OFF to keep the camera still and watch the banana physically fall (debug).")]
        public bool follow = true;

        private Camera _cam;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            if (_cam != null)
            {
                _cam.orthographic = true;
                if (config != null) _cam.orthographicSize = config.orthoSize;
            }
        }

        private void LateUpdate()
        {
            if (!follow || target == null || config == null) return;
            // Place banana at bananaScreenYFactor from the top:
            //   bananaY = camY + orthoSize * (1 - 2f)  ->  camY = bananaY - orthoSize*(1-2f)
            float offset = config.orthoSize * (1f - 2f * config.bananaScreenYFactor);
            var p = transform.position;
            p.y = target.position.y - offset;
            p.z = -10f;
            transform.position = p;
        }
    }
}
