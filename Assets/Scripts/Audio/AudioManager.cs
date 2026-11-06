using System.Collections;
using UnityEngine;

namespace Skybound
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private AudioSource musicA;
        private AudioSource musicB;
        private AudioSource activeMusic;
        private AudioSource inactiveMusic;
        private AudioSource[] sfxSources;
        private int sfxIndex;
        private Coroutine musicRoutine;
        private MusicTrack currentTrack = MusicTrack.None;
        private float masterVolume = 1f;
        private float musicVolume = 0.7f;
        private float sfxVolume = 0.9f;
        private float voiceVolume = 0.8f;
        private bool pausedDuck;
        private bool musicMuted;

        public static AudioManager Create(Transform parent)
        {
            GameObject go = new GameObject("AudioManager");
            if (parent != null) go.transform.SetParent(parent, false);
            return go.AddComponent<AudioManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            BuildSources();
            ApplySettings();
        }

        private void BuildSources()
        {
            musicA = gameObject.AddComponent<AudioSource>();
            musicA.playOnAwake = false;
            musicA.loop = true;
            musicA.spatialBlend = 0f;
            musicB = gameObject.AddComponent<AudioSource>();
            musicB.playOnAwake = false;
            musicB.loop = true;
            musicB.spatialBlend = 0f;
            activeMusic = musicA;
            inactiveMusic = musicB;

            sfxSources = new AudioSource[8];
            for (int i = 0; i < sfxSources.Length; i++)
            {
                AudioSource src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                src.loop = false;
                sfxSources[i] = src;
            }
        }

        public void ApplySettings()
        {
            if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
            {
                SettingsData s = SaveManager.Instance.Data.Settings;
                musicVolume = s.MusicVolume;
                sfxVolume = s.SfxVolume;
                voiceVolume = s.VoiceVolume;
            }
            AudioListener.volume = masterVolume;
            if (activeMusic != null && activeMusic.isPlaying)
            {
                activeMusic.volume = musicVolume * (pausedDuck ? 0.28f : 1f) * (musicMuted ? 0f : 1f);
            }
        }

        public void SetMasterVolume(float value)
        {
            masterVolume = Mathf.Clamp01(value);
            AudioListener.volume = masterVolume;
        }

        public void SetMusicVolume(float value)
        {
            musicVolume = Mathf.Clamp01(value);
            if (activeMusic != null)
                activeMusic.volume = musicVolume * (pausedDuck ? 0.28f : 1f);
        }

        public void SetSfxVolume(float value)
        {
            sfxVolume = Mathf.Clamp01(value);
        }

        public void SetVoiceVolume(float value)
        {
            voiceVolume = Mathf.Clamp01(value);
        }

        public void SetPausedDuck(bool paused)
        {
            pausedDuck = paused;
            if (activeMusic != null)
                activeMusic.volume = paused ? musicVolume * 0.28f : musicVolume;
        }

        public void PlayMusic(MusicTrack track, float fadeSeconds = 0.7f)
        {
            if (track == MusicTrack.None)
            {
                StopMusic(fadeSeconds);
                return;
            }
            if (currentTrack == track && activeMusic != null && activeMusic.isPlaying) return;
            currentTrack = track;
            AudioClip clip = ProceduralAudio.GetMusic(track);
            if (clip == null) return;
            if (musicRoutine != null) StopCoroutine(musicRoutine);
            musicRoutine = StartCoroutine(CrossfadeMusic(clip, fadeSeconds));
        }

        public void StopMusic(float fadeSeconds = 0.5f)
        {
            currentTrack = MusicTrack.None;
            if (musicRoutine != null) StopCoroutine(musicRoutine);
            musicRoutine = StartCoroutine(FadeOutMusic(fadeSeconds));
        }

        private IEnumerator CrossfadeMusic(AudioClip clip, float fadeSeconds)
        {
            AudioSource old = activeMusic;
            AudioSource next = inactiveMusic;
            next.clip = clip;
            next.volume = 0f;
            next.time = 0f;
            next.Play();
            float t = 0f;
            float duration = Mathf.Max(0.05f, fadeSeconds);
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float f = Mathf.Clamp01(t / duration);
                float smooth = Mathf.SmoothStep(0f, 1f, f);
                next.volume = musicVolume * smooth;
                if (old != null) old.volume = musicVolume * (1f - smooth);
                yield return null;
            }
            next.volume = musicVolume;
            if (old != null)
            {
                old.Stop();
                old.volume = 0f;
            }
            activeMusic = next;
            inactiveMusic = old;
            musicRoutine = null;
        }

        private IEnumerator FadeOutMusic(float duration)
        {
            if (activeMusic == null) yield break;
            float start = activeMusic.volume;
            float t = 0f;
            duration = Mathf.Max(0.05f, duration);
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                activeMusic.volume = Mathf.Lerp(start, 0f, t / duration);
                yield return null;
            }
            activeMusic.Stop();
            activeMusic.volume = 0f;
        }

        public void PlaySfx(string id, float volumeScale = 1f, float pitch = 1f)
        {
            AudioClip clip = ProceduralAudio.GetSfx(id);
            if (clip == null) return;
            AudioSource source = sfxSources[sfxIndex++ % sfxSources.Length];
            source.pitch = pitch;
            source.PlayOneShot(clip, Mathf.Clamp01(sfxVolume * volumeScale));
        }

        public void PlayVoice(string id, float volumeScale = 1f, float pitch = 1f)
        {
            AudioClip clip = ProceduralAudio.GetSfx(id);
            if (clip == null) return;
            AudioSource source = sfxSources[sfxIndex++ % sfxSources.Length];
            source.pitch = pitch;
            source.PlayOneShot(clip, Mathf.Clamp01(voiceVolume * volumeScale));
        }

        public void PlayButton()
        {
            PlaySfx("button", 0.7f, 1f);
        }

        public void PlayCoin(int combo, bool gold)
        {
            float pitch = 1f + Mathf.Min(0.35f, combo * 0.025f);
            PlaySfx(gold ? "gold" : "coin", 0.55f, pitch);
        }

        public void PlayJump() { PlaySfx("jump", 0.6f); }
        public void PlayLand() { PlaySfx("land", 0.5f); }
        public void PlaySlide() { PlaySfx("slide", 0.5f); }
        public void PlayHit() { PlaySfx("hit", 0.8f); Haptics.Heavy(); }
        public void PlayPowerUp() { PlaySfx("powerup", 0.7f); Haptics.Medium(); }
        public void PlayShield() { PlaySfx("shield", 0.7f); Haptics.Medium(); }
        public void PlayAchievement() { PlaySfx("achievement", 0.8f); Haptics.Light(); }
        public void PlayLevelComplete() { PlaySfx("level", 0.9f); }
        public void PlayWarning() { PlaySfx("warning", 0.55f); Haptics.Medium(); }
        public void PlayPurchase() { PlaySfx("purchase", 0.8f); }
        public void PlayRevive() { PlaySfx("revive", 0.85f); }
        public void PlayThunder() { PlaySfx("thunder", 0.6f); }
        public void PlayLaser() { PlaySfx("laser", 0.45f); }
        public void PlayStar() { PlaySfx("star", 0.7f); }
        public void PlaySwoosh() { PlaySfx("swoosh", 0.5f); }
    }

    /// <summary>Platform-safe haptics with three strengths.</summary>
    public static class Haptics
    {
        public static void Light()
        {
            if (!Enabled) return;
            Vibrate(12);
        }

        public static void Medium()
        {
            if (!Enabled) return;
            Vibrate(32);
        }

        public static void Heavy()
        {
            if (!Enabled) return;
            Vibrate(80);
        }

        private static bool Enabled
        {
            get
            {
                if (SaveManager.Instance == null || SaveManager.Instance.Data == null) return false;
                return SaveManager.Instance.Data.Settings.Vibration;
            }
        }

        private static void Vibrate(long milliseconds)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                try
                {
                    using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                    using (AndroidJavaObject vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                    {
                        if (vibrator != null) vibrator.Call("vibrate", milliseconds);
                    }
                }
                catch
                {
                    Handheld.Vibrate();
                }
            }
            else if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                Handheld.Vibrate();
            }
        }
    }
}
