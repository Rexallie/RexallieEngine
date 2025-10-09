using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using UnityEngine.UI; // Required for Coroutines

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private InputSystem_Actions _playerInput;

    [Header("Navigation Buttons")]
    [SerializeField] private Button backButton;

    [Header("Dialogue UI")]
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private DialogueAnimator dialogueAnimator;

    [Header("UI Animation Panels")]
    [Tooltip("Assign the main DialoguePanel GameObject here.")]
    [SerializeField] private CanvasGroup dialoguePanelCanvasGroup;
    [SerializeField] private RectTransform dialoguePanelRect;

    [Tooltip("Assign the SpeakerNamePanel GameObject here.")]
    [SerializeField] private CanvasGroup speakerNamePanelCanvasGroup;
    [SerializeField] private RectTransform speakerNamePanelRect;

    [Tooltip("Assign the QuickMenuPanel GameObject here.")]
    [SerializeField] private CanvasGroup quickMenuPanelCanvasGroup;
    [SerializeField] private RectTransform quickMenuPanelRect;

    private void Awake()
    {
        _playerInput = new InputSystem_Actions();

        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
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

        if (backButton != null)
        {
            backButton.onClick.AddListener(() => HistoryManager.Instance.Rollback());
        }

        DialogueManager.Instance.LoadScriptFromFile("en", "ui_test");
        DialogueManager.Instance.AdvanceDialogue();
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
            dialogueAnimator.ShowText(line.text);
        }
        else
        {
            dialogueText.text = line.text;
        }
    }

    private void HandleLanguageChange(TMP_FontAsset newFont)
    {
        if (speakerNameText != null) speakerNameText.font = newFont;
        if (dialogueText != null) dialogueText.font = newFont;
    }

    // --- NEW UI ANIMATION METHODS ---

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

    // Add this new public method inside your UIManager class
    public void ClearDialogueBox()
    {
        if (speakerNameText != null) speakerNameText.text = string.Empty;
        if (dialogueText != null) dialogueText.text = string.Empty;

        if (dialogueAnimator != null)
        {
            dialogueAnimator.Clear();
        }
    }

    private IEnumerator AnimateUIVisibility(bool show, float duration)
    {
        // Define animation targets
        float dialogueTargetY = -280f;
        float speakerTargetY = -118f;
        float offscreenYOffset = 300f;

        // Set start/end values based on whether we are showing or hiding
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

            // Animate Dialogue Panel
            dialoguePanelCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, easedProgress);
            dialoguePanelRect.anchoredPosition = new Vector2(dialoguePanelRect.anchoredPosition.x, Mathf.Lerp(dialogueStartY, dialogueEndY, easedProgress));
            dialoguePanelRect.localScale = Vector3.one * Mathf.Lerp(startScaleDialogue, endScaleDialogue, easedProgress);

            // Animate Speaker Name Panel
            speakerNamePanelCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, easedProgress);
            speakerNamePanelRect.anchoredPosition = new Vector2(speakerNamePanelRect.anchoredPosition.x, Mathf.Lerp(speakerStartY, speakerEndY, easedProgress));
            speakerNamePanelRect.localScale = Vector3.one * Mathf.Lerp(startScaleDialogue, endScaleDialogue, easedProgress);

            // Animate Quick Menu Buttons
            quickMenuPanelCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, easedProgress);
            quickMenuPanelRect.localScale = Vector3.one * Mathf.Lerp(startScaleButtons, endScaleButtons, easedProgress);

            yield return null;
        }

        // Snap to final values to ensure accuracy
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