using UnityEngine;

// This class holds all the game settings that need to be saved.
[System.Serializable]
public class SettingsData
{
    // Display & Graphics
    public int displayMode; // 0 = Exclusive Fullscreen, 1 = Borderless, 2 = Windowed
    public int resolutionIndex; // Index in the Screen.resolutions array
    public bool vsyncEnabled;
    public string languageCode;

    // Audio
    public float masterVolume; // Values will be 0.0 to 1.0
    public float musicVolume;
    public float sfxVolume;
    public float voiceVolume;

    // Gameplay & Text
    public float textSpeed; // Characters per second
    public float autoAdvanceDelay; // Seconds
    public bool skipUnreadText; // False = Skip All, True = Skip Read Only
    public float textBoxOpacity; // 0.0 to 1.0

    // Constructor to set default values
    public SettingsData()
    {
        displayMode = 1; // Default to Borderless Fullscreen
        resolutionIndex = -1; // -1 means use native resolution initially
        vsyncEnabled = true;
        languageCode = "en";

        masterVolume = 0.8f;
        musicVolume = 0.7f;
        sfxVolume = 1.0f;
        voiceVolume = 1.0f;

        textSpeed = 90f;
        autoAdvanceDelay = 2.0f;
        skipUnreadText = false;
        textBoxOpacity = 1.0f;
    }
}