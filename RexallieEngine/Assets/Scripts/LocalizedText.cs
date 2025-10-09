using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedText : MonoBehaviour
{
    private TextMeshProUGUI textField;
    public string localizationKey;

    void Start()
    {
        textField = GetComponent<TextMeshProUGUI>();
        // Subscribe to the event.
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChange;
        }
    }

    void OnDestroy()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChange;
        }
    }

    // When the language changes, this will update BOTH the font and the text.
    private void HandleLanguageChange(TMP_FontAsset newFont)
    {
        textField.font = newFont;
        UpdateText();
    }

    public void UpdateText()
    {
        if (LocalizationManager.Instance != null && !string.IsNullOrEmpty(localizationKey))
        {
            textField.text = LocalizationManager.Instance.GetLocalizedValue(localizationKey);
        }
    }
}