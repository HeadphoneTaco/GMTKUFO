using _Project.Code.Core;
using UnityEngine;

namespace _Project.Code.Utilities
{
    /// <summary>
    /// TEMPORARY test scaffolding. Lets a UI button drive the run flow before the player and Coffin
    /// exist, so the scene flow can be tested end to end. Wire a button's OnClick to SleepNow() to
    /// bank the run and jump to the EndScreen. Delete this once walking into a real Coffin works.
    /// </summary>
    public class RunTestControls : MonoBehaviour
    {
        [Tooltip("Start a run automatically when this scene is played directly. Without it the " +
                 "state stays MainMenu, so the clock does not tick and AddBlood rejects drains, " +
                 "which makes a working HUD look broken.")]
        [SerializeField] private bool _autoStartRunOnPlay = true;

        private void Start()
        {
            if (!_autoStartRunOnPlay) return;

            // Deliberately no Exists check. GameManager only lives in MainMenu, so when the game
            // scene is played directly Exists stays false forever and this would never fire.
            // Touching Instance is what creates it.
            GameManager gm = GameManager.Instance;
            if (gm.CurrentState == GameState.Playing) return;

            gm.StartRunInCurrentScene();
        }

        /// <summary>Ends the run and banks the score, same as sleeping in a coffin.</summary>
        public void SleepNow()
        {
            GameManager.Instance.EndRun(RunOutcome.ReachedCoffin);
        }

        /// <summary>Adds test blood to the score so the EndScreen shows a non-zero number.</summary>
        public void AddTestBlood()
        {
            if (GameManager.Exists) GameManager.Instance.AddBlood(25f);
        }
    }
}
