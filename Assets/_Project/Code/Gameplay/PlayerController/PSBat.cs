using UnityEngine;

namespace _Project.Code.Gameplay.PlayerController
{
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
        // transform into a bat
        _player.MyAnimator.ShowBat();
        Debug.Log("State Entered: Bat");
        _player.RB.useGravity = false;
    }

    public void Execute()
    {
        if (_player.ReduceBatTime()) _player.MyStateMachine.ChangeState(_player.MyStateMachine.StateMist);

        // FlySpeed is an acceleration, so this line compounds every frame. With gravity off and
        // no damping there was nothing to stop it, and the player hit roughly 60 units per second
        // by the end of the bat meter. Clamping the result gives flight a terminal velocity.
        //
        // This also keeps collisions honest. The rigidbody uses discrete detection, which only
        // tests for overlap at the end of each physics step, so anything moving further than the
        // thickness of a wall in one step passes straight through it. At a 0.02 timestep, 60 u/s
        // is 1.2 units of travel per step, enough to tunnel through most bounding geometry.
        Vector3 velocity = _player.RB.linearVelocity;
        velocity += _player.FlySpeed * Time.deltaTime * (Vector3)_player.DirectionalInput;
        _player.RB.linearVelocity = Vector3.ClampMagnitude(velocity, _player.MaxFlySpeed);
    }

    public void Exit()
    {
        if (_player != null)
        {
            // transform out of a bat if player isnt null
            _player.MyAnimator.ShowHumanoid();
            _player.RB.useGravity = true;
        }
    }
    public void ChangeDI(Vector2 direction)
    {
        _player.ChangeDI(direction);
    }

        public void FixedUpdate()
        {
        }
    }
}
