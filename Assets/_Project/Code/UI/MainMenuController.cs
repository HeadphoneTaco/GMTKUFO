using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Title scene controller. Wire these methods to the menu buttons' OnClick in the inspector.
///
/// Place the GameManager in THIS scene (it is DontDestroyOnLoad, so it rides along into the
/// Game and EndScreen scenes). Play starts a run via the GameManager, which loads the Game scene.
/// Options is a menu-side scene, so it is loaded here directly rather than through the run flow.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Tooltip("Options scene to load from the menu. Must be in Build Settings.")]
    [SerializeField] private string _optionsSceneName = "Options";

    /// <summary>Play button: reset and start a run (GameManager loads the Game scene).</summary>
    public void PlayGame()
    {
        GameManager.Instance.StartRun();
    }

    /// <summary>Options button: open the (placeholder) options scene.</summary>
    public void OpenOptions()
    {
        if (!string.IsNullOrEmpty(_optionsSceneName))
            SceneManager.LoadScene(_optionsSceneName);
    }

    /// <summary>Quit button: exit the game (stops play mode in the editor).</summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
