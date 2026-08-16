using UnityEngine;
using UnityEngine.Audio; // Required for AudioMixer

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    public SettingsData currentSettings;

    // Key used to save/load settings from PlayerPrefs
    private const string SettingsKey = "GameSettings";

    [Header("Audio Mixer References")]
    [Tooltip("Drag your main AudioMixer asset here.")]
    public AudioMixer masterMixer;
    public string masterVolumeParam = "MasterVolume";
    public string musicVolumeParam = "MusicVolume";
    public string sfxVolumeParam = "SFXVolume";
    public string voiceVolumeParam = "VoiceVolume";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Apply loaded settings when the game starts
        ApplyAllSettings();
    }

    // --- Loading & Saving ---

    private void LoadSettings()
    {
        if (PlayerPrefs.HasKey(SettingsKey))
        {
            string json = PlayerPrefs.GetString(SettingsKey);
            currentSettings = JsonUtility.FromJson<SettingsData>(json);

            // Ensure resolution index is valid after loading
            if (currentSettings.resolutionIndex >= Screen.resolutions.Length)
            {
                currentSettings.resolutionIndex = -1; // Reset to native if invalid
            }
        }
        else
        {
            currentSettings = new SettingsData(); // Load defaults
        }
    }

    public void SaveSettings()
    {
        string json = JsonUtility.ToJson(currentSettings, true);
        PlayerPrefs.SetString(SettingsKey, json);
        PlayerPrefs.Save();
    }

    // --- Applying Settings ---

    public void ApplyAllSettings()
    {
        ApplyDisplaySettings();
        ApplyAudioSettings();
        ApplyGameplaySettings();
        ApplyLanguageSetting();
    }

    public void ApplyDisplaySettings()
    {
        // Apply Display Mode
        FullScreenMode mode = FullScreenMode.Windowed;
        switch (currentSettings.displayMode)
        {
            case 0: mode = FullScreenMode.ExclusiveFullScreen; break;
            case 1: mode = FullScreenMode.FullScreenWindow; break; // Borderless
            case 2: mode = FullScreenMode.Windowed; break;
        }

        // Apply Resolution (if not using native)
        Resolution? targetResolution = null;
        if (currentSettings.resolutionIndex >= 0 && currentSettings.resolutionIndex < Screen.resolutions.Length)
        {
            targetResolution = Screen.resolutions[currentSettings.resolutionIndex];
        }

        if (targetResolution.HasValue)
        {
            Screen.SetResolution(targetResolution.Value.width, targetResolution.Value.height, mode);
        }
        else
        {
            // If index is invalid or -1, try to apply mode with current resolution
            // For Borderless, this usually snaps to native automatically.
            Screen.fullScreenMode = mode;
            // If we switched TO borderless, explicitly set native resolution
            if (mode == FullScreenMode.FullScreenWindow)
            {
                Resolution nativeRes = Screen.resolutions[Screen.resolutions.Length - 1];
                Screen.SetResolution(nativeRes.width, nativeRes.height, mode);
            }
        }


        // Apply VSync
        QualitySettings.vSyncCount = currentSettings.vsyncEnabled ? 1 : 0;
    }

    public void ApplyAudioSettings()
    {
        // Convert linear volume (0-1) to logarithmic dB (-80 to 0) for the mixer
        masterMixer.SetFloat(masterVolumeParam, Mathf.Log10(Mathf.Max(currentSettings.masterVolume, 0.0001f)) * 20f);
        masterMixer.SetFloat(musicVolumeParam, Mathf.Log10(Mathf.Max(currentSettings.musicVolume, 0.0001f)) * 20f);
        masterMixer.SetFloat(sfxVolumeParam, Mathf.Log10(Mathf.Max(currentSettings.sfxVolume, 0.0001f)) * 20f);
        masterMixer.SetFloat(voiceVolumeParam, Mathf.Log10(Mathf.Max(currentSettings.voiceVolume, 0.0001f)) * 20f);
    }

    public void ApplyGameplaySettings()
    {
        // Apply Text Speed (assuming DialogueAnimator is in the scene)
        DialogueAnimator animator = FindObjectOfType<DialogueAnimator>();
        if (animator != null)
        {
            animator.SetTypeSpeed(currentSettings.textSpeed);
        }

        // Apply Auto-Advance Delay (assuming UIManager is in the scene)
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            uiManager.SetAutoAdvanceDelay(currentSettings.autoAdvanceDelay);
        }

        // Apply Dialogue Opacity
        if (uiManager != null)
        {
            uiManager.SetDialogueOpacity(currentSettings.textBoxOpacity);
        }

        // Apply Skip Mode preference (DialogueManager reads this directly when needed)
        // No direct action here, but DialogueManager needs updating.
    }

    public void ApplyLanguageSetting()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.LoadLanguage(currentSettings.languageCode);
        }
    }
}
