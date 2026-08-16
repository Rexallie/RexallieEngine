using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System;

// Helper classes for JSON deserialization
[System.Serializable] public class LocalizationData { public LocalizationItem[] items; }
[System.Serializable] public class LocalizationItem { public string key; public string value; }

[System.Serializable]
public class LanguageFontMapping
{
    public string languageCode;
    public TMP_FontAsset fontAsset;
}

[System.Serializable]
public class FontCategoryMapping
{
    public string categoryName; // "Button", "Speaker", "Dialogue", "Common"
    public List<LanguageFontMapping> fonts = new List<LanguageFontMapping>();
}

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    [Header("Language Settings")]
    [Tooltip("Default font mapping per language (General / Fallback).")]
    public List<LanguageFontMapping> languageFonts = new List<LanguageFontMapping>();

    [Tooltip("Categorized font mappings per language (e.g., 'Button', 'Speaker', 'Dialogue').")]
    public List<FontCategoryMapping> fontCategories = new List<FontCategoryMapping>();

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
            if (loadedData != null && loadedData.items != null)
            {
                foreach (var item in loadedData.items)
                {
                    localizedText[item.key] = item.value;
                }
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

        // 2. Find the correct font for the new language (Dialogue category or general fallback).
        TMP_FontAsset newFont = GetFont("Dialogue", langCode);

        // 3. Fire the event to tell all UI elements to update their font and text.
        OnLanguageChanged?.Invoke(newFont);
    }

    public TMP_FontAsset GetFont(string category = "Dialogue", string langCode = null)
    {
        if (string.IsNullOrEmpty(langCode))
        {
            langCode = PlayerPrefs.GetString("language", defaultLanguage);
        }

        // 1. Check specific category
        if (fontCategories != null && !string.IsNullOrEmpty(category))
        {
            var cat = fontCategories.Find(c => string.Equals(c.categoryName, category, StringComparison.OrdinalIgnoreCase));
            if (cat != null && cat.fonts != null)
            {
                var mapping = cat.fonts.Find(f => f.languageCode == langCode);
                if (mapping != null && mapping.fontAsset != null) return mapping.fontAsset;

                // Fallback to defaultLanguage in this category
                mapping = cat.fonts.Find(f => f.languageCode == defaultLanguage);
                if (mapping != null && mapping.fontAsset != null) return mapping.fontAsset;
            }
        }

        // 2. Fallback to general languageFonts list
        if (languageFonts != null)
        {
            var generalMapping = languageFonts.Find(f => f.languageCode == langCode);
            if (generalMapping != null && generalMapping.fontAsset != null) return generalMapping.fontAsset;

            generalMapping = languageFonts.Find(f => f.languageCode == defaultLanguage);
            if (generalMapping != null && generalMapping.fontAsset != null) return generalMapping.fontAsset;

            if (languageFonts.Count > 0 && languageFonts[0].fontAsset != null)
            {
                return languageFonts[0].fontAsset;
            }
        }

        return null;
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