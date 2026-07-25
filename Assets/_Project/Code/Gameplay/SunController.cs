using _Project.Code.Core;
using UnityEngine;

namespace _Project.Code.Gameplay
{
    /// <summary>
    /// The sunrise. A spotlight that slowly rises into position across the night, so the buildings
    /// throw shadows the player can hide in. Driven by GameManager.NightProgress, not by its own
    /// timer, so it stays in step with the clock no matter what the sunrise duration is set to.
    /// </summary>
    public class SunController : MonoBehaviour
    {
        [Tooltip("The spotlight to move. Leave empty to use a Light on this object.")]
        [SerializeField] private Light _sun;

        [Header("Rise")]
        [Tooltip("Where the sun sits at dusk. Put this below the horizon, out of sight.")]
        [SerializeField] private Transform _riseFrom;

        [Tooltip("Where the sun sits at sunrise, fully up. Position and rotation are both used.")]
        [SerializeField] private Transform _riseTo;

        [Tooltip("Shapes the climb across the night. Linear is a steady rise. An ease in keeps it " +
                 "down for most of the run and rushes up at the end.")]
        [SerializeField] private AnimationCurve _riseCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Brightness")]
        [Tooltip("Intensity at dusk. 0 keeps the sun invisible until it starts to climb.")]
        [SerializeField] private float _duskIntensity;

        [Tooltip("Intensity at sunrise, fully up.")]
        [SerializeField] private float _sunriseIntensity = 3f;

        [Tooltip("Colour ramp across the night. Left is dusk, right is sunrise.")]
        [SerializeField] private Gradient _colourOverNight;

        [Header("Aim")]
        [Tooltip("Optional. Keeps the spotlight pointed at this while it rises, usually the player. " +
                 "Leave empty to use the rotations on the two markers instead.")]
        [SerializeField] private Transform _aimTarget;

        private bool _warned;

        private void Awake()
        {
            if (_sun == null) _sun = GetComponent<Light>();
        }

        private void Update()
        {
            if (_sun == null || _riseFrom == null || _riseTo == null)
            {
                WarnOnce();
                return;
            }

            // NightProgress is 1 at dusk and 0 at sunrise, so this climbs from 0 to 1.
            float t = _riseCurve.Evaluate(1f - GameManager.Instance.NightProgress);

            _sun.transform.position = Vector3.Lerp(_riseFrom.position, _riseTo.position, t);

            if (_aimTarget != null)
                _sun.transform.rotation = Quaternion.LookRotation(
                    (_aimTarget.position - _sun.transform.position).normalized);
            else
                _sun.transform.rotation = Quaternion.Slerp(_riseFrom.rotation, _riseTo.rotation, t);

            _sun.intensity = Mathf.Lerp(_duskIntensity, _sunriseIntensity, t);

            // A Gradient with no keys evaluates to black, which would silently kill the light.
            if (_colourOverNight != null && _colourOverNight.colorKeys.Length > 0)
                _sun.color = _colourOverNight.Evaluate(t);
        }

        private void WarnOnce()
        {
            if (_warned) return;
            _warned = true;
            Debug.LogWarning("[SunController] Needs a Light plus both Rise From and Rise To markers. " +
                             "Without all three the sun will not move at all.", this);
        }

        private void OnDrawGizmosSelected()
        {
            if (_riseFrom == null || _riseTo == null) return;

            // The path the sun travels, so it can be placed without entering play mode.
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(_riseFrom.position, _riseTo.position);
            Gizmos.DrawWireSphere(_riseFrom.position, 0.5f);
            Gizmos.DrawWireSphere(_riseTo.position, 1f);
        }
    }
}
