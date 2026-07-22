using UnityEngine;

public class PlayerStateMachine
{

    private PlayerController _player;
    private IState _currentState;

    public PlayerStateMachine(PlayerController player)
    {
        _player = player;
    }

    public void Initialize(IState state)
    {
        _currentState = state;
        _currentState.Enter();
    }
    public void Disable()
    {
        _currentState.Exit();
        _currentState = null;
    }
    public void ChangeState(IState state) 
    {
        _currentState.Exit();
        _currentState = state;
        _currentState.Enter();
    }
    public void Execute()
    {
        _currentState?.Execute();
    }
}
