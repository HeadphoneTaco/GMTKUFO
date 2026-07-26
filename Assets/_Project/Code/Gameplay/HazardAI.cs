using UnityEngine;
using _Project.Code.Gameplay.PlayerController;

/// <summary>How a hazard behaves when it has not noticed the player.</summary>
public enum HazardIdleMode
{
    /// <summary>Sits where it was placed. Crocs, spike fences, anything that lurks.</summary>
    Stationary,

    /// <summary>Paces back and forth around its spawn point. Kangaroos.</summary>
    Patrol
}

/// <summary>
/// Gives a hazard a life. It idles or patrols, notices the player within range, chases, and
/// eventually gives up and goes home.
///
/// Deliberately works on anything with an Obstacle, including the fences, because a chainlink fence
/// sprinting at Dracula is funnier than one that does not.
///
/// Moves on X only and holds its Y and Z, matching the side-scroller plane. Everything is per
/// instance in the Inspector, so a croc can lurk slowly while a roo bounds.
/// </summary>
public class HazardAI : MonoBehaviour
{
    [Header("Idle Behaviour")]
    [SerializeField] private HazardIdleMode _idleMode = HazardIdleMode.Stationary;

    [Tooltip("How far either side of its spawn point it paces. Only used in Patrol mode.")]
    [SerializeField] private float _patrolRange = 6f;

    [Tooltip("Movement speed while patrolling.")]
    [SerializeField] private float _patrolSpeed = 2f;

    [Tooltip("Seconds it waits at each end of the patrol before turning around.")]
    [SerializeField] private float _patrolPause = 0.75f;

    [Header("Detection")]
    [Tooltip("Distance at which it notices the player. Distance only, nothing blocks sight.")]
    [SerializeField] private float _sightRange = 12f;

    [Tooltip("Distance at which it loses him. Larger than sight range on purpose, so a player " +
             "hovering right on the edge does not flicker between chasing and not chasing.")]
    [SerializeField] private float _loseRange = 18f;

    [Header("Chase")]
    [Tooltip("Speed while chasing. The player walks at 5. Below that he can always escape on " +
             "foot, above it he needs bat or mist form.")]
    [SerializeField] private float _chaseSpeed = 4f;

    [Tooltip("Longest it will chase before giving up, however close the player stays.")]
    [SerializeField] private float _maxChaseTime = 5f;

    [Tooltip("Never chase further than this from its spawn point. 0 means no limit.")]
    [SerializeField] private float _leashFromHome = 25f;

    [Tooltip("Seconds after giving up before it can notice the player again, so it does not " +
             "immediately re-latch onto someone standing next to it.")]
    [SerializeField] private float _reacquireDelay = 2f;

    [Tooltip("On: it walks back to where it started after giving up. Off: it carries on from " +
             "wherever it stopped.")]
    [SerializeField] private bool _returnHome = true;

    [Header("Ledges")]
    [Tooltip("On: it will not walk off the end of the ground. The walkway has real gaps, so " +
             "without this a chaser strolls into one and is gone.")]
    [SerializeField] private bool _stopAtLedges = true;

    [Tooltip("Set this to the Ground layer. Left empty every probe misses and it treats the whole " +
             "level as a ledge, so it never moves at all.")]
    [SerializeField] private LayerMask _groundLayers;

    [Tooltip("How far ahead it checks for floor. Roughly one body width.")]
    [SerializeField] private float _ledgeProbeAhead = 1.2f;

    [Header("Facing")]
    [SerializeField] private bool _turnToFaceMovement = true;
    [SerializeField] private float _yawFacingRight = 90f;
    [SerializeField] private float _yawFacingLeft = -90f;
    [SerializeField] private float _turnSpeed = 540f;

    [Header("Animation")]
    [Tooltip("Animator on the model. Leave empty to find one in children.")]
    [SerializeField] private Animator _animator;

