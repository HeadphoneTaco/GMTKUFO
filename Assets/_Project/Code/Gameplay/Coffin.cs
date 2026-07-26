using _Project.Code.Core;
using _Project.Code.Gameplay.PlayerController;
using UnityEngine;


    /// <summary>
    /// Dracula's coffin. Walking into the one at the end of the level banks the run and sends the
    /// player to the coffin end screen.
    /// </summary>
    public class Coffin : MonoBehaviour
    {
        [Tooltip("On for the coffin at the end of the level. Off for the one the player starts in, " +
                 "otherwise the run ends on the first frame because the player is already inside it.")]
        [SerializeField] private bool _endsRun = true;

        [Tooltip("Extra safety for the end coffin. The player must leave the trigger once before it " +
                 "can fire. Harmless to leave on, and it covers a level where start and end overlap.")]
        [SerializeField] private bool _requireExitFirst;

        private bool _armed = true;
        private bool _fired;

        private void Awake()
        {
            if (_requireExitFirst) _armed = false;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<PlayerController>() == null) return;
            _armed = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_endsRun || _fired || !_armed) return;
            if (other.GetComponent<PlayerController>() == null) return;

            // Guard against a second trigger in the same frame as the scene load starts.
            _fired = true;
            GameManager.Instance.EndRun(RunOutcome.ReachedCoffin);
        }
    }