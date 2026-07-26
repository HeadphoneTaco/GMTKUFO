using _Project.Code.Core;
using _Project.Code.Gameplay.PlayerController;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Code.UI
{
    /// <summary>
    /// Drives the in-run HUD: bat meter on the left, sunrise clock beside it, blood bar and total
    /// top right. Put this on the GameScreen object inside the [UI]Game prefab.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Bat Meter")]
        [Tooltip("Fills 0 to 1 with the bat time left. Keep its Direction Left To Right, since " +
                 "Bottom To Top makes the handle area spill outside the frame art.")]
        [SerializeField] private Slider _batSlider;

        [Header("Sunrise Clock")]
        [Tooltip("Slow hand, one full turn over the whole night by default. Its pivot must sit at " +
                 "the end that stays put, normally bottom centre, or it will orbit instead of turn.")]
        [SerializeField] private RectTransform _hourHand;

        [Tooltip("Turns this many times over one night. 1 means it points at sunrise the whole run.")]
        [SerializeField] private float _hourHandTurns = 1f;

        [Tooltip("Optional fast hand. Leave empty for a single hand clock.")]
        [SerializeField] private RectTransform _minuteHand;

        [Tooltip("Turns this many times over one night. At a 360 second night, 6 gives one turn " +
                 "per minute, which is the most readable rate for a short run.")]
        [SerializeField] private float _minuteHandTurns = 6f;

        [Tooltip("Angle both hands sit at when the night begins. 0 points them straight up if the " +
                 "art is drawn pointing up.")]
        [SerializeField] private float _handStartAngle;

        [Tooltip("On: hands sweep clockwise like a real clock. Off: counter clockwise.")]
        [SerializeField] private bool _handsRunClockwise = true;

        [Header("Blood")]
        [Tooltip("The top right bar. Fills from 0 to Blood Bar Max, then stays full.")]
        [SerializeField] private Slider _bloodSlider;

        [Tooltip("Blood total that fills the bar completely. The score itself is uncapped and can " +
                 "pass this, at which point the bar simply stays full and the number keeps rising.")]
        [SerializeField] private float _bloodBarMax = 1000f;

        [Tooltip("Running blood total as text. Uncapped, which is why it is a number and not just " +
                 "the bar.")]
        [SerializeField] private TMP_Text _bloodText;

        [Tooltip("Formatting for the blood total. {0} is the number.")]
        [SerializeField] private string _bloodFormat = "{0}";

        [Header("Player")]
        [Tooltip("Leave empty to find the player by type at Awake.")]
        [SerializeField] private PlayerController _player;

        private GameManager _gm;

        // Touching Instance is what forces the lazy creation. Testing Exists instead returns false
        // forever when the game scene is played directly, because GameManager only lives in
        // MainMenu, and nothing ever brings one into being. That silently disables the whole HUD.
        private GameManager GM => _gm != null ? _gm : (_gm = GameManager.Instance);

        private void Awake()
        {
            if (_player == null) _player = FindAnyObjectByType<PlayerController>();
        }

        private void OnEnable()
        {
            // Static events, so unsubscribing matters. A HUD left subscribed after a scene load
            // keeps a dead object alive and throws once its fields are destroyed.
            GameManager.OnScoreChanged += HandleScoreChanged;
            GameManager.OnRunStarted += HandleRunStarted;
        }

        private void Start()
        {
            // In Start rather than OnEnable so GameManager's own OnEnable has already run when it
            // is present in the scene, instead of racing it.
            RefreshBlood();
        }

        private void OnDisable()
        {
            // Deliberately no Instance access here. On application quit that can resurrect a
            // destroyed singleton.
            GameManager.OnScoreChanged -= HandleScoreChanged;
            GameManager.OnRunStarted -= HandleRunStarted;
        }

        private void Update()
        {
            // Bat time and the clock have no change events, so they are polled. Both are cheap.
            if (_batSlider != null && _player != null)
                _batSlider.value = _player.BatTimeNormalized;

            UpdateClockHands();
        }

        private void UpdateClockHands()
        {
            if (_hourHand == null && _minuteHand == null) return;

            // NightProgress is 1 at dusk and 0 at sunrise, so this is the fraction of night used.
            float elapsed = 1f - GM.NightProgress;

            if (_hourHand != null) SetHandAngle(_hourHand, elapsed, _hourHandTurns);
            if (_minuteHand != null) SetHandAngle(_minuteHand, elapsed, _minuteHandTurns);
        }

        private void SetHandAngle(RectTransform hand, float elapsed, float turns)
        {
            float sweep = elapsed * 360f * turns;

            // Positive Z rotates counter clockwise in Unity, so a clockwise sweep is negative.
            if (_handsRunClockwise) sweep = -sweep;

            hand.localRotation = Quaternion.Euler(0f, 0f, _handStartAngle + sweep);
        }

        private void HandleRunStarted() => RefreshBlood();

        private void HandleScoreChanged(int score)
        {
            if (_bloodText != null) _bloodText.text = string.Format(_bloodFormat, score);

            if (_bloodSlider != null)
                _bloodSlider.value = _bloodBarMax <= 0f
                    ? 0f
                    : Mathf.Clamp01(score / _bloodBarMax);
        }

        // The score event only fires on change, so the readout would sit blank from scene load
        // until the first bite without an explicit pull.
        private void RefreshBlood() => HandleScoreChanged(GM.Score);
    }
}
