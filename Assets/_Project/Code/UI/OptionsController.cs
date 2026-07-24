using UnityEngine;
using UnityEngine.SceneManagement;


    /// <summary>
    /// Placeholder options scene. For now it is just a Back button that returns to the menu.
    /// Fill in volume/fullscreen/etc. later; keep the Back wiring when you do.
    /// </summary>
    public class OptionsController : MonoBehaviour
    {
        [Tooltip("Scene to return to (the title). Must be in Build Settings.")]
        [SerializeField] private string _returnSceneName = "MainMenu";

        /// <summary>Back button: return to the title scene.</summary>
        public void Back()
        {
            if (!string.IsNullOrEmpty(_returnSceneName))
                SceneManager.LoadScene(_returnSceneName);
        }
    }
