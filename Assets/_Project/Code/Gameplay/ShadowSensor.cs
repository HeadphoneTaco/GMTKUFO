using _Project.Code.Core;
using UnityEngine;

namespace _Project.Code.Gameplay
{
    /// <summary>
    /// Works out whether the player is standing in shade by casting toward the sun and seeing what
    /// gets in the way. Reports cover to the GameManager, which uses it to slow the daylight blood
    /// drain after sunrise. Put this on the player.
    /// </summary>
    public class ShadowSensor : MonoBehaviour
    {
        [Tooltip("The sun to test against. Leave empty to find the SunController's light at Start.")]
        [SerializeField] private Light _sun;

        [Tooltip("What counts as cover. Buildings and structures, not victims or hazards. Leaving " +
                 "this as Everything means the player's own collider can block the ray.")]
        [SerializeField] private LayerMask _occluderLayers = ~0;

        [Tooltip("Offset from this transform's origin to cast from, roughly chest height. The " +
                 "origin sits low on the player, so casting from it can clip the ground.")]
        [SerializeField] private Vector3 _castOffset = new Vector3(0f, 0.5f, 0f);

        [Tooltip("Seconds between checks. The result is smoothed, so this can be well above one " +
                 "frame without the drain rate visibly stepping.")]
        [SerializeField] private float _checkInterval = 0.1f;

        [Tooltip("How fast reported cover moves toward the measured value. Smoothing stops the " +
                 "drain rate flickering when walking past railings or thin posts.")]
        [SerializeField] private float _smoothing = 6f;

        [Tooltip("Draws the ray in the Scene view while playing.")]
        [SerializeField] private bool _drawRay;

        private float _nextCheck;
        private float _target;
        private float _current;

        private void Start()
        {
            if (_sun == null)
            {
                var controller = FindAnyObjectByType<SunController>();
                if (controller != null) _sun = controller.GetComponentInChildren<Light>();
            }

            if (_sun == null)
                Debug.LogWarning("[ShadowSensor] No sun found, so cover will always read as 0 and " +
                                 "the daylight drain will always run at the full sun rate.", this);
        }

        private void Update()
        {
            if (Time.time >= _nextCheck)
            {
                _nextCheck = Time.time + Mathf.Max(0f, _checkInterval);
                _target = MeasureCover();
            }

            // Smooth toward the measured value so the drain rate does not jump frame to frame.
            _current = Mathf.MoveTowards(_current, _target, _smoothing * Time.deltaTime);
            GameManager.Instance.SetShadowAmount(_current);
        }

        // 1 means fully covered, 0 means standing in open sun.
        private float MeasureCover()
        {
            if (_sun == null) return 0f;

            Vector3 origin = transform.position + _castOffset;
            Vector3 toSun = _sun.transform.position - origin;
            float distance = toSun.magnitude;
            if (distance < 0.01f) return 0f;

            Vector3 direction = toSun / distance;
            bool blocked = Physics.Raycast(origin, direction, distance, _occluderLayers,
                                           QueryTriggerInteraction.Ignore);

            if (_drawRay)
                Debug.DrawRay(origin, direction * distance, blocked ? Color.blue : Color.yellow);

            return blocked ? 1f : 0f;
        }
    }
}
