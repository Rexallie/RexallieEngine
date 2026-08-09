using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class PreferencesPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;

    [Header("Display Settings")]
    [SerializeField] private TMP_Dropdown displayModeDropdown;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle vsyncToggle;
    [SerializeField] private TMP_Dropdown languageDropdown;

    [Header("Audio Settings")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Slider voiceVolumeSlider;

    [Header("Gameplay Settings")]
    [SerializeField] private Slider textSpeedSlider;
    [SerializeField] private Slider autoDelaySlider;
    [SerializeField] private Toggle skipUnreadToggle; // True = Skip Read Only, False = Skip All
    [SerializeField] private Slider textBoxOpacitySlider;

    private List<Resolution> availableResolutions;

    void Start()
    {
        panelRoot.SetActive(false); // Start hidden
        closeButton.onClick.AddListener(Hide);

        PopulateDisplayOptions();
        SetupListeners();
        LoadCurrentSettings();
    }

    private void PopulateDisplayOptions()
    {
        // Display Modes
        displayModeDropdown.ClearOptions();
        displayModeDropdown.AddOptions(new List<string> { "Fullscreen", "Borderless", "Windowed" });

        // Resolutions
        availableResolutions = Screen.resolutions.ToList();
        resolutionDropdown.ClearOptions();
        List<string> resOptions = new List<string>();
        int currentResIndex = -1;
        for (int i = 0; i < availableResolutions.Count; i++)
        {
            Resolution res = availableResolutions[i];
            resOptions.Add($"{res.width} x {res.height} @ {res.refreshRateRatio}Hz");
            if (res.width == Screen.currentResolution.width && res.height == Screen.currentResolution.height)
            {
                currentResIndex = i;
            }
        }
        resolutionDropdown.AddOptions(resOptions);
        // Try to select current resolution, fallback to last (usually native)
        if (SettingsManager.Instance.currentSettings.resolutionIndex == -1)
            SettingsManager.Instance.currentSettings.resolutionIndex = currentResIndex != -1 ? currentResIndex : availableResolutions.Count - 1;

        // Languages
        languageDropdown.ClearOptions();
        languageDropdown.AddOptions(new List<string> { "English", "日本語", "中文" }); // Display names
    }

    private void SetupListeners()
    {
        // Display
        displayModeDropdown.onValueChanged.AddListener(OnDisplayModeChanged);
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);
        languageDropdown.onValueChanged.AddListener(OnLanguageChanged);

        // Audio
        masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        voiceVolumeSlider.onValueChanged.AddListener(OnVoiceVolumeChanged);

        // Gameplay
        textSpeedSlider.onValueChanged.AddListener(OnTextSpeedChanged);
        autoDelaySlider.onValueChanged.AddListener(OnAutoDelayChanged);
        skipUnreadToggle.onValueChanged.AddListener(OnSkipModeChanged);
        textBoxOpacitySlider.onValueChanged.AddListener(OnOpacityChanged);
    }

    // Load settings from manager into UI elements
    private void LoadCurrentSettings()
    {
        SettingsData settings = SettingsManager.Instance.currentSettings;

        displayModeDropdown.SetValueWithoutNotify(settings.displayMode);
        resolutionDropdown.SetValueWithoutNotify(settings.resolutionIndex);
        vsyncToggle.SetIsOnWithoutNotify(settings.vsyncEnabled);

        // Set language dropdown based on code
        int langIndex = 0;
        if (settings.languageCode == "ja") langIndex = 1;
        else if (settings.languageCode == "zh") langIndex = 2;
        languageDropdown.SetValueWithoutNotify(langIndex);

        masterVolumeSlider.SetValueWithoutNotify(settings.masterVolume);
        musicVolumeSlider.SetValueWithoutNotify(settings.musicVolume);
        sfxVolumeSlider.SetValueWithoutNotify(settings.sfxVolume);
        voiceVolumeSlider.SetValueWithoutNotify(settings.voiceVolume);

        textSpeedSlider.SetValueWithoutNotify(settings.textSpeed);
        autoDelaySlider.SetValueWithoutNotify(settings.autoAdvanceDelay);
        skipUnreadToggle.SetIsOnWithoutNotify(settings.skipUnreadText);
        textBoxOpacitySlider.SetValueWithoutNotify(settings.textBoxOpacity);

        // Disable resolution dropdown if in borderless mode
        resolutionDropdown.interactable = settings.displayMode != 1;
    }

    // --- Callback Methods for UI Changes ---

    private void OnDisplayModeChanged(int index)
    {
        SettingsManager.Instance.currentSettings.displayMode = index;
        resolutionDropdown.interactable = index != 1; // Disable resolution for Borderless
        SettingsManager.Instance.ApplyDisplaySettings();
        SettingsManager.Instance.SaveSettings();
    }

    private void OnResolutionChanged(int index)
    {
        SettingsManager.Instance.currentSettings.resolutionIndex = index;
        SettingsManager.Instance.ApplyDisplaySettings();
        SettingsManager.Instance.SaveSettings();
    }

    private void OnVSyncChanged(bool isOn)
    {
        SettingsManager.Instance.currentSettings.vsyncEnabled = isOn;
        SettingsManager.Instance.ApplyDisplaySettings();
        SettingsManager.Instance.SaveSettings();
    }

    private void OnLanguageChanged(int index)
    {
        string langCode = "en";
        if (index == 1) langCode = "ja";
        else if (index == 2) langCode = "zh";
        SettingsManager.Instance.currentSettings.languageCode = langCode;
        SettingsManager.Instance.ApplyLanguageSetting();
        SettingsManager.Instance.SaveSettings();
    }

    private void OnMasterVolumeChanged(float value)
    {
        SettingsManager.Instance.currentSettings.masterVolume = value;
        SettingsManager.Instance.ApplyAudioSettings();
        SettingsManager.Instance.SaveSettings();
    }
    private void OnMusicVolumeChanged(float value)
    {
        SettingsManager.Instance.currentSettings.musicVolume = value;
        SettingsManager.Instance.ApplyAudioSettings();
        // No need to call SaveSettings on every tiny slider change, maybe only on Apply/Close?
        // For simplicity, we save on every change for now.
        SettingsManager.Instance.SaveSettings();
    }
    private void OnSFXVolumeChanged(float value)
    {
        SettingsManager.Instance.currentSettings.sfxVolume = value;
        SettingsManager.Instance.ApplyAudioSettings();
        SettingsManager.Instance.SaveSettings();
    }
    private void OnVoiceVolumeChanged(float value)
    {
        SettingsManager.Instance.currentSettings.voiceVolume = value;
        SettingsManager.Instance.ApplyAudioSettings();
        SettingsManager.Instance.SaveSettings();
    }

    private void OnTextSpeedChanged(float value)
    {
        SettingsManager.Instance.currentSettings.textSpeed = value;
        SettingsManager.Instance.ApplyGameplaySettings();
        SettingsManager.Instance.SaveSettings();
    }

    private void OnAutoDelayChanged(float value)
    {
        SettingsManager.Instance.currentSettings.autoAdvanceDelay = value;
        SettingsManager.Instance.ApplyGameplaySettings();
        SettingsManager.Instance.SaveSettings();
    }
    private void OnSkipModeChanged(bool skipUnreadOnly)
    {
        SettingsManager.Instance.currentSettings.skipUnreadText = skipUnreadOnly;
        // ApplyGameplaySettings handles applying this implicitly via DialogueManager
        SettingsManager.Instance.SaveSettings();
    }
    private void OnOpacityChanged(float value)
    {
        SettingsManager.Instance.currentSettings.textBoxOpacity = value;
        SettingsManager.Instance.ApplyGameplaySettings();
        SettingsManager.Instance.SaveSettings();
    }

    // --- Public Show/Hide ---
    public void Show() 
    { 
        panelRoot.SetActive(true); 
        LoadCurrentSettings(); 

        if (EventSystem.current != null)
        {
            if (displayModeDropdown != null)
            {
                EventSystem.current.SetSelectedGameObject(displayModeDropdown.gameObject);
            }
            else if (closeButton != null)
            {
                EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
            }
        }
    }
    public void Hide() { panelRoot.SetActive(false); }
}