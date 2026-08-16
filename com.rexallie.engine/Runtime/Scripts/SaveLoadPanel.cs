using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class SaveLoadPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private RectTransform contentArea;
    [SerializeField] private Button closeButton;

    // --- NEW: References for the Rename Panel ---
    [Header("Rename Panel")]
    [SerializeField] private GameObject renamePanel;
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private Button confirmRenameButton;
    [SerializeField] private Button cancelRenameButton;

    [Header("Title References")]
    [SerializeField] private TextMeshProUGUI panelTitleText;

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private float startScale = 0.9f;

    private bool isSaveMode;
    private CanvasGroup canvasGroup;
    private Coroutine animationCoroutine;

    // --- NEW: To store which slot we are saving to ---
    private int currentSlotToSave;
    private ScrollRect scrollRect;
    private GameObject lastSelectedSlotButton = null;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        transform.localScale = Vector3.one * startScale;
        scrollRect = GetComponentInChildren<ScrollRect>();

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
        UpdateTitleText();
    }

    void Start()
    {
        closeButton.onClick.AddListener(Hide);

        // --- NEW: Hook up the rename panel buttons ---
        confirmRenameButton.onClick.AddListener(OnConfirmRename);
        cancelRenameButton.onClick.AddListener(HideRenamePrompt);
        renamePanel.SetActive(false);

        AutoLocalizePanel();
    }

    public void Show(bool isSaving)
    {
        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(ShowPanelCoroutine(isSaving));
    }

    public void Hide()
    {
        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(HidePanelCoroutine());
    }

    // --- NEW: This method will be called to refresh the save slots ---
    public void Refresh()
    {
        PopulateSlots();
    }

    private void PopulateSlots()
    {
        foreach (Transform child in contentArea)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < SaveManager.Instance.MaxSaveSlots; i++)
        {
            GameObject newSlot = Instantiate(slotPrefab, contentArea);
            SaveMetadata metadata = SaveManager.Instance.GetSaveMetadata(i);

            SaveSlotUI slotUI = newSlot.GetComponent<SaveSlotUI>();
            slotUI.Configure(i, isSaveMode, metadata, this);
        }
    }

    public void ShowRenamePrompt(int slotNumber, string currentName)
    {
        currentSlotToSave = slotNumber;
        nameInputField.text = currentName;
        renamePanel.SetActive(true);

        if (EventSystem.current != null)
        {
            lastSelectedSlotButton = EventSystem.current.currentSelectedGameObject;
            EventSystem.current.SetSelectedGameObject(nameInputField.gameObject);
        }
    }

    private void HideRenamePrompt()
    {
        renamePanel.SetActive(false);
        RestoreSlotButtonFocus();
    }

    private void RestoreSlotButtonFocus()
    {
        if (EventSystem.current != null && lastSelectedSlotButton != null)
        {
            EventSystem.current.SetSelectedGameObject(lastSelectedSlotButton);
            lastSelectedSlotButton = null;
        }
    }

    private void OnConfirmRename()
    {
        string savePrefix = "Save";
        if (LocalizationManager.Instance != null)
        {
            string locSave = LocalizationManager.Instance.GetLocalizedValue("ui_save");
            if (!string.IsNullOrEmpty(locSave) && locSave != "ui_save")
            {
                savePrefix = locSave;
            }
        }

        string newName = nameInputField.text;
        if (string.IsNullOrWhiteSpace(newName))
        {
            newName = savePrefix + " " + (currentSlotToSave + 1);
        }

        SaveManager.Instance.SaveGame(currentSlotToSave, newName, () =>
        {
            Refresh();
            StartCoroutine(RestoreFocusAfterRefresh(currentSlotToSave));
        });
        HideRenamePrompt();
    }

    private IEnumerator RestoreFocusAfterRefresh(int slotIndex)
    {
        yield return new WaitForEndOfFrame();
        if (EventSystem.current != null && contentArea != null && contentArea.childCount > slotIndex)
        {
            var slotBtn = contentArea.GetChild(slotIndex).GetComponentInChildren<Button>();
            if (slotBtn != null)
            {
                EventSystem.current.SetSelectedGameObject(slotBtn.gameObject);
            }
        }
    }

    private IEnumerator ShowPanelCoroutine(bool isSaving)
    {
        isSaveMode = isSaving;
        UpdateTitleText();
        PopulateSlots();

        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / animationDuration);
            float easedProgress = Easing.EaseOutQuad(progress);

            canvasGroup.alpha = easedProgress;
            transform.localScale = Vector3.one * Mathf.Lerp(startScale, 1f, easedProgress);

            yield return null;
        }

        canvasGroup.alpha = 1f;
        transform.localScale = Vector3.one;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        if (EventSystem.current != null)
        {
            if (contentArea != null && contentArea.childCount > 0)
            {
                var firstSlot = contentArea.GetChild(0).GetComponentInChildren<Button>();
                if (firstSlot != null)
                {
                    EventSystem.current.SetSelectedGameObject(firstSlot.gameObject);
                }
                else if (closeButton != null)
                {
                    EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
                }
            }
            else if (closeButton != null)
            {
                EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
            }
        }
    }

    private IEnumerator HidePanelCoroutine()
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / animationDuration);
            float easedProgress = Easing.EaseOutQuad(progress);

            canvasGroup.alpha = 1f - easedProgress;
            transform.localScale = Vector3.one * Mathf.Lerp(1f, startScale, easedProgress);

            yield return null;
        }

        canvasGroup.alpha = 0f;
        transform.localScale = Vector3.one * startScale;
    }

    void Update()
    {
        if (canvasGroup != null && canvasGroup.interactable)
        {
            UpdateScrollPositionToSelected(scrollRect, contentArea);
        }
    }

    private void UpdateScrollPositionToSelected(ScrollRect scrollRect, RectTransform contentArea)
    {
        if (scrollRect == null || contentArea == null || EventSystem.current == null) return;

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null || !selected.transform.IsChildOf(contentArea)) return;

        RectTransform selectedRect = selected.GetComponent<RectTransform>();
        if (selectedRect == null) return;

        Vector3 selectedPos = contentArea.InverseTransformPoint(selectedRect.position);
        float selectedY = selectedPos.y;
        float selectedHeight = selectedRect.rect.height;

        RectTransform viewportRect = scrollRect.viewport != null ? scrollRect.viewport : scrollRect.GetComponent<RectTransform>();
        float viewportHeight = viewportRect.rect.height;

        float contentHeight = contentArea.rect.height;
        if (contentHeight <= viewportHeight) return;

        float currentScrollY = contentArea.anchoredPosition.y;
        
        float topBoundary = -selectedY - (selectedHeight * 0.5f);
        float bottomBoundary = -selectedY + (selectedHeight * 0.5f);

        if (topBoundary < currentScrollY)
        {
            Vector2 pos = contentArea.anchoredPosition;
            pos.y = topBoundary;
            contentArea.anchoredPosition = pos;
        }
        else if (bottomBoundary > currentScrollY + viewportHeight)
        {
            Vector2 pos = contentArea.anchoredPosition;
            pos.y = bottomBoundary - viewportHeight;
            contentArea.anchoredPosition = pos;
        }
    }

    private void UpdateTitleText()
    {
        if (panelTitleText == null)
        {
            panelTitleText = GetComponentsInChildren<TextMeshProUGUI>(true)
                .FirstOrDefault(t => t.gameObject.name.ToLower().Contains("title"));
        }

        if (panelTitleText != null && LocalizationManager.Instance != null)
        {
            string titleKey = isSaveMode ? "ui_save_game" : "ui_load_game";
            string titleText = LocalizationManager.Instance.GetLocalizedValue(titleKey);
            if (!string.IsNullOrEmpty(titleText) && titleText != titleKey)
            {
                panelTitleText.text = titleText;
            }
            else
            {
                panelTitleText.text = isSaveMode ? "Save Game" : "Load Game";
            }
        }
    }

    private void AutoLocalizePanel()
    {
        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var txt in texts)
        {
            if (txt == null) continue;

            LocalizedText loc = txt.GetComponent<LocalizedText>();
            if (loc == null)
            {
                loc = txt.gameObject.AddComponent<LocalizedText>();
            }

            if (string.IsNullOrEmpty(loc.localizationKey))
            {
                string textVal = txt.text.Trim().ToLower();
                string objName = txt.gameObject.name.ToLower();

                if (textVal.Contains("close") || objName.Contains("close"))
                {
                    loc.localizationKey = "ui_close";
                }
                else if (textVal.Contains("confirm") || objName.Contains("confirm"))
                {
                    loc.localizationKey = "ui_confirm";
                }
                else if (textVal.Contains("cancel") || objName.Contains("cancel"))
                {
                    loc.localizationKey = "ui_cancel";
                }
                else if (textVal.Contains("rename") || objName.Contains("rename"))
                {
                    loc.localizationKey = "ui_rename";
                }
                else if (textVal.Contains("delete") || objName.Contains("delete"))
                {
                    loc.localizationKey = "ui_delete";
                }
            }

            loc.UpdateTextAndFont();
        }
    }
}