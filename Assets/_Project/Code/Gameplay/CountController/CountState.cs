namespace _Project.Code.Gameplay.CountController
{
    public abstract class CountState
    {
        protected CountController _countController;
        public CountState(CountController controller)=>_countController = controller;
        public virtual void Enter(){}

        public virtual void Exit()
        {
        }

        public virtual void Update()
        {
        }

        public virtual void FixedUpdate()
        {
        }
    }
}