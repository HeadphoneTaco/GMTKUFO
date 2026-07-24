using UnityEngine;

    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance;
        public AudioSource sfxAudioSource;
        public AudioSource bgAudioSource;

        //null check and functionality to play a one shot audio clip when authorized.

        private void Awake()
        {
            if (Instance != null && Instance != this) Destroy(this);
            Instance = this;
        }

        public void PlaySound(AudioClip clip)
        {
            if (clip == null) return;
            sfxAudioSource.PlayOneShot(clip);
        }

        public void PlayBGMusic()
        {
            bgAudioSource.loop = true;
            bgAudioSource.Play();
        }
    }