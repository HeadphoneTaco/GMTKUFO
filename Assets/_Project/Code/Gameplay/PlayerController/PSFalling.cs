namespace _Project.Code.Gameplay.PlayerController
{
    public class PSFalling : IState
    {
        private PlayerController _player;
    
        public PSFalling(PlayerController player)
        {
            _player = player;
        }
        // if the player presses space go into mist form
        // if the player hits the ground they should either go into walk or idle depending on if their di is 0 or not
        // if the player hits a victim, suck their blood
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
}