    [Tooltip("Played while sitting still. Gator: AmericanAlligator_Idle. Roo: kangaroo_idle_01. " +
             "Both already loop.")]
    [SerializeField] private RuntimeAnimatorController _idleController;

    [Tooltip("Played while patrolling or walking home. Roo: kangaroo_trot_fwd_01, already loops. " +
             "Leave empty to fall back to the chase controller.")]
    [SerializeField] private RuntimeAnimatorController _patrolController;

    [Tooltip("Played while chasing. Gator: AmericanAlligator_Trot_F. Roo: kangaroo_sprint_fwd_01. " +
             "NEITHER of those loops by default, so tick Loop Time on the clip in the FBX import " +
             "settings or the chase freezes on the last frame.")]
    [SerializeField] private RuntimeAnimatorController _chaseController;

    [Header("Debug")]
    [Tooltip("Draws sight, lose and leash ranges in the Scene view when selected.")]
    [SerializeField] private bool _drawGizmos = true;

    private enum State { Idle, Patrol, Chase, Returning }

    private Transform _player;
    private float _homeX;
    private float _fixedY;
    private float _fixedZ;

    private State _state = State.Idle;
    private float _direction = 1f;
    private float _targetYaw;
    private float _pauseUntil;
    private float _chaseEndsAt;
    private float _canReacquireAt;
    private RuntimeAnimatorController _currentController;

    private void Start()
    {
        var pc = FindAnyObjectByType<PlayerController>();
        if (pc != null) _player = pc.transform;

        _homeX = transform.position.x;
        _fixedY = transform.position.y;
        _fixedZ = transform.position.z;
        _targetYaw = transform.eulerAngles.y;

        _state = _idleMode == HazardIdleMode.Patrol ? State.Patrol : State.Idle;

        if (_animator == null) _animator = GetComponentInChildren<Animator>(true);

        if (_stopAtLedges && _groundLayers.value == 0)
            Debug.LogWarning($"[HazardAI] '{name}' has Stop At Ledges on but no Ground Layers set, " +
                             "so every probe fails and it will never move. Set the mask.", this);
    }

    private void Update()
    {
        if (_player != null) UpdateState();

        switch (_state)
        {
            case State.Patrol: TickPatrol(); break;
            case State.Chase: TickChase(); break;
            case State.Returning: TickReturn(); break;
        }

        TickFacing();
        TickAnimation();
    }

    /// <summary>
    /// Swaps the Animator's controller to match the state, the same one-controller-per-action
    /// pattern the player uses. Only assigns on an actual change, because reassigning
    /// runtimeAnimatorController restarts the state machine and would hold the clip on frame one
    /// forever if done every frame.
    /// </summary>
    private void TickAnimation()
    {
        if (_animator == null) return;

        RuntimeAnimatorController want;
        if (_state == State.Chase)
            want = _chaseController;
        else if (_state == State.Patrol || _state == State.Returning)
            want = _patrolController != null ? _patrolController : _chaseController;
        else
            want = _idleController;

        if (want == null || want == _currentController) return;

        _animator.runtimeAnimatorController = want;
        _currentController = want;
    }

    // Acquire and give up. Kept separate from the movement so the transitions are readable.
    private void UpdateState()
    {
        float distance = Mathf.Abs(_player.position.x - transform.position.x);

        if (_state == State.Chase)
        {
            bool lostHim = distance > _loseRange;
            bool outOfTime = Time.time >= _chaseEndsAt;
            bool tooFarFromHome = _leashFromHome > 0f &&
                                  Mathf.Abs(transform.position.x - _homeX) > _leashFromHome;

            if (lostHim || outOfTime || tooFarFromHome)
            {
                _canReacquireAt = Time.time + _reacquireDelay;
                _state = _returnHome ? State.Returning
                                     : (_idleMode == HazardIdleMode.Patrol ? State.Patrol : State.Idle);
            }
            return;
        }

        if (Time.time < _canReacquireAt) return;

        if (distance <= _sightRange)
        {
            _state = State.Chase;
            _chaseEndsAt = Time.time + _maxChaseTime;
        }
    }

