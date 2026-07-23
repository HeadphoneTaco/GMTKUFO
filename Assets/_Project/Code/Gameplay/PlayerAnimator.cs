using UnityEngine;

namespace _Project.Code.Gameplay
{
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] private PlayerController.PlayerController playerController;
        [SerializeField] private Animator anim;

        private Vector3 _playerVelocity;

        private void Update()
        {
            anim.SetBool("IsGrounded", playerController.IsGrounded());
            _playerVelocity += playerController.GetPlayerVelocity();
            _playerVelocity.y = 0;
            anim.SetFloat("Velocity", playerController.GetPlayerVelocity().sqrMagnitude);
        }

        private void OnEnable()
        {
            playerController.OnJumpEvent += OnJump;
        }

        private void OnDisable()
        {
            playerController.OnJumpEvent -= OnJump;
        }

        private void OnJump()
        {
            anim.SetTrigger("Jump");
        }
    }
}