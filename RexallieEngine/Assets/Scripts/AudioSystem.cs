using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource voiceSource;

    [Header("Audio Database")]
    public List<AudioData> musicTracks = new List<AudioData>();
    public List<AudioData> soundEffects = new List<AudioData>();
    public List<AudioData> voiceClips = new List<AudioData>();

    [Header("Settings")]
    public float musicVolume = 0.7f;
    public float sfxVolume = 1f;
    public float voiceVolume = 1f;

    // We no longer need the 'musicFadeCoroutine' variable.

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
        AudioData sfx = soundEffects.Find(s => s.clipName == sfxName);

        if (sfx == null)
        {
            Debug.LogError($"Sound effect not found: {sfxName}");
            return;
        }

        sfxSource.PlayOneShot(sfx.clip);
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
}

[System.Serializable]
public class AudioData
{
    public string clipName;
    public AudioClip clip;
}