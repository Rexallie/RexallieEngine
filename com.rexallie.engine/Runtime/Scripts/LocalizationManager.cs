using UnityEngine;
using System.Collections.Generic;
using TMPro; // Add this for TextMesh Pro
using System; // Add this for Action

// Helper classes for JSON deserialization (no changes here)
[System.Serializable] public class LocalizationData { public LocalizationItem[] items; }
[System.Serializable] public class LocalizationItem { public string key; public string value; }

// NEW: A class to link a language code to a font asset in the Inspector.
[System.Serializable]
public class LanguageFontMapping
{
    public string languageCode;
    public TMP_FontAsset fontAsset;
}

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    [Header("Language Settings")]
    [Tooltip("Links language codes (e.g., 'en', 'ja') to specific font assets.")]
    public List<LanguageFontMapping> languageFonts;

    // This event will notify all UI elements when the language (and font) changes.
    public event Action<TMP_FontAsset> OnLanguageChanged;

    private Dictionary<string, string> localizedText;
    private const string defaultLanguage = "en";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // Load the language saved from the last session, or the default.
            LoadLanguage(PlayerPrefs.GetString("language", defaultLanguage));
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadLanguage(string langCode)
    {
        // 1. Load the translation text from the JSON file.
        string filePath = $"Localization/{langCode}";
        TextAsset targetFile = Resources.Load<TextAsset>(filePath);

        if (targetFile != null)
        {
            LocalizationData loadedData = JsonUtility.FromJson<LocalizationData>(targetFile.text);
            localizedText = new Dictionary<string, string>();
            foreach (var item in loadedData.items)
            {
                localizedText.Add(item.key, item.value);
            }
            PlayerPrefs.SetString("language", langCode);
            Debug.Log($"Successfully loaded localization for language: {langCode}");
        }
        else
        {
            Debug.LogError($"Cannot find localization file: {filePath}. Reverting to default.");
            if (langCode != defaultLanguage) { LoadLanguage(defaultLanguage); }
            return;
        }

        // 2. Find the correct font for the new language.
        TMP_FontAsset newFont = languageFonts.Find(f => f.languageCode == langCode)?.fontAsset;
        if (newFont == null)
        {
            // Fallback to the default language font if the specific one isn't found.
            newFont = languageFonts.Find(f => f.languageCode == defaultLanguage)?.fontAsset;
        }

        // 3. Fire the event to tell all UI elements to update their font and text.
        if (newFont != null)
        {
            OnLanguageChanged?.Invoke(newFont);
        }
        else
        {
            Debug.LogError($"No font asset found for language '{langCode}' or for the default language.");
        }
    }

    public string GetLocalizedValue(string key)
    {
        if (localizedText != null && localizedText.TryGetValue(key, out string value))
        {
            return value;
        }
        return key;
    }
}