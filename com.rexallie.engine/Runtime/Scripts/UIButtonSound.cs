using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    public AudioManager.UISoundType hoverSound = AudioManager.UISoundType.Select;
    public AudioManager.UISoundType clickSound = AudioManager.UISoundType.Confirm;
    public bool playHoverSound = true;
    public bool playClickSound = true;

    private Button button;
    private float lastSelectSoundTime = -1f;
    private static AudioSource fallbackAudioSource;
    private static readonly Dictionary<AudioManager.UISoundType, AudioClip> fallbackClips = new Dictionary<AudioManager.UISoundType, AudioClip>();

    void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
    }

    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!playHoverSound || button == null || !button.IsInteractable()) return;
        PlayHoverWithCooldown();
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!playHoverSound || button == null || !button.IsInteractable()) return;
        // Avoid double-triggering when pointer click/hover selects the button on the same frame
        if (eventData is PointerEventData) return;
        PlayHoverWithCooldown();
    }

    private void OnButtonClicked()
    {
        if (!playClickSound) return;
        PlayGlobal(clickSound);
    }

    private void PlayHoverWithCooldown()
    {
        if (Time.unscaledTime - lastSelectSoundTime < 0.05f) return;
        lastSelectSoundTime = Time.unscaledTime;
        PlayGlobal(hoverSound);
    }

    public static void PlayGlobal(AudioManager.UISoundType soundType)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUISound(soundType);
            return;
        }

        AudioClip clip = GetFallbackClip(soundType);
        if (clip == null) return;

        AudioSource source = GetOrCreateFallbackSource();
        if (source != null)
        {
            float vol = SettingsManager.Instance != null ? SettingsManager.Instance.currentSettings.sfxVolume : 0.5f;
            source.PlayOneShot(clip, vol * 0.25f);
        }
    }

    private static AudioClip GetFallbackClip(AudioManager.UISoundType soundType)
    {
        if (fallbackClips.TryGetValue(soundType, out AudioClip cached) && cached != null)
        {
            return cached;
        }

        string path = soundType switch
        {
            AudioManager.UISoundType.Select => "Sound/select",
            AudioManager.UISoundType.Cancel => "Sound/cancel",
            _ => "Sound/confirm"
        };

        AudioClip loaded = Resources.Load<AudioClip>(path);
        fallbackClips[soundType] = loaded;
        return loaded;
    }

    private static AudioSource GetOrCreateFallbackSource()
    {
        if (fallbackAudioSource != null) return fallbackAudioSource;

        GameObject obj = new GameObject("UIButtonSound_FallbackAudio");
        DontDestroyOnLoad(obj);
        fallbackAudioSource = obj.AddComponent<AudioSource>();
        fallbackAudioSource.playOnAwake = false;
        fallbackAudioSource.spatialBlend = 0f;

        if (SettingsManager.Instance != null && SettingsManager.Instance.masterMixer != null)
        {
            AudioMixerGroup[] groups = SettingsManager.Instance.masterMixer.FindMatchingGroups("SFX");
            if (groups != null && groups.Length > 0)
            {
                fallbackAudioSource.outputAudioMixerGroup = groups[0];
            }
        }

        return fallbackAudioSource;
    }

    public static void AttachAll(GameObject root, Func<Button, bool> isCancelButton = null)
    {
        if (root == null) return;
        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons)
        {
            if (btn == null) continue;
            UIButtonSound sound = btn.GetComponent<UIButtonSound>();
            if (sound == null)
            {
                sound = btn.gameObject.AddComponent<UIButtonSound>();
            }

            bool isCancel = isCancelButton != null && isCancelButton(btn);
            if (!isCancel)
            {
                string lowerName = btn.gameObject.name.ToLowerInvariant();
                isCancel = lowerName.Contains("close") || lowerName.Contains("cancel") || lowerName.Contains("exit");
            }

            if (isCancel)
            {
                sound.clickSound = AudioManager.UISoundType.Cancel;
            }
        }
    }

    public static void Attach(Button btn, AudioManager.UISoundType clickSound = AudioManager.UISoundType.Confirm)
    {
        if (btn == null) return;
        UIButtonSound sound = btn.GetComponent<UIButtonSound>();
        if (sound == null)
        {
            sound = btn.gameObject.AddComponent<UIButtonSound>();
        }
        sound.clickSound = clickSound;
    }
}
