using UnityEngine;

public class PSEating : IState
{
    private PlayerController _player;
    
    public PSEating(PlayerController player)
    {
        _player = player;
    }
    // play the eating animation
    // exit into Idle
    // 
    public void Enter()
    {
        
    }

    public void Execute()
    {

    }

    public void Exit()
    {

    }
}
