using System;
using CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Code.Core
{
    /// <summary>
    /// Central run manager for "Count Down Under". Owns the game-flow state, the score
    /// (blood gathered), the sunrise countdown, and the post-sunrise daylight drain.
    ///
    /// Pattern: singleton + static events. Grab data via GameManager.Instance and its
    /// properties; subscribe to the OnXxx events to react (UI, SFX, VFX) without polling.
    ///
    /// How the rest of the game talks to it (gameplay hooks in here):
    ///   GameManager.Instance.AddBlood(amount)        // a victim gets drained
    ///   GameManager.Instance.SetShadowAmount(0..1)   // sun/shadow zone reports cover
    ///   GameManager.Instance.EndRun()                // player sleeps in a coffin
    ///   GameManager.OnSunrise += ...                 // the sun came up, drain phase begins
    ///
    /// Put ONE GameManager in the first scene that loads. It survives scene loads
    /// (DontDestroyOnLoad), so score/timer persist across MainMenu -> Game -> EndScreen.
    ///
    /// Derives from CoreUtils.Singleton&lt;T&gt;, which supplies the static Instance accessor,
    /// lazy creation, and quit-safety (via AppTracker). We keep an OnEnable override to add
    /// the cross-scene persistence and duplicate-kill the base intentionally leaves out.
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        // ---- Events (static so UI/SFX can subscribe in OnEnable without racing Awake) ----
        /// <summary>Fired whenever the score changes. Payload = new score (whole blood points).</summary>
        public static event Action<int> OnScoreChanged;
        /// <summary>Fired every frame the clock ticks. Payload = seconds remaining until sunrise.</summary>
        public static event Action<float> OnTimerChanged;
        /// <summary>Fired once, the moment the countdown hits zero and daylight drain begins.</summary>
        public static event Action OnSunrise;
        /// <summary>Fired whenever the flow state changes. Payload = the new state.</summary>
        public static event Action<GameState> OnGameStateChanged;
        /// <summary>Fired when a run starts (score reset, clock running).</summary>
        public static event Action OnRunStarted;
        /// <summary>Fired when a run ends. Payload = final banked score.</summary>
        public static event Action<int> OnRunEnded;

        [Header("Sunrise Countdown")]
        [Tooltip("Seconds of night before the sun rises and the daylight drain begins.")]
        [SerializeField] private float _sunriseDuration = 180f;

        [Header("Daylight Drain (points per second, after sunrise)")]
        [Tooltip("Drain rate in full open sun (shadow amount = 0).")]
        [SerializeField] private float _sunDrainPerSecond = 1f;
        [Tooltip("Drain rate in full shadow (shadow amount = 1). GDD target ~1 pt / 10 sec.")]
        [SerializeField] private float _shadowDrainPerSecond = 0.1f;

        [Header("Scene Flow (optional - leave blank to stay in one scene)")]
        [Tooltip("Scene loaded by StartRun(). Blank = don't load, just switch state.")]
        [SerializeField] private string _gameSceneName = "Game";
        [Tooltip("Scene loaded by ReturnToMenu(). Blank = don't load, just switch state.")]
        [SerializeField] private string _mainMenuSceneName = "MainMenu";
        [Tooltip("Scene loaded by EndRun(). Blank = don't load, just switch state (panel-based end screen).")]
        [SerializeField] private string _endSceneName = "EndScreen";

        [Header("Pause")]
        [Tooltip("If true, PauseGame() sets Time.timeScale = 0.")]
        [SerializeField] private bool _pauseFreezesTime = true;

        // ---- Runtime state ----
        private float _score;               // kept as float so fractional drain is smooth
        private float _timeRemaining;       // seconds until sunrise
        private float _shadowAmount;        // 0 = full sun, 1 = full shadow (current player cover)
        private bool _afterSunrise;

        // ---- Read-only accessors for anyone who'd rather poll than subscribe ----
        public GameState CurrentState { get; private set; } = GameState.MainMenu;
        /// <summary>Current score as whole blood points (floored).</summary>
        public int Score => Mathf.FloorToInt(_score);
        /// <summary>Seconds left until sunrise (0 once the sun is up).</summary>
        public float TimeRemaining => _timeRemaining;
        /// <summary>0..1 fraction of night remaining, handy for a countdown bar fill.</summary>
        public float NightProgress => _sunriseDuration <= 0f ? 0f : Mathf.Clamp01(_timeRemaining / _sunriseDuration);
        /// <summary>True once the sun has risen and score is draining.</summary>
        public bool IsAfterSunrise => _afterSunrise;
        /// <summary>Score banked by the last EndRun(). Stable to read on a separate EndScreen scene.</summary>
        public int LastBankedScore { get; private set; }
        /// <summary>Current shadow cover on the player, 0 (sun) .. 1 (shade).</summary>
        public float ShadowAmount => _shadowAmount;

        public override void OnEnable()
        {
            // A duplicate riding in on a scene load: kill it, keep the original. (The base
            // only logs duplicates, so we handle the destroy ourselves.)
            if (Exists && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            base.OnEnable();               // base sets the static Instance
            DontDestroyOnLoad(gameObject); // persist across MainMenu -> Game -> EndScreen
        }

        private void Update()
        {
            if (CurrentState != GameState.Playing) return; // paused/menu/gameover -> no ticking

            if (!_afterSunrise)
            {
                TickCountdown();
            }
            else
            {
                TickDaylightDrain();
            }
        }

        private void TickCountdown()
        {
            _timeRemaining -= Time.deltaTime;
            if (_timeRemaining <= 0f)
            {
                _timeRemaining = 0f;
                _afterSunrise = true;
                OnTimerChanged?.Invoke(0f);
                OnSunrise?.Invoke();
                return;
            }
            OnTimerChanged?.Invoke(_timeRemaining);
        }

        private void TickDaylightDrain()
        {
            // Lerp between sun and shadow rates by how covered the player is.
            float rate = Mathf.Lerp(_sunDrainPerSecond, _shadowDrainPerSecond, _shadowAmount);
            float before = _score;
            _score = Mathf.Max(0f, _score - rate * Time.deltaTime);

            // Only fire the event when the whole-point display actually changes, to avoid spamming UI.
            if (Mathf.FloorToInt(before) != Mathf.FloorToInt(_score))
            {
                OnScoreChanged?.Invoke(Score);
            }
        }

        // ================================================================
        //  Public API — gameplay and UI call these
        // ================================================================

        /// <summary>Start a fresh run: reset score and clock, go to Playing. Loads the game scene if one is set.</summary>
        public void StartRun()
        {
            _score = 0f;
            _timeRemaining = _sunriseDuration;
            _shadowAmount = 0f;
            _afterSunrise = false;

            if (_pauseFreezesTime) Time.timeScale = 1f;

            if (!string.IsNullOrEmpty(_gameSceneName))
                SceneManager.LoadScene(_gameSceneName);

            SetState(GameState.Playing);
            OnRunStarted?.Invoke();
            OnScoreChanged?.Invoke(Score);
            OnTimerChanged?.Invoke(_timeRemaining);
        }

        /// <summary>Add drained blood to the score. Call this when a victim is drained.</summary>
        public void AddBlood(float amount)
        {
            if (amount <= 0f || CurrentState != GameState.Playing) return;
            _score += amount;
            OnScoreChanged?.Invoke(Score);
        }

        /// <summary>Report how shaded the player currently is: 0 = full open sun, 1 = full shadow.</summary>
        public void SetShadowAmount(float amount)
        {
            _shadowAmount = Mathf.Clamp01(amount);
        }

        /// <summary>Binary convenience for zone triggers with no partial shade.</summary>
        public void SetPlayerInShadow(bool inShadow)
        {
            SetShadowAmount(inShadow ? 1f : 0f);
        }

        /// <summary>End the run and bank the current score (player slept in a coffin, or burned out).</summary>
        public void EndRun()
        {
            if (CurrentState == GameState.GameOver) return;
            LastBankedScore = Score;
            if (_pauseFreezesTime) Time.timeScale = 1f; // don't leave time frozen on the results screen
            SetState(GameState.GameOver);
            OnRunEnded?.Invoke(LastBankedScore);

            // Separate-scenes flow: hand off to the EndScreen scene, which reads LastBankedScore.
            // Leave _endSceneName blank to stay put (panel-based results in the current scene).
            if (!string.IsNullOrEmpty(_endSceneName))
                SceneManager.LoadScene(_endSceneName);
        }

        /// <summary>Freeze the run. Safe to call from a pause menu.</summary>
        public void PauseGame()
        {
            if (CurrentState != GameState.Playing) return;
            if (_pauseFreezesTime) Time.timeScale = 0f;
            SetState(GameState.Paused);
        }

        /// <summary>Resume a paused run.</summary>
        public void ResumeGame()
        {
            if (CurrentState != GameState.Paused) return;
            if (_pauseFreezesTime) Time.timeScale = 1f;
            SetState(GameState.Playing);
        }

        /// <summary>Toggle pause/resume, e.g. from a single Escape/Start button.</summary>
        public void TogglePause()
        {
            if (CurrentState == GameState.Playing) PauseGame();
            else if (CurrentState == GameState.Paused) ResumeGame();
        }

        /// <summary>Go back to the main menu. Loads the menu scene if one is set.</summary>
        public void ReturnToMenu()
        {
            if (_pauseFreezesTime) Time.timeScale = 1f;
            SetState(GameState.MainMenu);
            if (!string.IsNullOrEmpty(_mainMenuSceneName))
                SceneManager.LoadScene(_mainMenuSceneName);
        }

        private void SetState(GameState next)
        {
            if (CurrentState == next) return;
            CurrentState = next;
            OnGameStateChanged?.Invoke(next);
        }
    }
}
