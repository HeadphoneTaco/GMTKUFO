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
        // Blood is the health pool. It lives on GameManager as the score, so there is no MaxHealth
        // or _currentHealth here any more. Hazards call TakeDamage, which spends blood, and the
        // run ends when it hits zero.
        [SerializeField] private float _invincibilityTime = 1f;
        private bool IsInvincible;

        [Tooltip("Seconds after a hit during which the movement states stop overwriting horizontal " +
                 "velocity, so the knockback is actually visible. Walk and Idle both assign x " +
                 "every frame, which otherwise erases it before it moves the player at all.")]
        [SerializeField] private float _knockbackTime = 0.25f;
        private float _knockbackUntil;

        /// <summary>True while a recent hit's knockback should be left alone by the movement states.</summary>
        public bool IsKnockedBack => Time.time < _knockbackUntil;

        /// <summary>Bat time left as 0 to 1, for the HUD meter. Guards a zero max, which would divide by zero.</summary>
        public float BatTimeNormalized => _maxBatTime <= 0f ? 0f : Mathf.Clamp01(_currentBatTime / _maxBatTime);

        [Header("Jump")]
        [Tooltip("Upward velocity applied when jumping from the ground.")]
        [SerializeField] private float _jumpForce = 8f;


        [Header("GroundCheck")]
        [Tooltip("On: offset and distance are computed from the capsule at Awake and the two values " +
                 "below are ignored. Off: the values below are used as typed.")]
        [SerializeField] private bool _autoSizeGroundCheck = true;

        [Tooltip("How far above the capsule's bottom the ray starts. It must start INSIDE the " +
                 "capsule. A ray starting at or below the feet begins under the floor and cannot " +
                 "detect it.")]
        [SerializeField] private float _groundRayInset = 0.15f;

        [Tooltip("How far past the capsule's bottom the ray reaches. This is the real tolerance " +
                 "for slopes, bumps and settling.")]
        [SerializeField] private float _groundRayReach = 0.15f;

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
        [Tooltip("How close the player is pulled to the victim when a bite starts. The bite can " +
                 "trigger from up to _boxCastHalf.x away, which reads as biting thin air. Keep " +
                 "this above the two collider radii added together, about 0.7 here, or they end " +
                 "up inside each other and physics shoves the player back out of the bite.")]
        [SerializeField] public float BiteStandoff = 0.75f;

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
            ConfigureGroundCheck();
        }

        /// <summary>
        /// Derives the ground ray from the capsule instead of trusting hand-typed numbers.
        ///
        /// Those numbers have to track the collider, and nothing enforces it. Resizing the capsule
        /// silently breaks IsGrounded, which surfaces much later as the player being stuck in the
        /// falling animation, so the symptom points nowhere near the cause. That has cost time
        /// three separate times on this project.
        ///
        /// Two failure modes, and the safe window between them is narrow:
        ///   offset too small  -> the ray stops inside the capsule, above the floor, never hits
        ///   offset too large  -> the ray starts below the feet, under the floor, never hits
        /// </summary>
        private void ConfigureGroundCheck()
        {
            if (!_autoSizeGroundCheck) return;

            var capsule = GetComponent<CapsuleCollider>();
            if (capsule == null)
            {
                Debug.LogWarning("[PlayerController] Auto ground check needs a CapsuleCollider on " +
                                 "this object. Falling back to the values in the Inspector.", this);
                return;
            }

            // Distance from the transform origin down to the bottom of the capsule, in world units.
            float scaleY = Mathf.Abs(transform.lossyScale.y);
            float bottom = -(capsule.center.y - capsule.height * 0.5f) * scaleY;

            _groundCheckOffset = bottom - _groundRayInset;
            _groundCheckDistance = _groundRayInset + _groundRayReach;

            if (_groundCheckOffset <= 0f)
                Debug.LogWarning($"[PlayerController] Capsule bottom is only {bottom:F3} below the " +
                                 $"origin, less than the {_groundRayInset} inset, so the ray would " +
                                 "start above the origin. Lower the inset.", this);
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
            // Bail before the knockback too. Applying it while invincible let a hazard shove the
            // player around repeatedly during the very window meant to protect them.
            if (IsInvincible) return;

            // Replace horizontal velocity rather than adding to it. The player is usually running
            // into the hazard, so adding would partly cancel the push out.
            Vector3 v = RB.linearVelocity;
            v.x = BounceDirection.x;
            v.y = Mathf.Max(v.y, 0f) + BounceDirection.y;
            RB.linearVelocity = v;

            _knockbackUntil = Time.time + _knockbackTime;

            // Blood is the health pool, so a hit spends blood. RemoveBlood ends the run itself
            // when it reaches zero, which is why there is no death check here. Instance rather
            // than Exists, because Exists stays false until something forces the lazy creation.
            GameManager.Instance.RemoveBlood(damage);

            StartCoroutine(InvincibilityTime());
        }
        public IEnumerator InvincibilityTime()
        {
            IsInvincible = true;
            yield return new WaitForSeconds(_invincibilityTime);
            IsInvincible = false;
        }
    }
}
