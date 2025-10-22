using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private InputSystem_Actions _playerInput;

    [Header("UI Content")]
    [Tooltip("Assign the parent GameObject that holds all UI elements here.")]
    [SerializeField] private GameObject uiContent;

    [Header("Dialogue UI")]
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private DialogueAnimator dialogueAnimator;

    [Header("UI Animation Panels")]
    [SerializeField] private CanvasGroup dialoguePanelCanvasGroup;
    [SerializeField] private RectTransform dialoguePanelRect;
    [SerializeField] private CanvasGroup speakerNamePanelCanvasGroup;
    [SerializeField] private RectTransform speakerNamePanelRect;
    [SerializeField] private CanvasGroup quickMenuPanelCanvasGroup;
    [SerializeField] private RectTransform quickMenuPanelRect;

    // --- NEW: References for the notification ---
    [Header("Notifications")]
    [SerializeField] private CanvasGroup notificationPanel;
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private float notificationDuration = 2f;
    [SerializeField] private float notificationFadeTime = 0.5f;

    [Header("History")]
    [SerializeField] private HistoryPanel historyPanel;

    [Header("Save & Load")]
    [SerializeField] private SaveLoadPanel saveLoadPanel;

    [Header("Navigation Buttons")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button historyButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button quickSaveButton; // <-- ADDED
    [SerializeField] private Button quickLoadButton; // <-- ADDED
    [SerializeField] private Button skipButton; // <-- ADD THIS
    [SerializeField] private Button autoButton;

    // --- NEW: For Auto Mode ---
    [Header("Auto Mode Settings")]
    [Tooltip("The delay in seconds after text finishes typing before auto-advancing.")]
    [SerializeField] private float autoAdvanceDelay = 2.0f;
    private float autoAdvanceTimer = 0f;


    private void Awake()
    {
        if (Instance == null) { Instance = this; } else { Destroy(gameObject); }
        _playerInput = new InputSystem_Actions();

        if (notificationPanel != null) notificationPanel.alpha = 0;
    }

    private void OnEnable()
    {
        _playerInput.UI.Enable();
        _playerInput.UI.Submit.performed += OnAdvanceDialogue;
    }

    private void OnDisable()
    {
        _playerInput.UI.Disable();
        _playerInput.UI.Submit.performed -= OnAdvanceDialogue;
    }

    void Start()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueLineDisplayed += DisplayDialogue;
        }

        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChange;
        }

        if (backButton != null && HistoryManager.Instance != null)
        {
            backButton.onClick.AddListener(HistoryManager.Instance.Rollback);
        }

        if (historyButton != null && historyPanel != null)
        {
            historyButton.onClick.AddListener(historyPanel.Show);
        }

        if (saveButton != null && saveLoadPanel != null)
        {
            saveButton.onClick.AddListener(() => saveLoadPanel.Show(true));
        }

        if (loadButton != null && saveLoadPanel != null)
        {
            loadButton.onClick.AddListener(() => saveLoadPanel.Show(false));
        }

        if (quickSaveButton != null && SaveManager.Instance != null)
        {
            quickSaveButton.onClick.AddListener(OnQuickSave);
        }

        if (quickLoadButton != null && SaveManager.Instance != null)
        {
            quickLoadButton.onClick.AddListener(() => SaveManager.Instance.LoadGame(SaveManager.QuickSaveSlot));
        }

        if (skipButton != null && DialogueManager.Instance != null)
        {
            skipButton.onClick.AddListener(ToggleSkipMode);
        }

        if (autoButton != null && DialogueManager.Instance != null)
        {
            autoButton.onClick.AddListener(ToggleAutoMode);
        }

        DialogueManager.Instance.LoadScriptFromFile("en", "ui_test");
        DialogueManager.Instance.AdvanceDialogue();
    }

    void Update()
    {
        if (DialogueManager.Instance.IsAutoMode)
        {
            CheckAutoAdvance();
        }
    }

    void OnDestroy()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueLineDisplayed -= DisplayDialogue;
        }
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChange;
        }
    }

    private void OnAdvanceDialogue(InputAction.CallbackContext context)
    {
        if (DialogueManager.Instance.IsAutoMode)
        {
            DialogueManager.Instance.IsAutoMode = false;
        }

        if (DialogueManager.Instance.IsSkipping)
        {
            DialogueManager.Instance.IsSkipping = false;
            return;
        }

        if (dialogueAnimator != null && dialogueAnimator.IsAnimating)
        {
            dialogueAnimator.FinishAnimation();
        }
        else
        {
            DialogueManager.Instance.AdvanceDialogue();
        }
    }

    private void DisplayDialogue(DialogueLine line)
    {
        string nameKey = $"character_{line.speakerID.ToLower()}_name";
        string displayName = LocalizationManager.Instance.GetLocalizedValue(nameKey);

        speakerNameText.text = displayName;

        if (dialogueAnimator != null)
        {
            // --- THIS IS THE KEY CHANGE ---
            // Check if we are skipping and pass that to the ShowText method.
            bool instant = DialogueManager.Instance.IsSkipping;
            dialogueAnimator.ShowText(line.text, instant);
        }
        else
        {
            dialogueText.text = line.text;
        }

        DialogueLogManager.Instance.AddLog(displayName, line.text);
    }

    // --- NEW: Method to toggle Auto mode ---
    private void ToggleAutoMode()
    {
        if (DialogueManager.Instance == null) return;

        // Toggle the state
        DialogueManager.Instance.IsAutoMode = !DialogueManager.Instance.IsAutoMode;

        if (DialogueManager.Instance.IsAutoMode)
        {
            // If we just turned Auto ON:
            // 1. Turn Skip OFF (they are mutually exclusive)
            DialogueManager.Instance.IsSkipping = false;

            // 2. Start the timer for the first advance
            autoAdvanceTimer = autoAdvanceDelay;
        }
    }

    // --- NEW: The logic that runs every frame during Auto mode ---
    private void CheckAutoAdvance()
    {
        // Don't do anything if the text is still typing
        if (dialogueAnimator != null && dialogueAnimator.IsAnimating)
        {
            autoAdvanceTimer = autoAdvanceDelay; // Reset the timer
            return;
        }

        // Don't do anything if the DialogueManager is waiting for a choice
        if (DialogueManager.Instance.IsWaitingForChoice())
        {
            DialogueManager.Instance.IsAutoMode = false; // Stop auto-mode at choices
            return;
        }

        // Count down the timer
        autoAdvanceTimer -= Time.deltaTime;
        if (autoAdvanceTimer <= 0)
        {
            // Timer is up! Advance the dialogue and reset the timer.
            DialogueManager.Instance.AdvanceDialogue();
            autoAdvanceTimer = autoAdvanceDelay;
        }
    }

    private void ToggleSkipMode()
    {
        if (DialogueManager.Instance == null) return;
        
        // Invert the skipping state
        DialogueManager.Instance.IsSkipping = !DialogueManager.Instance.IsSkipping;

        // --- THIS IS THE FIX ---
        // If we just turned skipping ON, call the DialogueManager directly to start the loop.
        // This avoids the OnAdvanceDialogue method which is meant to CANCEL the skip.
        if (DialogueManager.Instance.IsSkipping)
        {
            DialogueManager.Instance.IsAutoMode = false; // Turn Auto OFF
            DialogueManager.Instance.AdvanceDialogue();
        }
    }

    private void OnQuickSave()
    {
        SaveManager.Instance.SaveGame(SaveManager.QuickSaveSlot, "Quick Save", () => {
            StartCoroutine(ShowNotification("ui_quick_save_notification"));
        });
    }

    private IEnumerator ShowNotification(string localizationKey)
    {
        if (notificationPanel == null) yield break;

        notificationText.text = LocalizationManager.Instance.GetLocalizedValue(localizationKey);

        float timer = 0;
        while (timer < notificationFadeTime)
        {
            timer += Time.deltaTime;
            notificationPanel.alpha = Mathf.Lerp(0, 1, timer / notificationFadeTime);
            yield return null;
        }
        notificationPanel.alpha = 1;

        yield return new WaitForSeconds(notificationDuration);

        timer = 0;
        while (timer < notificationFadeTime)
        {
            timer += Time.deltaTime;
            notificationPanel.alpha = Mathf.Lerp(1, 0, timer / notificationFadeTime);
            yield return null;
        }
        notificationPanel.alpha = 0;
    }

    private void HandleLanguageChange(TMP_FontAsset newFont)
    {
        if (speakerNameText != null) speakerNameText.font = newFont;
        if (dialogueText != null) dialogueText.font = newFont;
    }

    public void ClearDialogueBox()
    {
        if (speakerNameText != null) speakerNameText.text = string.Empty;
        if (dialogueText != null) dialogueText.text = string.Empty;

        if (dialogueAnimator != null)
        {
            dialogueAnimator.Clear();
        }
    }

    // --- NEW METHOD for SCREENSHOTS ---
    public void SetUIActive(bool isActive)
    {
        if (uiContent != null)
        {
            uiContent.SetActive(isActive);
        }
    }

    public UISaveData GetState()
    {
        return new UISaveData
        {
            dialoguePanelVisible = dialoguePanelCanvasGroup.alpha > 0.5f,
            speakerNamePanelVisible = speakerNamePanelCanvasGroup.alpha > 0.5f,
            quickMenuPanelVisible = quickMenuPanelCanvasGroup.alpha > 0.5f
        };
    }

    public void RestoreState(UISaveData data)
    {
        dialoguePanelCanvasGroup.alpha = data.dialoguePanelVisible ? 1f : 0f;
        speakerNamePanelCanvasGroup.alpha = data.speakerNamePanelVisible ? 1f : 0f;
        quickMenuPanelCanvasGroup.alpha = data.quickMenuPanelVisible ? 1f : 0f;
    }

    public void ShowUI(float duration)
    {
        StopAllCoroutines();
        StartCoroutine(AnimateUIVisibility(true, duration));
    }

    public void HideUI(float duration)
    {
        StopAllCoroutines();
        StartCoroutine(AnimateUIVisibility(false, duration));
    }

    private IEnumerator AnimateUIVisibility(bool show, float duration)
    {
        float dialogueTargetY = -280f;
        float speakerTargetY = -118f;
        float offscreenYOffset = 300f;

        float dialogueStartY = show ? dialogueTargetY - offscreenYOffset : dialogueTargetY;
        float dialogueEndY = show ? dialogueTargetY : dialogueTargetY - offscreenYOffset;

        float speakerStartY = show ? speakerTargetY - offscreenYOffset : speakerTargetY;
        float speakerEndY = show ? speakerTargetY : speakerTargetY - offscreenYOffset;

        float startAlpha = show ? 0f : 1f;
        float endAlpha = show ? 1f : 0f;

        float startScaleDialogue = show ? 0.8f : 1f;
        float endScaleDialogue = show ? 1f : 0.8f;

        float startScaleButtons = show ? 0f : 1f;
        float endScaleButtons = show ? 1f : 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = Easing.EaseOutQuad(progress);

            dialoguePanelCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, easedProgress);
            dialoguePanelRect.anchoredPosition = new Vector2(dialoguePanelRect.anchoredPosition.x, Mathf.Lerp(dialogueStartY, dialogueEndY, easedProgress));
            dialoguePanelRect.localScale = Vector3.one * Mathf.Lerp(startScaleDialogue, endScaleDialogue, easedProgress);

            speakerNamePanelCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, easedProgress);
            speakerNamePanelRect.anchoredPosition = new Vector2(speakerNamePanelRect.anchoredPosition.x, Mathf.Lerp(speakerStartY, speakerEndY, easedProgress));
            speakerNamePanelRect.localScale = Vector3.one * Mathf.Lerp(startScaleDialogue, endScaleDialogue, easedProgress);

            quickMenuPanelCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, easedProgress);
            quickMenuPanelRect.localScale = Vector3.one * Mathf.Lerp(startScaleButtons, endScaleButtons, easedProgress);

            yield return null;
        }

        dialoguePanelCanvasGroup.alpha = endAlpha;
        dialoguePanelRect.anchoredPosition = new Vector2(dialoguePanelRect.anchoredPosition.x, dialogueEndY);
        dialoguePanelRect.localScale = Vector3.one * endScaleDialogue;

        speakerNamePanelCanvasGroup.alpha = endAlpha;
        speakerNamePanelRect.anchoredPosition = new Vector2(speakerNamePanelRect.anchoredPosition.x, speakerEndY);
        speakerNamePanelRect.localScale = Vector3.one * endScaleDialogue;

        quickMenuPanelCanvasGroup.alpha = endAlpha;
        quickMenuPanelRect.localScale = Vector3.one * endScaleButtons;
    }
}