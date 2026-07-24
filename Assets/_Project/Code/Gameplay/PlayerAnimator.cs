using UnityEngine;

namespace _Project.Code.Gameplay
{
    // Thin animation driver. PlayerController owns one of these (like its state machine)
    // and the player states call these methods at the right moments. Not a MonoBehaviour:
    // it just wraps an Animator and turns game state into animator parameters.
    public class PlayerAnimator
    {
        private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
        private static readonly int Velocity = Animator.StringToHash("Velocity");
        private static readonly int Jump = Animator.StringToHash("Jump");

        private readonly Animator _anim;

        public PlayerAnimator(Animator anim)
        {
            _anim = anim;
        }

        public void SetGrounded(bool grounded)
        {
            if (_anim == null) return;
            _anim.SetBool(IsGrounded, grounded);
        }

        public void SetSpeed(float speed)
        {
            if (_anim == null) return;
            _anim.SetFloat(Velocity, speed);
        }

        public void PlayJump()
        {
            if (_anim == null) return;
            _anim.SetTrigger(Jump);
        }
    }
}
