using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Code.UI
{
    public class SettingsController : MonoBehaviour
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
}
