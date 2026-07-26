using UnityEngine;
using _Project.Code.Gameplay.PlayerController;

/// <summary>
/// Side scroller follow camera with directional look ahead. Put this on Main Camera.
///
/// Stands in for a Cinemachine vcam, which is not installed in this project. Nothing else depends
/// on it, so swapping to Cinemachine later means deleting this component and building a rig.
/// </summary>
public class FollowCamera : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Leave empty to find the player by type at Awake.")]
    [SerializeField] private Transform _target;

    [Tooltip("Camera position relative to the target before look ahead is applied. X is normally 0 " +
             "so the lead comes entirely from look ahead.")]
    [SerializeField] private Vector3 _offset = new Vector3(0f, 0.08f, -11.61f);

    [Header("Follow")]
    [Tooltip("Roughly how long the camera takes to catch up horizontally. Higher is lazier.")]
    [SerializeField] private float _smoothTime = 0.25f;

    [Header("Vertical")]
    [Tooltip("Off: the camera holds its starting height. On: it tracks upward when the player " +
             "leaves the deadzone, which is what keeps the bat on screen while flying.")]
    [SerializeField] private bool _followY = true;

    [Tooltip("How far the player can rise above the camera's focus before it starts climbing. " +
             "This is what stops ordinary hops from lurching the whole frame.")]
    [SerializeField] private float _deadzoneUp = 1.5f;

    [Tooltip("How far the player can drop below the focus before the camera follows down. Usually " +
             "larger than the upward band, so falling reads as falling.")]
    [SerializeField] private float _deadzoneDown = 3f;

    [Tooltip("Vertical catch up time. Slower than horizontal reads calmer.")]
    [SerializeField] private float _verticalSmoothTime = 0.35f;

    [Tooltip("On: the camera never drops below its starting height, so it does not sink into the " +
             "ground when the player falls into a gap.")]
    [SerializeField] private bool _clampToStartHeight = true;

    [Tooltip("Hard limit on how far the camera may trail its target height. Bat flight accelerates " +
             "without a cap, and smoothing alone lags by roughly speed times smooth time, which at " +
             "flight speeds is enough to lose the player off the top of the screen. 0 disables.")]
    [SerializeField] private float _maxVerticalLag = 6f;

    [Header("Look Ahead")]
    [Tooltip("How far the camera leads in the direction of travel, so hazards are visible sooner.")]
    [SerializeField] private float _lookAheadDistance = 6f;

    [Tooltip("How quickly the lead swings around when the player turns. Too fast reads as a jerk, " +
             "too slow feels like the camera is dragging.")]
    [SerializeField] private float _lookAheadSmoothTime = 0.5f;

    [Tooltip("Horizontal speed below which the player counts as stationary. Without this the lead " +
             "jitters left and right while standing still.")]
    [SerializeField] private float _speedDeadzone = 0.2f;

    [Tooltip("On: the lead holds its last direction when the player stops, instead of drifting back " +
             "to centre. Usually reads better in a runner.")]
    [SerializeField] private bool _holdLeadWhenStopped = true;

    private Rigidbody _targetBody;
    private float _fixedY;
    private float _focusY;
    private float _lookAhead;
    private float _lookAheadVelocity;
    private Vector3 _followVelocity;
    private float _verticalVelocity;
    private float _lastDirection = 1f;

    private void Awake()
    {
        if (_target == null)
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null) _target = player.transform;
        }

        if (_target == null)
        {
            Debug.LogError("[FollowCamera] No target and no PlayerController in the scene. The " +
                           "camera will not move.", this);
            enabled = false;
            return;
        }

        _targetBody = _target.GetComponent<Rigidbody>();
        _fixedY = transform.position.y;

        // Seed the focus on the player. Left at 0 the camera would think the player was far above
        // it on the first frame and lurch upward before settling.
        _focusY = _target.position.y;

        // Start already framed, so the first frame does not slide in from wherever the camera was
        // left in the editor.
        transform.position = Desired(instant: true);
    }

    // LateUpdate, so the camera moves after the player has finished moving this frame. Following in
    // Update leaves the camera one frame behind and shows up as jitter at speed.
    private void LateUpdate()
    {
        float speedX = _targetBody != null ? _targetBody.linearVelocity.x : 0f;

        if (Mathf.Abs(speedX) > _speedDeadzone)
            _lastDirection = Mathf.Sign(speedX);
        else if (!_holdLeadWhenStopped)
            _lastDirection = 0f;

        _lookAhead = Mathf.SmoothDamp(_lookAhead, _lastDirection * _lookAheadDistance,
                                      ref _lookAheadVelocity, _lookAheadSmoothTime);

        UpdateFocusY();

        Vector3 desired = Desired(instant: false);

        // Horizontal and depth are damped together. Vertical is damped separately and more slowly,
        // so climbing after the bat reads as calm rather than snapping.
        float y = transform.position.y;
        Vector3 flat = Vector3.SmoothDamp(transform.position, desired, ref _followVelocity, _smoothTime);

        float newY = _followY
            ? Mathf.SmoothDamp(y, desired.y, ref _verticalVelocity, _verticalSmoothTime)
            : _fixedY;

        // Smoothing alone cannot keep up with an uncapped climb, so cap the trail distance. This
        // is what guarantees the bat stays on screen no matter how fast it is going.
        if (_followY && _maxVerticalLag > 0f)
            newY = Mathf.Clamp(newY, desired.y - _maxVerticalLag, desired.y + _maxVerticalLag);

        if (_followY && _clampToStartHeight) newY = Mathf.Max(newY, _fixedY);

        transform.position = new Vector3(flat.x, newY, flat.z);
    }

    /// <summary>
    /// Vertical deadzone. The camera keeps a focus height and only moves it when the player leaves
    /// the band around it. Without this the camera tracks every small hop and the whole frame
    /// bounces while simply walking, which is far worse than a static camera.
    ///
    /// The band is what lets one camera serve both forms: on foot the player stays inside it and
    /// the view holds still, in bat form he climbs out of it and the camera comes along.
    /// </summary>
    private void UpdateFocusY()
    {
        if (!_followY) return;

        float ty = _target.position.y;

        if (ty > _focusY + _deadzoneUp) _focusY = ty - _deadzoneUp;
        else if (ty < _focusY - _deadzoneDown) _focusY = ty + _deadzoneDown;
    }

    private Vector3 Desired(bool instant)
    {
        Vector3 p = _target.position + _offset;
        p.x += instant ? 0f : _lookAhead;
        p.y = _followY ? _focusY + _offset.y : _fixedY;
        return p;
    }

    private void OnDrawGizmosSelected()
    {
        if (_target == null) return;

        // The framing line, so the offset can be judged without entering play mode.
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, _target.position);

        // How far the lead reaches in each direction.
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.8f);
        Vector3 c = _target.position + _offset;
        Gizmos.DrawWireSphere(c + Vector3.right * _lookAheadDistance, 0.4f);
        Gizmos.DrawWireSphere(c + Vector3.left * _lookAheadDistance, 0.4f);

        // The vertical deadzone, drawn at the player's Z so it can be judged against a jump arc.
        if (!_followY) return;
        float focus = Application.isPlaying ? _focusY : _target.position.y;
        Gizmos.color = new Color(0f, 0.9f, 1f, 0.9f);
        Vector3 up = new Vector3(_target.position.x, focus + _deadzoneUp, _target.position.z);
        Vector3 dn = new Vector3(_target.position.x, focus - _deadzoneDown, _target.position.z);
        Gizmos.DrawLine(up + Vector3.left * 4f, up + Vector3.right * 4f);
        Gizmos.DrawLine(dn + Vector3.left * 4f, dn + Vector3.right * 4f);
    }
}
