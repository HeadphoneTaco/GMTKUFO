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
                // go into eating animation
                _player.RB.linearVelocity = Vector3.zero;
                _player.transform.position = new Vector3(_victim.transform.position.x, _victim.transform.position.y, _player.transform.position.z);
            }
    }

    public void Execute()
    {
        _player.IncreaseBatTime();
            (_drainFinished,_drainAmount) = _victim.DrainBlood(_player.BloodDrainSpeed * Time.deltaTime);
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
