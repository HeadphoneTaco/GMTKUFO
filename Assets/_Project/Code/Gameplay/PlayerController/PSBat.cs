using UnityEngine;

public class PSBat : IState
{
    private PlayerController _player;
    
    public PSBat(PlayerController player)
    {
        _player = player;
    }
    // the player is a bat and can fly in 8 directions
    // drain the bat meter, if it is empty: change back into mist
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
