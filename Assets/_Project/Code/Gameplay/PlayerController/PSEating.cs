using _Project.Code.Core;
using Unity.VisualScripting;
using UnityEngine;

namespace _Project.Code.Gameplay.PlayerController
{
public class PSEating : IState
{
    private PlayerController _player;
    private Victim _victim;
        private float _drainAmount;
        private bool _drainFinished;
    
    public PSEating(PlayerController player)
    {
        _player = player;
    }
    // play the eating animation
    // exit into Idle
    // 
    public void Enter()
    {
            _player.CanTransform = false;

            EventManager.DIEvent += ChangeDI;
            _drainAmount = 0;
            _drainFinished = false;
        _victim = _player.EatCastHit.GetComponent<Victim>();
        if (_victim == null) { _player.MyStateMachine.ChangeState(_player.MyStateMachine.StateIdle); }
            else if (_victim.GetBit()) 
                _player.MyStateMachine.ChangeState(_player.MyStateMachine.StateIdle);
        else
            {
                _player.MyAnimator.PlayAttack();
                _player.RB.linearVelocity = Vector3.zero;
                // No repositioning on enter. The overlap check has already confirmed the victim
                // is in reach, and snapping onto the victim's exact x put the two colliders in
                // the same place, which physics resolved by shoving the player back out again.
            }
    }

    public void Execute()
    {
            // Never sit in this state without something to drain. Returning instead of leaving
            // would strand the player here with the attack animation looping.
            if (_victim == null)
            {
                _player.MyStateMachine.ChangeState(_player.MyStateMachine.StateIdle);
                return;
            }

            // Hold still for the whole drain. Horizontal only, so a bite that started mid air
            // still falls to the ground instead of hanging there. Input is already ignored,
            // because this state's ChangeDI deliberately does nothing.
            Vector3 v = _player.RB.linearVelocity;
            v.x = 0;
            _player.RB.linearVelocity = v;

        _player.IncreaseBatTime();
            (_drainFinished,_drainAmount) = _victim.DrainBlood(_player.BloodDrainSpeed * Time.deltaTime);

            // Instance lazily creates the manager when the scene is entered directly, so this is
            // safe without an Exists guard. Guarding it would skip scoring entirely in that case,
            // because Exists stays false until something touches Instance.
            GameManager.Instance.AddBlood(_drainAmount);

            if (_drainFinished) { _player.MyStateMachine.ChangeState(_player.MyStateMachine.StateIdle); }
    }

    public void Exit()
    {
            EventManager.DIEvent -= ChangeDI;
            if (_player != null)
        {
            _player.CanTransform = true;
        }
    }
        public void ChangeDI(Vector2 di)
        {

        }

        public void FixedUpdate()
        {
        }
    }
}
