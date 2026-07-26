using UnityEngine;

namespace _Project.Code.Gameplay
{
    // Drives the player's visuals. The player object holds two models (humanoid vampire + bat),
    // each with its own Animator. Marina's animations are one controller per action, so humanoid
    // states swap the vampire Animator's runtimeAnimatorController, and transforming toggles which
    // model is visible. PlayerController owns one of these and the states call into it.
    public class PlayerAnimator
    {
        private readonly GameObject _humanoidModel;
        private readonly GameObject _batModel;
        private readonly Animator _humanoidAnim;

        private readonly RuntimeAnimatorController _idle;
        private readonly RuntimeAnimatorController _running;
        private readonly RuntimeAnimatorController _falling;
        private readonly RuntimeAnimatorController _landing;
        private readonly RuntimeAnimatorController _attacking;

        public PlayerAnimator(
            GameObject humanoidModel, Animator humanoidAnim, GameObject batModel,
            RuntimeAnimatorController idle, RuntimeAnimatorController running,
            RuntimeAnimatorController falling, RuntimeAnimatorController landing,
            RuntimeAnimatorController attacking)
        {
            _humanoidModel = humanoidModel;
            _humanoidAnim = humanoidAnim;
            _batModel = batModel;
            _idle = idle;
            _running = running;
            _falling = falling;
            _landing = landing;
            _attacking = attacking;
        }

        // Humanoid actions: swap the vampire Animator's controller to Marina's matching clip.
        public void PlayIdle() => SetHumanoid(_idle);
        public void PlayRunning() => SetHumanoid(_running);
        public void PlayFalling() => SetHumanoid(_falling);
        public void PlayLanding() => SetHumanoid(_landing);
        public void PlayAttack() => SetHumanoid(_attacking);

        private void SetHumanoid(RuntimeAnimatorController controller)
        {
            if (_humanoidAnim == null || controller == null) return;
            _humanoidAnim.runtimeAnimatorController = controller;
        }

        // Transform: show one rig and hide the other. Call at the point the transition effect hides the swap.
        public void ShowHumanoid()
        {
            if (_batModel != null) _batModel.SetActive(false);
            if (_humanoidModel != null) _humanoidModel.SetActive(true);
        }

        public void ShowBat()
        {
            if (_humanoidModel != null) _humanoidModel.SetActive(false);
            if (_batModel != null) _batModel.SetActive(true);
        }
    }
}
