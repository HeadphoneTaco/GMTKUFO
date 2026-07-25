using _Project.Code.Core;
using System.Collections;
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
        [SerializeField] private float MaxHealth;
        private float _currentHealth;
        [SerializeField] private float _healSpeed;
        [SerializeField] private float _invincibilityTime;
        private bool IsInvincible;

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
        [Tooltip("The humanoid (vampire) model root, shown in humanoid form.")]
        [SerializeField] private GameObject _humanoidModel;
        [Tooltip("Animator on the vampire model. Its controller is swapped per state.")]
        [SerializeField] private Animator _humanoidAnimator;
        [Tooltip("The bat model root, shown in bat form.")]
        [SerializeField] private GameObject _batModel;

        [Header("Vampire Animator Controllers")]
        [SerializeField] private RuntimeAnimatorController _idleController;
        [SerializeField] private RuntimeAnimatorController _runningController;
        [SerializeField] private RuntimeAnimatorController _fallingController;
        [SerializeField] private RuntimeAnimatorController _landingController;
        [SerializeField] private RuntimeAnimatorController _attackingController;



        private void Awake()
        {
            MyStateMachine = new PlayerStateMachine(this);
            MyAnimator = new PlayerAnimator(
                _humanoidModel, _humanoidAnimator, _batModel,
                _idleController, _runningController, _fallingController,
                _landingController, _attackingController);
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
            _currentHealth = MaxHealth;
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
            // No dedicated jump clip; the Falling state swaps in the falling controller
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
        public void TakeDamage(float damage, Vector3 BounceDirection)
        {
            RB.linearVelocity += BounceDirection ;
            if (!IsInvincible) { 
            if (_currentHealth < damage)
            {
                _currentHealth = 0;
                GameManager.Instance.EndRun();
            }
            else
            {
                _currentHealth -= damage;
            }
            }
        }
        public IEnumerator InvincibilityTime()
        {
            IsInvincible = true;
            yield return new WaitForSeconds(_invincibilityTime);
            IsInvincible = false;
        }
    }
}
