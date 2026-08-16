using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedText : MonoBehaviour
{
    private TextMeshProUGUI textField;

    [Tooltip("The translation key in the localization JSON files (e.g. 'menu_start').")]
    public string localizationKey;

    [Tooltip("Font category to use from LocalizationManager (e.g. 'Button', 'Speaker', 'Dialogue', 'Common').")]
    public string fontCategory = "Button";

    private void Awake()
    {
        if (textField == null) textField = GetComponent<TextMeshProUGUI>();
    }

    void Start()
    {
        if (textField == null) textField = GetComponent<TextMeshProUGUI>();

        // Subscribe to the event.
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChange;
        }

        UpdateTextAndFont();
    }

    void OnDestroy()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChange;
        }
    }

    // When the language changes, this will update BOTH the font and the text.
    private void HandleLanguageChange(TMP_FontAsset defaultFont)
    {
        UpdateTextAndFont(defaultFont);
    }

    public void UpdateTextAndFont(TMP_FontAsset fallbackFont = null)
    {
        if (textField == null) textField = GetComponent<TextMeshProUGUI>();
        if (textField == null) return;

        if (LocalizationManager.Instance != null)
        {
            TMP_FontAsset targetFont = LocalizationManager.Instance.GetFont(fontCategory);
            if (targetFont != null)
            {
                textField.font = targetFont;
            }
            else if (fallbackFont != null)
            {
                textField.font = fallbackFont;
            }

            if (!string.IsNullOrEmpty(localizationKey))
            {
                textField.text = LocalizationManager.Instance.GetLocalizedValue(localizationKey);
            }
        }
    }

    public void UpdateText()
    {
        UpdateTextAndFont();
    }
}