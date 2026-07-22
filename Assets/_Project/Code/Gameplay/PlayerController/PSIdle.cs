using UnityEngine;

public class PSIdle : IState
{
    private PlayerController _player;
    
    public PSIdle(PlayerController player)
    {
        _player = player;
    }
    // play idle animation
    // listen for inputs to go into walking but maybe not mist 
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
