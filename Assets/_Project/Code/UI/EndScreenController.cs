using UnityEngine;
using TMPro;

/// <summary>
/// Results scene. Reads the score the GameManager banked in EndRun() (LastBankedScore stays
/// stable after the Game scene unloads, since the GameManager persists across scenes).
///
/// Hookup:
///   - Assign the score text.
///   - Play Again button -> PlayAgain(); Main Menu button -> MainMenu().
/// </summary>
public class EndScreenController : MonoBehaviour
{
    [Tooltip("Text that shows the banked score.")]
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private string _scoreFormat = "Blood banked: {0}";

    private void Start()
    {
        int score = GameManager.Exists ? GameManager.Instance.LastBankedScore : 0;
        if (_scoreText != null)
            _scoreText.text = string.Format(_scoreFormat, score);
        else
            Debug.LogWarning("[EndScreenController] Score Text is not assigned, so the score can't show.", this);
    }

    /// <summary>Play Again button: start a fresh run (GameManager loads the Game scene).</summary>
    public void PlayAgain()
    {
        GameManager.Instance.StartRun();
    }

    /// <summary>Main Menu button: back to the title.</summary>
    public void MainMenu()
    {
        GameManager.Instance.ReturnToMenu();
    }
}
