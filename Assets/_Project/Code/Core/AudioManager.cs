using UnityEngine;

namespace _Project.Code.Core
{
    /// <summary>
    /// Audio singleton. Bootstraps itself, so it does not need to be placed in any scene.
    ///
    /// The class already existed, but no scene contained an AudioManager object and nothing ever
    /// called PlayBGMusic(), which is why playtesters reported no music at all. Rather than hand
    /// place it in four scenes at the end of a jam, it now creates itself on game start and
    /// survives every scene load.
    ///
    /// The music track is loaded by path from Resources, so swapping it means dropping a
    /// different file at Assets/Resources/Music/BGMusic.mp3. No inspector work, no recompile.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        /// <summary>Resources path, no extension, of the looping background track.</summary>
        private const string MusicResourcePath = "Music/BGMusic";

        [Range(0f, 1f)] public float musicVolume = 0.5f;
        [Range(0f, 1f)] public float sfxVolume = 1f;

        public static AudioManager Instance { get; private set; }
        public AudioSource sfxAudioSource;
        public AudioSource bgAudioSource;

        /// <summary>
        /// Runs once after the first scene loads. This is what removes the need for an
        /// AudioManager object in any scene.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;

            var host = new GameObject("[AudioManager]");
            host.AddComponent<AudioManager>();
        }

        private void Awake()
        {
            // Destroy the whole object, not just this component. Destroying only the component
            // leaves a stray object behind still holding live AudioSources, which is how you get
            // the same track playing twice slightly out of sync.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureSources();
        }

        private void Start()
        {
            PlayBGMusic();
        }

        /// <summary>
        /// Creates the two AudioSources if they were not assigned in the inspector, so this works
        /// whether it bootstrapped itself or was placed in a scene by hand.
        /// </summary>
        private void EnsureSources()
        {
            if (bgAudioSource == null) bgAudioSource = gameObject.AddComponent<AudioSource>();
            if (sfxAudioSource == null) sfxAudioSource = gameObject.AddComponent<AudioSource>();

            // spatialBlend 0 forces 2D. The imported clips are marked 3D, and a 3D music source
            // attenuates with distance from the listener, which reads as the music cutting out.
            bgAudioSource.spatialBlend = 0f;
            bgAudioSource.playOnAwake = false;
            bgAudioSource.loop = true;
            bgAudioSource.volume = musicVolume;

            sfxAudioSource.spatialBlend = 0f;
            sfxAudioSource.playOnAwake = false;
            sfxAudioSource.loop = false;
            sfxAudioSource.volume = sfxVolume;
        }

        /// <summary>Plays a one shot sound effect. Ignores a null clip.</summary>
        public void PlaySound(AudioClip clip)
        {
            if (clip == null || sfxAudioSource == null) return;
            sfxAudioSource.PlayOneShot(clip, sfxVolume);
        }

        /// <summary>
        /// Starts the looping background track. Safe to call repeatedly: if the track is already
        /// playing it does nothing, so a scene change does not restart the music from the top.
        /// </summary>
        public void PlayBGMusic()
        {
            if (bgAudioSource == null) return;
            if (bgAudioSource.isPlaying) return;

            if (bgAudioSource.clip == null)
            {
                bgAudioSource.clip = Resources.Load<AudioClip>(MusicResourcePath);

                if (bgAudioSource.clip == null)
                {
                    Debug.LogWarning($"[AudioManager] No clip at Resources/{MusicResourcePath}, " +
                                     "so there will be no music.", this);
                    return;
                }
            }

            bgAudioSource.loop = true;
            bgAudioSource.Play();
        }

        /// <summary>Stops the background track.</summary>
        public void StopBGMusic()
        {
            if (bgAudioSource != null) bgAudioSource.Stop();
        }

        /// <summary>Music volume, 0 to 1. Hook a settings slider to this.</summary>
        public void SetMusicVolume(float value)
        {
            musicVolume = Mathf.Clamp01(value);
            if (bgAudioSource != null) bgAudioSource.volume = musicVolume;
        }

        /// <summary>Sound effect volume, 0 to 1. Hook a settings slider to this.</summary>
        public void SetSfxVolume(float value)
        {
            sfxVolume = Mathf.Clamp01(value);
            if (sfxAudioSource != null) sfxAudioSource.volume = sfxVolume;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
