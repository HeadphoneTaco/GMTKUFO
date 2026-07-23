using _Project.Code.Core;
using UnityEngine;

namespace _Project.Code.UI
{
    /// <summary>
    /// Pause overlay, lives in the Game scene. The pause menu is a panel (not a scene) so the run
    /// stays loaded underneath. Shows/hides itself off the GameManager's state, so it stays in sync
    /// no matter what triggers the pause.
    ///
    /// Hookup:
    ///   - Bind your pause input action (or a button) to TogglePause().
    ///   - Resume button -> Resume(); Main Menu button -> ReturnToMenu().
    ///   - Assign the pause panel GameObject below.
    /// (Options from pause is left out for now since Options is a separate scene; add it as an
    ///  overlay panel later if the run should survive opening options.)
    /// </summary>
    public class PauseController : MonoBehaviour
    {
        [Tooltip("The pause menu panel to show while paused.")]
        [SerializeField] private GameObject _pausePanel;
        [Tooltip("Optional options sub-panel, opened from the pause menu. Kept in-scene so the run stays paused underneath.")]
        [SerializeField] private GameObject _optionsPanel;

        private void Awake()
        {
            if (_pausePanel != null) _pausePanel.SetActive(false);
            if (_optionsPanel != null) _optionsPanel.SetActive(false);
        }

        private void OnEnable()
        {
            GameManager.OnGameStateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            GameManager.OnGameStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(GameState state)
        {
            bool paused = state == GameState.Paused;
            // Options is only ever opened manually from the pause menu, so close it on any state change.
            if (_optionsPanel != null) _optionsPanel.SetActive(false);
            if (_pausePanel != null) _pausePanel.SetActive(paused);
        }

        /// <summary>Pause input / button: flip between paused and playing.</summary>
        public void TogglePause()
        {
            if (GameManager.Exists) GameManager.Instance.TogglePause();
        }

        /// <summary>Resume button.</summary>
        public void Resume()
        {
            if (GameManager.Exists) GameManager.Instance.ResumeGame();
        }

        /// <summary>Options button on the pause menu: swap the pause panel for the options sub-panel.</summary>
        public void OpenOptions()
        {
            if (_pausePanel != null) _pausePanel.SetActive(false);
            if (_optionsPanel != null) _optionsPanel.SetActive(true);
        }

        /// <summary>Back button on the options sub-panel: swap back to the pause menu.</summary>
        public void BackToPause()
        {
            if (_optionsPanel != null) _optionsPanel.SetActive(false);
            if (_pausePanel != null) _pausePanel.SetActive(true);
        }

        /// <summary>Main Menu button: abandon the run and return to the title.</summary>
        public void ReturnToMenu()
        {
            if (GameManager.Exists) GameManager.Instance.ReturnToMenu();
        }
    }
}
