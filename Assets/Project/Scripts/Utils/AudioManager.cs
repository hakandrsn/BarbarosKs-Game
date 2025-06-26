using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace BarbarosKs.Utils
{
    public class AudioManager : MonoBehaviour
    {
        [System.Serializable]
        public class Sound
        {
            public string name;
            public AudioClip clip;
            [Range(0f, 1f)]
            public float volume = 1f;
            [Range(0.1f, 3f)]
            public float pitch = 1f;
            public bool loop = false;

            [HideInInspector]
            public AudioSource source;
        }

        [Header("Ses Ayarları")]
        [SerializeField] private Sound[] sounds;
        [SerializeField] private AudioMixerGroup musicMixerGroup;
        [SerializeField] private AudioMixerGroup sfxMixerGroup;

        [Header("Müzik Ayarları")]
        [SerializeField] private string[] ambientMusicTracks;
        [SerializeField] private string[] combatMusicTracks;
        [SerializeField] private string[] menuMusicTracks;
        [SerializeField] private float musicFadeDuration = 2f;

        // Singleton örneği
        public static AudioManager Instance { get; private set; }

        private string currentMusicTrack;
        private bool inCombat = false;
        private Coroutine musicFadeCoroutine;

        private void Awake()
        {
            // Singleton kontrolü
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Tüm ses objelerini oluştur
            foreach (Sound s in sounds)
            {
                s.source = gameObject.AddComponent<AudioSource>();
                s.source.clip = s.clip;
                s.source.volume = s.volume;
                s.source.pitch = s.pitch;
                s.source.loop = s.loop;

                // Müzik veya efekt mixer grubuna ata
                if (s.name.StartsWith("Music"))
                {
                    s.source.outputAudioMixerGroup = musicMixerGroup;
                }
                else
                {
                    s.source.outputAudioMixerGroup = sfxMixerGroup;
                }
            }
        }

        private void Start()
        {
            // Başlangıçta ortam müziği çal
            PlayRandomMusic(ambientMusicTracks);
        }

        public void Play(string soundName)
        {
            Sound s = GetSound(soundName);
            if (s == null)
            {
                Debug.LogWarning("Ses bulunamadı: " + soundName);
                return;
            }

            s.source.Play();
        }

        public void Stop(string soundName)
        {
            Sound s = GetSound(soundName);
            if (s == null)
            {
                Debug.LogWarning("Ses bulunamadı: " + soundName);
                return;
            }

            s.source.Stop();
        }

        public void SetCombatState(bool combat)
        {
            if (inCombat == combat) return;

            inCombat = combat;

            if (inCombat)
            {
                // Savaş müziğine geç
                FadeToMusic(GetRandomTrack(combatMusicTracks));
            }
            else
            {
                // Ortam müziğine geri dön
                FadeToMusic(GetRandomTrack(ambientMusicTracks));
            }
        }

        public void PlayMenuMusic()
        {
            PlayRandomMusic(menuMusicTracks);
        }

        private void PlayRandomMusic(string[] tracks)
        {
            if (tracks == null || tracks.Length == 0) return;

            string trackName = GetRandomTrack(tracks);
            FadeToMusic(trackName);
        }

        private string GetRandomTrack(string[] tracks)
        {
            if (tracks == null || tracks.Length == 0) return null;

            return tracks[Random.Range(0, tracks.Length)];
        }

        private void FadeToMusic(string newTrackName)
        {
            if (string.IsNullOrEmpty(newTrackName) || newTrackName == currentMusicTrack) return;

            // Müzik geçişini başlat
            if (musicFadeCoroutine != null)
            {
                StopCoroutine(musicFadeCoroutine);
            }

            musicFadeCoroutine = StartCoroutine(FadeMusicTrack(currentMusicTrack, newTrackName));
            currentMusicTrack = newTrackName;
        }

        private IEnumerator FadeMusicTrack(string oldTrack, string newTrack)
        {
            Sound oldSound = null;
            Sound newSound = GetSound(newTrack);

            if (!string.IsNullOrEmpty(oldTrack))
            {
                oldSound = GetSound(oldTrack);
            }

            // Yeni parçayı sıfır ses seviyesiyle başlat
            if (newSound != null)
            {
                newSound.source.volume = 0;
                newSound.source.Play();
            }

            float timer = 0;
            float oldVolume = oldSound != null ? oldSound.volume : 0;
            float newVolume = newSound != null ? newSound.volume : 0;

            while (timer < musicFadeDuration)
            {
                timer += Time.deltaTime;
                float t = timer / musicFadeDuration;

                // Eski parçayı kıs
                if (oldSound != null)
                {
                    oldSound.source.volume = Mathf.Lerp(oldVolume, 0, t);
                }

                // Yeni parçayı yükselt
                if (newSound != null)
                {
                    newSound.source.volume = Mathf.Lerp(0, newVolume, t);
                }

                yield return null;
            }

            // Eski parçayı durdur
            if (oldSound != null)
            {
                oldSound.source.Stop();
                oldSound.source.volume = oldVolume; // Orijinal ses seviyesini geri yükle
            }

            musicFadeCoroutine = null;
        }

        private Sound GetSound(string soundName)
        {
            return System.Array.Find(sounds, sound => sound.name == soundName);
        }

        // Ses ayarlarını değiştirme
        public void SetMusicVolume(float volume)
        {
            if (musicMixerGroup != null)
            {
                musicMixerGroup.audioMixer.SetFloat("MusicVolume", Mathf.Log10(volume) * 20);
            }
        }

        public void SetSFXVolume(float volume)
        {
            if (sfxMixerGroup != null)
            {
                sfxMixerGroup.audioMixer.SetFloat("SFXVolume", Mathf.Log10(volume) * 20);
            }
        }
    }
}
