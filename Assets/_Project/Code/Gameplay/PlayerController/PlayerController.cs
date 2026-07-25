using UnityEngine;

namespace _Project.Code.Gameplay.PlayerController
{
    public class PlayerController : MonoBehaviour
    {
        public PlayerStateMachine MyStateMachine;
        public PlayerAnimator MyAnimator;
        [HideInInspector] public Vector2 DirectionalInput = Vector2.zero;
        [HideInInspector] public bool BatInputHeld;
        [HideInInspector] public Rigidbody RB;
        [HideInInspector] public bool CanTransform = true;

        [Header("Player Stats")]
        [SerializeField] public float WalkSpeed;
        [SerializeField] public float FlySpeed;
        [SerializeField] public float MistSpeed;
        [SerializeField] public float MistTime;
        [SerializeField] private float _maxBatTime;
        [SerializeField] private float _batTimeDrainRate;
        [SerializeField] private float _batTimeFillRate;
        private float _currentBatTime;
        [HideInInspector] public float LastBatBreakTime;
        [SerializeField] public float TimeAfterBreakToTransform;
        [SerializeField] public float TimeBetweenMist;
        private float _lastTransformationTime;
        [SerializeField] public float DefaultGravity;

        [Header("Jump")]
        [Tooltip("Upward velocity applied when jumping from the ground.")]
        [SerializeField] private float _jumpForce = 8f;


        [Header("GroundCheck")]
        [SerializeField] private float _groundCheckOffset;
        [SerializeField] private float _groundCheckDistance;
        [SerializeField] private string _groundLayerName;
        private int _groundLayerIndex;

        [Header("EatStats")]
        [SerializeField] private Vector3 _boxCastHalf;
        [SerializeField] private string _victimLayerName;
        [HideInInspector] public Collider EatCastHit;
        private Collider[] EatCastHits;
        private int _victimLayerIndex;
        [SerializeField] public float BloodDrainSpeed;

        [Header("Animation")]
        [Tooltip("Animator that plays the player's clips. Leave empty to auto-find one in the children.")]
        [SerializeField] private Animator _animator;



        private void Awake()
        {
            MyStateMachine = new PlayerStateMachine(this);
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            MyAnimator = new PlayerAnimator(_animator);
            _groundLayerIndex = LayerMask.GetMask(_groundLayerName);
            _victimLayerIndex = LayerMask.GetMask(_victimLayerName);
            RB = GetComponent<Rigidbody>();
            // Side-scroller: keep the body on the XY plane and stop it tipping over
            RB.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        }
        private void OnEnable()
        {
            MyStateMachine.Initialize(MyStateMachine.StateIdle);
            EventManager.TransformationChanged += ChangeBatInput;
            EventManager.JumpEvent += Jump;
            _currentBatTime = _maxBatTime;
            LastBatBreakTime = Time.time;
        }
        private void OnDisable()
        {
            MyStateMachine.Disable();
            EventManager.TransformationChanged -= ChangeBatInput;
            EventManager.JumpEvent -= Jump;
        }

        public void Update()
        {
            MyStateMachine.Execute();
        }
        void FixedUpdate()
        {
            MyStateMachine.FixedUpdate();
        }
        public bool IsGrounded()
        {
            return Physics.Raycast(transform.position + _groundCheckOffset * Vector3.down, Vector3.down, _groundCheckDistance, _groundLayerIndex);
        }

        // Grounded-only jump: launch upward, play the jump clip, and hand off to the falling state
        public void Jump()
        {
            if (!IsGrounded()) return;
            Vector3 v = RB.linearVelocity;
            v.y = _jumpForce;
            RB.linearVelocity = v;
            MyAnimator.PlayJump();
            MyStateMachine.ChangeState(MyStateMachine.StateFalling);
        }
        public void ChangeDI(Vector2 directionalInput)
        {
            DirectionalInput = directionalInput;
        }
        public void ChangeBatInput(bool batInputHeld)
        {
            BatInputHeld = batInputHeld;
            if (CanTransform && Time.time - _lastTransformationTime > TimeBetweenMist && Time.time - LastBatBreakTime > TimeAfterBreakToTransform)
            {
                MyStateMachine.ChangeState(MyStateMachine.StateMist);
            }
        }
        public bool ReduceBatTime()
        {
            _currentBatTime = Mathf.Clamp(_currentBatTime - _batTimeDrainRate * Time.deltaTime, 0, _maxBatTime);
            if (_currentBatTime == 0)
            {
                LastBatBreakTime = Time.time;
                return true;
            }
            return false;
        }
        public void IncreaseBatTime()
        {
            if (_currentBatTime >= _maxBatTime)
            {
                _currentBatTime = _maxBatTime;
            }
            else
            {
                _currentBatTime = Mathf.Clamp( _currentBatTime + _batTimeFillRate * Time.deltaTime, 0, _maxBatTime );
            }
        }
        public bool CheckForVictims()
        {
            EatCastHits = Physics.OverlapBox(transform.position, _boxCastHalf, Quaternion.identity, _victimLayerIndex);
            if (EatCastHits.Length > 0) { EatCastHit = EatCastHits[0]; return true; }
            else return false;
            //return Physics.BoxCast(transform.position, _boxCastHalf, new Vector3(0,0,1), out EatCastHit, Quaternion.identity, 20f, _victimLayerIndex);
        }
    }
}
