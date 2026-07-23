namespace _Project.Code.Gameplay.PlayerController
{
    public class PSMist : IState
    {
        private PlayerController _player;
    
        public PSMist(PlayerController player)
        {
            _player = player;
        }
        // the inbetween dashing and transforming state
        // give the player a bunch of speed in the direction they were going before (or not if from idle state)
        // do a boxcast to check for victims and if there are go into blood sucking state
        // if the player is still holding space by the end, put the player into bat state
        // if the player stops holding space put them back into falling state
        // if the player alternatively enters from the bat state the player should be put into falling (this may just happen anyway because they are not still holding space)
        // there may need to be a timer to prevent the player from spamming this ability
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
