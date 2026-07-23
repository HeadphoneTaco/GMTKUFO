using System;
using UnityEngine;

namespace _Project.Code.Gameplay.PlayerController
{
    public class PlayerController : MonoBehaviour
    {
        public PlayerStateMachine MyStateMachine;
        [HideInInspector] public Vector2 DirectionalInput;
        [HideInInspector] public bool BatInputHeld;

        // Raised when the player jumps. States that trigger a jump should call InvokeJump().
        public event Action OnJumpEvent;

        [Header("Physics")]
        [SerializeField] private Rigidbody _rb;

        [Header("GroundCheck")]
        [SerializeField] private float _groundCheckOffset;
        [SerializeField] private float _groundCheckDistance;
        [SerializeField] private string _groundLayerName;
        private int _groundLayerMask;



        private void Awake()
        {
            MyStateMachine = new PlayerStateMachine(this);
            _groundLayerMask = LayerMask.GetMask(_groundLayerName);
            if (_rb == null)
            {
                _rb = GetComponent<Rigidbody>();
            }
        }
        private void OnEnable()
        {
            MyStateMachine.Initialize(MyStateMachine.StateIdle);
        }
        private void OnDisable()
        {
            MyStateMachine.Disable();
        }

        public void Update()
        {
            MyStateMachine.Execute();
        }

        public bool IsGrounded()
        {
            return Physics.Raycast(transform.position + _groundCheckOffset * Vector3.down, Vector3.down, _groundCheckDistance, _groundLayerMask);
        }

        public Vector3 GetPlayerVelocity()
        {
            return _rb != null ? _rb.linearVelocity : Vector3.zero;
        }

        // Call from a state when the player jumps so listeners like PlayerAnimator can react.
        public void InvokeJump()
        {
            OnJumpEvent?.Invoke();
        }
    }
}
