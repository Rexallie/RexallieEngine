using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public enum UISoundType
    {
        Select,
        Confirm,
        Cancel
    }

    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource voiceSource;

    [Header("Audio Database")]
    public List<AudioData> musicTracks = new List<AudioData>();
    public List<AudioData> soundEffects = new List<AudioData>();
    public List<AudioData> voiceClips = new List<AudioData>();

    [Header("UI Button Sounds")]
    public AudioClip uiSelectClip;
    public AudioClip uiConfirmClip;
    public AudioClip uiCancelClip;
    public string uiSelectResourcePath = "Sound/select";
    public string uiConfirmResourcePath = "Sound/confirm";
    public string uiCancelResourcePath = "Sound/cancel";

    [Header("Settings")]
    public float musicVolume = 0.7f;
    public float sfxVolume = 0.45f;
    public float voiceVolume = 1f;
    [Range(0f, 1f)] public float sfxOneShotScale = 0.5f;
    [Range(0f, 1f)] public float uiButtonVolume = 0.25f;

    // Add this near the top of the class
    private string currentMusicTrack;
    private readonly Dictionary<string, AudioClip> cachedResourceSfx = new Dictionary<string, AudioClip>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        musicSource.volume = musicVolume;
        sfxSource.volume = sfxVolume;
        voiceSource.volume = voiceVolume;
    }

    // ==================== MUSIC ====================

    public void PlayMusic(string trackName, float fadeInDuration = 0f)
    {
        AudioData track = musicTracks.Find(t => t.clipName == trackName);

        if (track == null)
        {
            Debug.LogError($"Music track not found: {trackName}");
            return;
        }

        currentMusicTrack = trackName; // Store for saving

        // Stop any currently running fades to prevent conflicts.
        StopAllCoroutines();

        if (fadeInDuration > 0)
        {
            StartCoroutine(FadeInMusic(track.clip, fadeInDuration));
        }
        else
        {
            musicSource.volume = musicVolume; // Ensure volume is correct for instant playback
            musicSource.clip = track.clip;
            musicSource.Play();
        }
    }

    public void StopMusic(float fadeOutDuration = 0f)
    {
        currentMusicTrack = null;

        // Stop any currently running fades to prevent conflicts.
        StopAllCoroutines();

        if (fadeOutDuration > 0 && musicSource.isPlaying)
        {
            StartCoroutine(FadeOutMusic(fadeOutDuration));
        }
        else
        {
            musicSource.Stop();
        }
    }

    private IEnumerator FadeInMusic(AudioClip clip, float duration)
    {
        musicSource.clip = clip;
        musicSource.volume = 0f;
        musicSource.Play();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, musicVolume, elapsed / duration);
            yield return null;
        }

        musicSource.volume = musicVolume;
    }

    private IEnumerator FadeOutMusic(float duration)
    {
        float startVolume = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        musicSource.volume = 0f;
        musicSource.Stop();
    }

    // ==================== SOUND EFFECTS ====================

    public void PlaySFX(string sfxName)
    {
        AudioClip clip = null;
        AudioData sfx = soundEffects.Find(s => s.clipName == sfxName);
        if (sfx != null)
        {
            clip = sfx.clip;
        }
        else if (!cachedResourceSfx.TryGetValue(sfxName, out clip) || clip == null)
        {
            clip = Resources.Load<AudioClip>($"Sound/{sfxName}");
            if (clip != null)
            {
                cachedResourceSfx[sfxName] = clip;
            }
        }

        if (clip == null)
        {
            Debug.LogError($"Sound effect not found: {sfxName}");
            return;
        }

        if (sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, sfxOneShotScale);
        }
    }

    public void PlayUISound(UISoundType type)
    {
        AudioClip clip = null;
        switch (type)
        {
            case UISoundType.Select:
                if (uiSelectClip == null && !string.IsNullOrEmpty(uiSelectResourcePath))
                    uiSelectClip = Resources.Load<AudioClip>(uiSelectResourcePath);
                clip = uiSelectClip;
                break;
            case UISoundType.Cancel:
                if (uiCancelClip == null && !string.IsNullOrEmpty(uiCancelResourcePath))
                    uiCancelClip = Resources.Load<AudioClip>(uiCancelResourcePath);
                clip = uiCancelClip;
                break;
            case UISoundType.Confirm:
            default:
                if (uiConfirmClip == null && !string.IsNullOrEmpty(uiConfirmResourcePath))
                    uiConfirmClip = Resources.Load<AudioClip>(uiConfirmResourcePath);
                clip = uiConfirmClip;
                break;
        }

        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, uiButtonVolume);
        }
    }

    // ==================== VOICE ====================

    public void PlayVoice(string voiceName)
    {
        AudioData voice = voiceClips.Find(v => v.clipName == voiceName);

        if (voice == null)
        {
            Debug.LogError($"Voice clip not found: {voiceName}");
            return;
        }

        voiceSource.clip = voice.clip;
        voiceSource.Play();
    }

    public void StopVoice()
    {
        voiceSource.Stop();
    }

    // ==================== VOLUME CONTROL ====================

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        musicSource.volume = musicVolume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        sfxSource.volume = sfxVolume;
    }

    public void SetVoiceVolume(float volume)
    {
        voiceVolume = Mathf.Clamp01(volume);
        voiceSource.volume = voiceVolume;
    }

    // Add these new methods
    public string GetCurrentMusicTrack()
    {
        return currentMusicTrack;
    }

    public void RestoreState(string musicTrackName)
    {
        if (!string.IsNullOrEmpty(musicTrackName))
        {
            if (currentMusicTrack == musicTrackName && musicSource != null && musicSource.isPlaying)
            {
                return;
            }

            // Instantly play the music track, no fade.
            PlayMusic(musicTrackName);
        }
        else
        {
            StopMusic();
        }
    }
}

[System.Serializable]
public class AudioData
{
    public string clipName;
    public AudioClip clip;
}