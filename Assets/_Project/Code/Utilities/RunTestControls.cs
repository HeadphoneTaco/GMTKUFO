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
        /// <summary>Ends the run and banks the score, same as sleeping in a coffin.</summary>
        public void SleepNow()
        {
            if (GameManager.Exists) GameManager.Instance.EndRun();
        }

        /// <summary>Adds test blood to the score so the EndScreen shows a non-zero number.</summary>
        public void AddTestBlood()
        {
            if (GameManager.Exists) GameManager.Instance.AddBlood(25f);
        }
    }
}