    private void TickPatrol()
    {
        if (Time.time < _pauseUntil) return;

        float x = transform.position.x;

        // Turn at the ends of the beat, or early if the floor runs out.
        if (x > _homeX + _patrolRange) TurnAround(-1f);
        else if (x < _homeX - _patrolRange) TurnAround(1f);
        else if (!CanStep(_direction)) TurnAround(-_direction);

        if (CanStep(_direction)) Step(_direction, _patrolSpeed);
    }

    private void TickChase()
    {
        float dx = _player.position.x - transform.position.x;
        if (Mathf.Abs(dx) < 0.05f) return;

        float dir = Mathf.Sign(dx);
        _direction = dir;

        // Stop at the edge rather than following him into a gap. Standing at the lip still reads
        // as menacing, and the alternative is hazards quietly deleting themselves off the level.
        if (CanStep(dir)) Step(dir, _chaseSpeed);
    }

    private void TickReturn()
    {
        float dx = _homeX - transform.position.x;

        if (Mathf.Abs(dx) < 0.1f)
        {
            transform.position = new Vector3(_homeX, _fixedY, _fixedZ);
            _state = _idleMode == HazardIdleMode.Patrol ? State.Patrol : State.Idle;
            return;
        }

        float dir = Mathf.Sign(dx);
        _direction = dir;
        if (CanStep(dir)) Step(dir, _patrolSpeed);
        else _state = _idleMode == HazardIdleMode.Patrol ? State.Patrol : State.Idle;
    }

    private void TurnAround(float newDirection)
    {
        _direction = newDirection;
        _pauseUntil = Time.time + _patrolPause;
    }

    // X only, holding the authored Y and Z so nothing drifts off the plane the player is frozen to.
    private void Step(float dir, float speed)
    {
        float x = transform.position.x + dir * speed * Time.deltaTime;
        transform.position = new Vector3(x, _fixedY, _fixedZ);
    }

    /// <summary>Is there floor one body width ahead in this direction.</summary>
    private bool CanStep(float dir)
    {
        if (!_stopAtLedges) return true;

        Vector3 probe = new Vector3(transform.position.x + dir * _ledgeProbeAhead,
                                    _fixedY + 1f, _fixedZ);
        return Physics.Raycast(probe, Vector3.down, 4f, _groundLayers, QueryTriggerInteraction.Ignore);
    }

    private void TickFacing()
    {
        if (!_turnToFaceMovement) return;

        _targetYaw = _direction >= 0f ? _yawFacingRight : _yawFacingLeft;
        Quaternion target = Quaternion.Euler(0f, _targetYaw, 0f);

        transform.rotation = _turnSpeed <= 0f
            ? target
            : Quaternion.RotateTowards(transform.rotation, target, _turnSpeed * Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        if (!_drawGizmos) return;

        float homeX = Application.isPlaying ? _homeX : transform.position.x;
        Vector3 home = new Vector3(homeX, transform.position.y, transform.position.z);

        // Sight and lose ranges, drawn as horizontal spans because detection is X distance only.
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position + Vector3.left * _sightRange,
                        transform.position + Vector3.right * _sightRange);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.7f);
        Gizmos.DrawLine(transform.position + Vector3.left * _loseRange + Vector3.up * 0.3f,
                        transform.position + Vector3.right * _loseRange + Vector3.up * 0.3f);

        if (_idleMode == HazardIdleMode.Patrol)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(home + Vector3.left * _patrolRange + Vector3.up * 0.6f,
                            home + Vector3.right * _patrolRange + Vector3.up * 0.6f);
        }

        if (_leashFromHome > 0f)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(home + Vector3.left * _leashFromHome + Vector3.up * 0.9f,
                            home + Vector3.right * _leashFromHome + Vector3.up * 0.9f);
        }
    }
}
