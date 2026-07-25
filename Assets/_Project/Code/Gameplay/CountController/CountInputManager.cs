using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Project.Code.Gameplay.CountController
{
    public class CountInputManager : MonoBehaviour
    {
        public event Action<Vector2> OnMove;
        public event Action<bool> OnJump;
        // OnEat removed: nothing raised or subscribed to it, and there is no Eat action in
        // PlayerInputs to raise it from. Biting is automatic on proximity now, so an eat input is
        // not planned. Add it back alongside a real input action if that changes.


        private PlayerInputs _playerInputs;
        private void Awake()
        {
            _playerInputs = new PlayerInputs();
        }

        void OnEnable()
        {
            _playerInputs.Player.Move.performed += OnMovePerformed;
            _playerInputs.Player.Move.canceled += OnMoveCanceled;
            _playerInputs.Player.Transform.performed += OnJumpPerformed;
            _playerInputs.Player.Transform.canceled += OnMoveCanceled;
            _playerInputs.Enable();
        }
        
        void OnDisable()
        {
            _playerInputs.Player.Move.performed -= OnMovePerformed;
            _playerInputs.Player.Move.canceled -= OnMoveCanceled;
            _playerInputs.Player.Transform.performed -= OnJumpPerformed;
            _playerInputs.Player.Transform.canceled -= OnMoveCanceled;
            _playerInputs.Enable();
        }

        private void OnMovePerformed(InputAction.CallbackContext obj)=>OnMove?.Invoke(obj.ReadValue<Vector2>());
        private void OnMoveCanceled(InputAction.CallbackContext obj)=>OnMove?.Invoke(new Vector2());
        private void OnJumpPerformed(InputAction.CallbackContext obj)=>OnJump?.Invoke(obj.ReadValue<bool>());
    }
}