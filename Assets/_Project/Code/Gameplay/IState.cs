namespace _Project.Code.Gameplay
{
    public interface IState
    {
        public void Enter();
        public void Exit();
        public void Execute();
    }
}
