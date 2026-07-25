using System;
using Unity.VisualScripting;
using UnityEngine;

namespace _Project.Code.Gameplay.CountController
{
    public class CountWalkState :CountState
    {

        public CountWalkState(CountController controller) : base(controller)
        {
        }

        public override void Enter()
        {
            _countController.CountInputs.OnMove += HandleMove;
            //in both, set up your animator
        }

        private void HandleMove(Vector2 obj)
        {
            _countController.MoveInput = obj;
            //handle move
        }

        public override void Update()
        {
            
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            var xVelocity =
                Mathf.SmoothStep(_countController.RB.linearVelocity.x, _countController.MoveInput.x,
                    .7f); //change .7f later
            _countController.RB.linearVelocity = new Vector3(xVelocity, _countController.RB.linearVelocity.y, 0f);
            if(_countController.RB.linearVelocity.magnitude < 0.1f)
                _countController.StateMachine.TransitionToState(CountStateValue.IdleState);
        }


        public override void Exit()
        {
            _countController.CountInputs.OnMove -= HandleMove;
        }
    }
}