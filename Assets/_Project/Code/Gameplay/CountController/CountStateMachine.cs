using System.Collections.Generic;
using UnityEngine;

namespace _Project.Code.Gameplay.CountController
{
    public enum CountStateValue {EmptyState, IdleState, WalkState, FallState, EatState }
    public class CountStateMachine
    {
        private CountController _countController;
        private CountState _currentState;
        private Dictionary<CountStateValue, CountState> _states = new Dictionary<CountStateValue, CountState>();

        public CountStateMachine(CountController controller)
        {
            _countController = controller;
            _states.Add(CountStateValue.IdleState, new CountIdleState(controller));
            _states.Add(CountStateValue.WalkState, new CountWalkState(controller));
            TransitionToState(CountStateValue.IdleState);
        }

        public void TransitionToState(CountStateValue stateValue)
        {
            CountState oldState = _currentState;
            if (_states[stateValue] == oldState) return;
            oldState?.Exit();
            _currentState = _states[stateValue];
            _currentState.Enter();
        }

        private void HandleStateSwap(CountStateValue stateValue)
        {
            _currentState = _states[stateValue];
            if(_currentState == null)
                Debug.Log("You forgot to set up a state in the dictionary, dummy");
        }

        public void Enable()
        {
            _currentState = _states[CountStateValue.IdleState] ?? null;
            if (_currentState == null)
                Debug.Log("We forgot to actually put states");
        }

        public void Disable()
        {
            
        }

        public void Update()
        {
            _currentState.Update();
        }

        public void FixedUpdate()
        {
            _currentState.FixedUpdate();
        }
        
    }
}