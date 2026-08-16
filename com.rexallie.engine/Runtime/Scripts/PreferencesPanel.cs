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

    void Awake()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += OnSystemLanguageChanged;
        }
    }

    void OnDestroy()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= OnSystemLanguageChanged;
        }
    }

    private void OnSystemLanguageChanged(TMP_FontAsset defaultFont)
    {
        AutoLocalizePanel();
    }

    void Start()
    {
        panelRoot.SetActive(false); // Start hidden
        closeButton.onClick.AddListener(Hide);

        PopulateDisplayOptions();
        SetupListeners();
        LoadCurrentSettings();
        AutoLocalizePanel();
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
        if (SettingsManager.Instance.currentSettings.resolutionIndex == -1)
            SettingsManager.Instance.currentSettings.resolutionIndex = currentResIndex != -1 ? currentResIndex : availableResolutions.Count - 1;

        // Languages
        languageDropdown.ClearOptions();
        languageDropdown.AddOptions(new List<string> { "English", "日本語", "中文" });

        // Ensure language dropdown uses a font that supports Japanese and Chinese glyphs
        TMP_FontAsset cjkFont = Resources.Load<TMP_FontAsset>("Fonts/Japanese/Kosugi-Regular SDF") 
                             ?? Resources.Load<TMP_FontAsset>("Fonts/Chinese/ChineseBasic SDF");
        if (cjkFont != null)
        {
            if (languageDropdown.captionText != null) languageDropdown.captionText.font = cjkFont;
            if (languageDropdown.itemText != null) languageDropdown.itemText.font = cjkFont;
        }
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
        displayModeDropdown.RefreshShownValue();

        resolutionDropdown.SetValueWithoutNotify(settings.resolutionIndex);
        resolutionDropdown.RefreshShownValue();

        vsyncToggle.SetIsOnWithoutNotify(settings.vsyncEnabled);

        // Set language dropdown based on code
        int langIndex = 0;
        if (settings.languageCode == "ja") langIndex = 1;
        else if (settings.languageCode == "zh") langIndex = 2;
        languageDropdown.SetValueWithoutNotify(langIndex);
        languageDropdown.RefreshShownValue();

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
        SettingsManager.Instance.SaveSettings();
    }
    private void OnOpacityChanged(float value)
    {
        SettingsManager.Instance.currentSettings.textBoxOpacity = value;
        SettingsManager.Instance.ApplyGameplaySettings();
        SettingsManager.Instance.SaveSettings();
    }

    private void AutoLocalizePanel()
    {
        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var txt in texts)
        {
            if (txt == null) continue;

            string objName = txt.gameObject.name.ToLower();
            // Skip dropdown template labels and caption texts so their fonts & options are not corrupted
            if (objName.Contains("item label") || (objName.Contains("label") && txt.transform.parent != null && txt.transform.parent.GetComponent<TMP_Dropdown>() != null))
            {
                continue;
            }

            LocalizedText loc = txt.GetComponent<LocalizedText>();
            if (loc == null)
            {
                loc = txt.gameObject.AddComponent<LocalizedText>();
            }
            loc.fontCategory = "Button";

            if (string.IsNullOrEmpty(loc.localizationKey))
            {
                string textVal = txt.text.Trim().ToLower();

                if (objName.Contains("mastervolume") || textVal.Contains("master volume"))
                    loc.localizationKey = "ui_master_volume";
                else if (objName.Contains("musicvolume") || textVal.Contains("music volume"))
                    loc.localizationKey = "ui_music_volume";
                else if (objName.Contains("sfxvolume") || objName.Contains("soundvolume") || textVal.Contains("sfx volume") || textVal.Contains("sound volume"))
                    loc.localizationKey = "ui_sfx_volume";
                else if (objName.Contains("voicevolume") || textVal.Contains("voice volume"))
                    loc.localizationKey = "ui_voice_volume";
                else if (objName.Contains("textspeed") || textVal.Contains("text speed"))
                    loc.localizationKey = "ui_text_speed";
                else if (objName.Contains("autodelay") || objName.Contains("autoforward") || textVal.Contains("auto forward") || textVal.Contains("auto delay"))
                    loc.localizationKey = "ui_auto_forward";
                else if (objName.Contains("displaymode") || textVal.Contains("display mode"))
                    loc.localizationKey = "ui_display_mode";
                else if (objName.Contains("resolution") || textVal.Contains("resolution"))
                    loc.localizationKey = "ui_resolution";
                else if (objName.Contains("vsync") || textVal.Contains("v-sync") || textVal.Contains("vsync"))
                    loc.localizationKey = "ui_vsync";
                else if (objName.Contains("language") || textVal.Contains("language"))
                    loc.localizationKey = "ui_language";
                else if (objName.Contains("skipunread") || textVal.Contains("skip unread"))
                    loc.localizationKey = "ui_skip_unread";
                else if (objName.Contains("opacity") || textVal.Contains("opacity"))
                    loc.localizationKey = "ui_textbox_opacity";
                else if (objName.Contains("title") && textVal.Contains("preferences"))
                    loc.localizationKey = "ui_preferences";
                else if (textVal.Equals("preferences"))
                    loc.localizationKey = "ui_preferences";
                else if (textVal.Contains("close") || objName.Contains("close"))
                    loc.localizationKey = "ui_close";
                else if (textVal.Contains("back") || objName.Contains("back"))
                    loc.localizationKey = "ui_back";
            }

            loc.UpdateTextAndFont();
        }

        UpdateDropdownTranslations();
    }

    private void UpdateDropdownTranslations()
    {
        if (displayModeDropdown != null)
        {
            int currentVal = displayModeDropdown.value;
            displayModeDropdown.ClearOptions();

            string fullscreen = LocalizationManager.Instance != null ? LocalizationManager.Instance.GetLocalizedValue("ui_fullscreen") : "Fullscreen";
            string borderless = LocalizationManager.Instance != null ? LocalizationManager.Instance.GetLocalizedValue("ui_borderless") : "Borderless";
            string windowed = LocalizationManager.Instance != null ? LocalizationManager.Instance.GetLocalizedValue("ui_windowed") : "Windowed";

            if (string.IsNullOrEmpty(fullscreen) || fullscreen == "ui_fullscreen") fullscreen = "Fullscreen";
            if (string.IsNullOrEmpty(borderless) || borderless == "ui_borderless") borderless = "Borderless";
            if (string.IsNullOrEmpty(windowed) || windowed == "ui_windowed") windowed = "Windowed";

            displayModeDropdown.AddOptions(new List<string> { fullscreen, borderless, windowed });
            displayModeDropdown.SetValueWithoutNotify(currentVal);
            displayModeDropdown.RefreshShownValue();
        }

        if (languageDropdown != null)
        {
            languageDropdown.RefreshShownValue();
        }

        if (resolutionDropdown != null)
        {
            resolutionDropdown.RefreshShownValue();
        }
    }

    // --- Public Show/Hide ---
    public void Show() 
    { 
        panelRoot.SetActive(true); 
        LoadCurrentSettings(); 
        AutoLocalizePanel();

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
