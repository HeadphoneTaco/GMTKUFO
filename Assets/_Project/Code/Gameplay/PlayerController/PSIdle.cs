using UnityEngine;

namespace _Project.Code.Gameplay.PlayerController
{
public class PSIdle : IState
{
    private PlayerController _player;
    
    public PSIdle(PlayerController player)
    {
        _player = player;
    }
    // play idle animation
    // listen for inputs to go into walking and the non moving mist
    public void Enter()
    {
        //enter animation state
        EventManager.DIEvent += ChangeDI;
        Debug.Log("State Entered: Idle");
    }

    public void Execute()
    {
        Vector3 v = _player.RB.linearVelocity;
        v.x = 0;
        _player.RB.linearVelocity = v;
        _player.IncreaseBatTime();
    }

    public void Exit()
    {
        // leave animation state
        EventManager.DIEvent -= ChangeDI;
    }
    public void ChangeDI(Vector2 direction)
    {
        _player.ChangeDI(direction);
        if (direction.x != 0) _player.MyStateMachine.ChangeState(_player.MyStateMachine.StateWalk);
    }
}
}
