using UnityEngine;

public class PSWalk : IState
{
    private PlayerController _player;
    
    public PSWalk(PlayerController player)
    {
        _player = player;
    }
    // take a DI and walk left or right
    // enter mist state on space press 
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
