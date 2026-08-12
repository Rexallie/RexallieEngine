using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

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
    [SerializeField] private Button mainMenuButton;

    [Header("Preferences")]
    [SerializeField] private PreferencesPanel preferencesPanel;
    [SerializeField] private Button preferencesButton;

    [Header("Dialogue UI Opacity")]
    [SerializeField] private CanvasGroup dialogueMasterCanvasGroup; // Parent of DialoguePanel and SpeakerNamePanel

    [Header("Initial Script Settings")]
    [SerializeField] private string initialScriptName = "ui_test";

    // --- NEW: For Auto Mode ---
    [Header("Auto Mode Settings")]
    [Tooltip("The delay in seconds after text finishes typing before auto-advancing.")]
    [SerializeField] private float autoAdvanceDelay = 2.0f;
    private float autoAdvanceTimer = 0f;

    private bool isUIHidden = false;
    private GameObject lastSelectedObjectBeforePanelOpen = null;
    private bool isCtrlSkipping = false;
    private Vector2 touchStartPos;
    private float minSwipeDistance = 50f;
    private bool isTrackingTouch = false;
    private bool wasClicked = false;
    private float scrollCooldownTimer = 0f;
    private Vector2 lastMousePosition = Vector2.zero;


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
        _playerInput.UI.Cancel.performed += OnCancelPerformed;
        _playerInput.UI.QuickSave.performed += OnQuickSavePerformed;
        _playerInput.UI.QuickLoad.performed += OnQuickLoadPerformed;
    }

    private void OnDisable()
    {
        _playerInput.UI.Disable();
        _playerInput.UI.Submit.performed -= OnAdvanceDialogue;
        _playerInput.UI.Cancel.performed -= OnCancelPerformed;
        _playerInput.UI.QuickSave.performed -= OnQuickSavePerformed;
        _playerInput.UI.QuickLoad.performed -= OnQuickLoadPerformed;
    }

    void Start()
    {
        Button[] allButtons = new Button[] {
            backButton,
            historyButton,
            skipButton,
            autoButton,
            saveButton,
            loadButton,
            quickSaveButton,
            quickLoadButton,
            preferencesButton,
            mainMenuButton
        };
        foreach (Button btn in allButtons)
        {
            if (btn != null)
            {
                btn.navigation = new Navigation { mode = Navigation.Mode.None };
            }
        }

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
            historyButton.onClick.AddListener(() => ShowPanelWithFocus(historyPanel.Show));
        }

        if (saveButton != null && saveLoadPanel != null)
        {
            saveButton.onClick.AddListener(() => ShowPanelWithFocus(() => saveLoadPanel.Show(true)));
        }

        if (loadButton != null && saveLoadPanel != null)
        {
            loadButton.onClick.AddListener(() => ShowPanelWithFocus(() => saveLoadPanel.Show(false)));
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

        if (preferencesButton != null && preferencesPanel != null)
        {
            preferencesButton.onClick.AddListener(() => ShowPanelWithFocus(preferencesPanel.Show));
        }

        DialogueManager.Instance.LoadScriptFromFile("en", initialScriptName);
        DialogueManager.Instance.AdvanceDialogue();
    }

    void Update()
    {
        if (DialogueManager.Instance.IsAutoMode)
        {
            CheckAutoAdvance();
        }

        if (Mouse.current != null)
        {
            Vector2 currentMousePos = Mouse.current.position.ReadValue();
            if (Vector2.Distance(currentMousePos, lastMousePosition) > 2f)
            {
                lastMousePosition = currentMousePos;
                if (EventSystem.current != null && IsAnyMenuButtonSelected())
                {
                    EventSystem.current.SetSelectedGameObject(null);
                }
            }
        }

        CheckKeyboardShortcuts();
        CheckMouseShortcuts();
        HandleMenuNavigationInputs();
        UpdateMenuButtonVisuals();
        HandleHoldToSkip();
        HandleTouchGestures();
        HandleMouseScroll();

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            wasClicked = true;
        }

        if (wasClicked)
        {
            wasClicked = false;
            ProcessClick();
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
        if (IsAnyPanelActive()) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsWaitingForChoice()) return;

        if (IsAnyMenuButtonSelected())
        {
            return;
        }

        if (dialogueAnimator != null && dialogueAnimator.IsWaitingForInlineClick)
        {
            dialogueAnimator.IsWaitingForInlineClick = false;
            return;
        }

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

        // Interpolate variables before display
        string interpolatedText = InterpolateVariables(line.text);

        Debug.Log($"[UIManager] DisplayDialogue: Speaker={displayName} (ID={line.speakerID}), Text='{interpolatedText}'");

        speakerNameText.text = displayName;

        AudioClip voiceBlip = null;
        if (CharacterManager.Instance != null)
        {
            CharacterData charData = CharacterManager.Instance.GetCharacterData(line.speakerID);
            if (charData != null)
            {
                voiceBlip = charData.voiceBlip;
            }
        }

        if (dialogueAnimator != null)
        {
            // --- THIS IS THE KEY CHANGE ---
            // Check if we are skipping and pass that to the ShowText method.
            bool instant = DialogueManager.Instance.IsSkipping;
            Debug.Log($"[UIManager] Sending text to dialogueAnimator (instant={instant})");
            dialogueAnimator.ShowText(interpolatedText, voiceBlip, instant);
        }
        else
        {
            Debug.LogWarning("[UIManager] dialogueAnimator is null! Writing text directly to dialogueText field.");
            if (dialogueText != null)
            {
                dialogueText.text = interpolatedText;
            }
            else
            {
                Debug.LogError("[UIManager] dialogueText is ALSO null! Cannot display dialogue!");
            }
        }

        DialogueLogManager.Instance.AddLog(displayName, interpolatedText);
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

    public void SetAutoAdvanceDelay(float delay)
    {
        autoAdvanceDelay = delay;
    }

    public void SetDialogueOpacity(float opacity)
    {
        if (dialogueMasterCanvasGroup != null)
        {
            dialogueMasterCanvasGroup.alpha = opacity;
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
        InitializeFadeOverlay();
        return new UISaveData
        {
            dialoguePanelVisible = dialoguePanelCanvasGroup.alpha > 0.5f,
            speakerNamePanelVisible = speakerNamePanelCanvasGroup.alpha > 0.5f,
            quickMenuPanelVisible = quickMenuPanelCanvasGroup.alpha > 0.5f,
            screenFadeAlpha = fadeOverlayCanvasGroup != null ? fadeOverlayCanvasGroup.alpha : 0f,
            screenFadeColor = fadeOverlayImage != null ? fadeOverlayImage.color : Color.black
        };
    }

    public void RestoreState(UISaveData data)
    {
        InitializeFadeOverlay();
        dialoguePanelCanvasGroup.alpha = data.dialoguePanelVisible ? 1f : 0f;
        speakerNamePanelCanvasGroup.alpha = data.speakerNamePanelVisible ? 1f : 0f;
        quickMenuPanelCanvasGroup.alpha = data.quickMenuPanelVisible ? 1f : 0f;
        
        if (fadeOverlayCanvasGroup != null)
        {
            fadeOverlayCanvasGroup.alpha = data.screenFadeAlpha;
            fadeOverlayCanvasGroup.blocksRaycasts = data.screenFadeAlpha > 0.5f;
        }
        if (fadeOverlayImage != null)
        {
            fadeOverlayImage.color = data.screenFadeColor;
        }
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

    private void ShowPanelWithFocus(System.Action showAction)
    {
        if (EventSystem.current != null)
        {
            lastSelectedObjectBeforePanelOpen = EventSystem.current.currentSelectedGameObject;
        }
        showAction.Invoke();
    }

    private void RestoreFocusToMenu()
    {
        if (EventSystem.current != null)
        {
            if (lastSelectedObjectBeforePanelOpen != null)
            {
                EventSystem.current.SetSelectedGameObject(lastSelectedObjectBeforePanelOpen);
            }
            else if (preferencesButton != null)
            {
                EventSystem.current.SetSelectedGameObject(preferencesButton.gameObject);
            }
        }
    }

    private bool IsAnyPanelActive()
    {
        if (historyPanel != null && historyPanel.gameObject.activeSelf && historyPanel.GetComponent<CanvasGroup>().alpha > 0.5f) return true;
        if (saveLoadPanel != null && saveLoadPanel.gameObject.activeSelf && saveLoadPanel.GetComponent<CanvasGroup>().alpha > 0.5f) return true;
        if (preferencesPanel != null && preferencesPanel.gameObject.activeSelf && preferencesPanel.GetComponent<CanvasGroup>().alpha > 0.5f) return true;
        return false;
    }

    private void ToggleUIVisibility()
    {
        isUIHidden = !isUIHidden;
        if (isUIHidden)
        {
            HideUI(0.3f);
        }
        else
        {
            ShowUI(0.3f);
        }
    }


    private void ProcessClick()
    {
        if (isUIHidden)
        {
            ShowUI(0.3f);
            isUIHidden = false;
            return;
        }

        if (IsAnyPanelActive()) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsWaitingForChoice()) return;

        if (IsPointerOverInteractiveUI())
        {
            return;
        }

        if (EventSystem.current != null && IsAnyMenuButtonSelected())
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        if (dialogueAnimator != null && dialogueAnimator.IsWaitingForInlineClick)
        {
            dialogueAnimator.IsWaitingForInlineClick = false;
            return;
        }

        if (DialogueManager.Instance != null)
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
    }

    private void OnCancelPerformed(InputAction.CallbackContext context)
    {
        HandleCancelAction();
    }

    private void HandleCancelAction()
    {
        if (historyPanel != null && historyPanel.gameObject.activeSelf && historyPanel.GetComponent<CanvasGroup>().alpha > 0.5f)
        {
            historyPanel.Hide();
            RestoreFocusToMenu();
        }
        else if (saveLoadPanel != null && saveLoadPanel.gameObject.activeSelf && saveLoadPanel.GetComponent<CanvasGroup>().alpha > 0.5f)
        {
            saveLoadPanel.Hide();
            RestoreFocusToMenu();
        }
        else if (preferencesPanel != null && preferencesPanel.gameObject.activeSelf && preferencesPanel.GetComponent<CanvasGroup>().alpha > 0.5f)
        {
            preferencesPanel.Hide();
            RestoreFocusToMenu();
        }
        else if (isUIHidden)
        {
            ShowUI(0.3f);
            isUIHidden = false;
        }
        else
        {
            ToggleUIVisibility();
        }
    }

    private void OnQuickSavePerformed(InputAction.CallbackContext context)
    {
        if (IsAnyPanelActive()) return;
        OnQuickSave();
    }

    private void OnQuickLoadPerformed(InputAction.CallbackContext context)
    {
        if (IsAnyPanelActive()) return;
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.LoadGame(SaveManager.QuickSaveSlot);
        }
    }

    private void CheckKeyboardShortcuts()
    {
        if (Keyboard.current != null && !IsAnyPanelActive())
        {
            if (Keyboard.current.f5Key.wasPressedThisFrame)
            {
                OnQuickSave();
            }
            else if (Keyboard.current.f9Key.wasPressedThisFrame)
            {
                if (SaveManager.Instance != null)
                {
                    SaveManager.Instance.LoadGame(SaveManager.QuickSaveSlot);
                }
            }
            else if (Keyboard.current.f1Key.wasPressedThisFrame)
            {
                ToggleUIVisibility();
            }
        }
    }

    private void CheckMouseShortcuts()
    {
        if (Mouse.current != null)
        {
            if (Mouse.current.middleButton.wasPressedThisFrame && !IsAnyPanelActive())
            {
                ToggleUIVisibility();
            }

            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                if (IsAnyPanelActive())
                {
                    HandleCancelAction();
                }
                else
                {
                    if (saveLoadPanel != null)
                    {
                        ShowPanelWithFocus(() => saveLoadPanel.Show(true));
                    }
                }
            }
        }
    }

    private void HandleHoldToSkip()
    {
        if (DialogueManager.Instance == null || IsAnyPanelActive() || DialogueManager.Instance.IsWaitingForChoice())
        {
            if (isCtrlSkipping)
            {
                DialogueManager.Instance.IsSkipping = false;
                isCtrlSkipping = false;
            }
            return;
        }

        bool isKeyboardSkip = false;
        if (Keyboard.current != null)
        {
            isKeyboardSkip = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
        }

        bool isGamepadSkip = false;
        if (Gamepad.current != null)
        {
            isGamepadSkip = Gamepad.current.rightTrigger.isPressed || Gamepad.current.rightShoulder.isPressed;
        }

        bool shouldSkip = isKeyboardSkip || isGamepadSkip;

        if (shouldSkip)
        {
            if (!DialogueManager.Instance.IsSkipping)
            {
                DialogueManager.Instance.IsSkipping = true;
                DialogueManager.Instance.IsAutoMode = false;
                isCtrlSkipping = true;
                DialogueManager.Instance.AdvanceDialogue();
            }
        }
        else if (isCtrlSkipping)
        {
            DialogueManager.Instance.IsSkipping = false;
            isCtrlSkipping = false;
        }
    }

    private void HandleTouchGestures()
    {
        if (Touchscreen.current == null) return;

        var primaryTouch = Touchscreen.current.primaryTouch;
        if (primaryTouch.press.wasPressedThisFrame)
        {
            touchStartPos = primaryTouch.position.ReadValue();
            isTrackingTouch = true;
        }
        else if (primaryTouch.press.wasReleasedThisFrame && isTrackingTouch)
        {
            isTrackingTouch = false;
            Vector2 touchEndPos = primaryTouch.position.ReadValue();
            Vector2 swipeVector = touchEndPos - touchStartPos;

            if (swipeVector.magnitude >= minSwipeDistance)
            {
                Vector2 direction = swipeVector.normalized;
                
                // Swipe Down: Hide UI
                if (Vector2.Dot(direction, Vector2.down) > 0.7f)
                {
                    if (!isUIHidden && !IsAnyPanelActive())
                    {
                        ToggleUIVisibility();
                    }
                }
                // Swipe Up: Open History Panel
                else if (Vector2.Dot(direction, Vector2.up) > 0.7f)
                {
                    if (!isUIHidden && !IsAnyPanelActive() && historyPanel != null)
                    {
                        ShowPanelWithFocus(historyPanel.Show);
                    }
                }
                // Swipe Right: Toggle Auto Mode
                else if (Vector2.Dot(direction, Vector2.right) > 0.7f)
                {
                    if (!isUIHidden && !IsAnyPanelActive())
                    {
                        ToggleAutoMode();
                    }
                }
            }
            else
            {
                // Touch release without a swipe is a TAP
                wasClicked = true;
            }
        }
    }

    private bool IsPointerOverInteractiveUI()
    {
        if (EventSystem.current == null) return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        Vector2 pointerPos = Vector2.zero;
        if (Pointer.current != null)
        {
            pointerPos = Pointer.current.position.ReadValue();
        }
        else if (Mouse.current != null)
        {
            pointerPos = Mouse.current.position.ReadValue();
        }
        else
        {
            pointerPos = Input.mousePosition;
        }
        eventData.position = pointerPos;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            if (result.gameObject != null)
            {
                // Ignore click if it's over interactive selectables
                if (result.gameObject.GetComponentInParent<Selectable>() != null || 
                    result.gameObject.GetComponentInParent<TMP_InputField>() != null)
                {
                    return true;
                }

                // Ignore click if it's inside active menu panels
                if ((historyPanel != null && result.gameObject.transform.IsChildOf(historyPanel.transform)) ||
                    (saveLoadPanel != null && result.gameObject.transform.IsChildOf(saveLoadPanel.transform)) ||
                    (preferencesPanel != null && result.gameObject.transform.IsChildOf(preferencesPanel.transform)))
                {
                    return true;
                }

                // Ignore click if it's inside the quick menu panel
                if (quickMenuPanelRect != null && result.gameObject.transform.IsChildOf(quickMenuPanelRect))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void HandleMouseScroll()
    {
        if (scrollCooldownTimer > 0f)
        {
            scrollCooldownTimer -= Time.deltaTime;
            return;
        }

        if (Mouse.current == null) return;
        if (IsAnyPanelActive() || isUIHidden) return;

        float scrollY = Mouse.current.scroll.ReadValue().y;
        if (scrollY > 0.1f)
        {
            // Scroll Up: Rewind
            if (HistoryManager.Instance != null)
            {
                HistoryManager.Instance.Rollback();
                scrollCooldownTimer = 0.25f;
            }
        }
    }

    public string GetCurrentSpeakerName() => speakerNameText != null ? speakerNameText.text : "";
    public string GetCurrentDialogueText() => dialogueText != null ? dialogueText.text : "";

    private bool IsAnyMenuButtonSelected()
    {
        if (EventSystem.current == null) return false;
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null) return false;

        return selected == (backButton != null ? backButton.gameObject : null) ||
               selected == (historyButton != null ? historyButton.gameObject : null) ||
               selected == (saveButton != null ? saveButton.gameObject : null) ||
               selected == (loadButton != null ? loadButton.gameObject : null) ||
               selected == (quickSaveButton != null ? quickSaveButton.gameObject : null) ||
               selected == (quickLoadButton != null ? quickLoadButton.gameObject : null) ||
               selected == (autoButton != null ? autoButton.gameObject : null) ||
               selected == (skipButton != null ? skipButton.gameObject : null) ||
               selected == (preferencesButton != null ? preferencesButton.gameObject : null) ||
               selected == (mainMenuButton != null ? mainMenuButton.gameObject : null);
    }

    private void NavigateMenuButtons(int direction)
    {
        if (EventSystem.current == null) return;

        List<Button> activeButtons = new List<Button>();
        Button[] allButtons = new Button[] {
            backButton,
            historyButton,
            skipButton,
            autoButton,
            saveButton,
            loadButton,
            quickSaveButton,
            quickLoadButton,
            preferencesButton,
            mainMenuButton
        };

        foreach (Button btn in allButtons)
        {
            if (btn != null && btn.gameObject.activeInHierarchy && btn.interactable)
            {
                activeButtons.Add(btn);
            }
        }

        if (activeButtons.Count == 0) return;

        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;
        int currentIndex = -1;

        if (currentSelected != null)
        {
            for (int i = 0; i < activeButtons.Count; i++)
            {
                if (activeButtons[i].gameObject == currentSelected)
                {
                    currentIndex = i;
                    break;
                }
            }
        }

        if (currentIndex == -1)
        {
            // Nothing selected, select first or last depending on direction
            int targetIndex = direction > 0 ? 0 : activeButtons.Count - 1;
            EventSystem.current.SetSelectedGameObject(activeButtons[targetIndex].gameObject);
            Debug.Log($"[UIManager] Selected first active menu button: {activeButtons[targetIndex].gameObject.name}");
        }
        else
        {
            // Move index with wrap-around
            int targetIndex = currentIndex + direction;
            if (targetIndex < 0) targetIndex = activeButtons.Count - 1;
            if (targetIndex >= activeButtons.Count) targetIndex = 0;
            EventSystem.current.SetSelectedGameObject(activeButtons[targetIndex].gameObject);
            Debug.Log($"[UIManager] Navigated selection to: {activeButtons[targetIndex].gameObject.name}");
        }
    }

    private void HandleMenuNavigationInputs()
    {
        if (IsAnyPanelActive()) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsWaitingForChoice()) return;

        // Check Up arrow or gamepad D-pad Up to clear selection
        bool upPressed = false;
        if (Keyboard.current != null)
        {
            upPressed = Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame;
        }
        if (Gamepad.current != null)
        {
            upPressed = upPressed || Gamepad.current.dpad.up.wasPressedThisFrame;
        }

        if (upPressed)
        {
            if (EventSystem.current != null && IsAnyMenuButtonSelected())
            {
                EventSystem.current.SetSelectedGameObject(null);
                Debug.Log("[UIManager] Cleared menu button selection (pressed UP). Proceeding dialogue is re-enabled.");
            }
            return;
        }

        // Check Left/Right arrows or gamepad D-pad Left/Right
        bool leftPressed = false;
        bool rightPressed = false;

        if (Keyboard.current != null)
        {
            leftPressed = Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame;
            rightPressed = Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame;
        }
        if (Gamepad.current != null)
        {
            leftPressed = leftPressed || Gamepad.current.dpad.left.wasPressedThisFrame;
            rightPressed = rightPressed || Gamepad.current.dpad.right.wasPressedThisFrame;
        }

        if (leftPressed)
        {
            NavigateMenuButtons(-1);
        }
        else if (rightPressed)
        {
            NavigateMenuButtons(1);
        }
    }

    private Dictionary<Button, CanvasGroup> buttonCanvasGroups = new Dictionary<Button, CanvasGroup>();
    private float noActiveButtonTimer = 0f;
    private Button lastActiveButton = null;

    private CanvasGroup GetOrCreateCanvasGroup(Button btn)
    {
        if (btn == null) return null;
        if (buttonCanvasGroups.TryGetValue(btn, out CanvasGroup cg))
        {
            return cg;
        }

        cg = btn.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = btn.gameObject.AddComponent<CanvasGroup>();
        }
        buttonCanvasGroups[btn] = cg;
        return cg;
    }

    private Button GetCurrentlyHoveredOrSelectedButton()
    {
        if (EventSystem.current == null) return null;

        Button[] allButtons = new Button[] {
            backButton,
            historyButton,
            skipButton,
            autoButton,
            saveButton,
            loadButton,
            quickSaveButton,
            quickLoadButton,
            preferencesButton,
            mainMenuButton
        };

        // 1. Check selected GameObject (keyboard/gamepad navigation)
        GameObject selectedObj = EventSystem.current.currentSelectedGameObject;
        if (selectedObj != null)
        {
            foreach (Button btn in allButtons)
            {
                if (btn != null && btn.gameObject == selectedObj)
                {
                    return btn;
                }
            }
        }

        // 2. Check hovered GameObject (mouse pointer)
        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        Vector2 pointerPos = Vector2.zero;
        if (Pointer.current != null) pointerPos = Pointer.current.position.ReadValue();
        else if (Mouse.current != null) pointerPos = Mouse.current.position.ReadValue();
        else pointerPos = Input.mousePosition;
        
        pointerData.position = pointerPos;
        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        foreach (var result in raycastResults)
        {
            if (result.gameObject != null)
            {
                foreach (Button btn in allButtons)
                {
                    if (btn != null && result.gameObject.transform.IsChildOf(btn.transform))
                    {
                        return btn;
                    }
                }
            }
        }

        return null;
    }

    private void UpdateMenuButtonVisuals()
    {
        Button[] allButtons = new Button[] {
            backButton,
            historyButton,
            skipButton,
            autoButton,
            saveButton,
            loadButton,
            quickSaveButton,
            quickLoadButton,
            preferencesButton,
            mainMenuButton
        };

        Button activeBtn = GetCurrentlyHoveredOrSelectedButton();
        
        if (activeBtn != null)
        {
            noActiveButtonTimer = 0f;
            lastActiveButton = activeBtn;
        }
        else
        {
            noActiveButtonTimer += Time.deltaTime;
            if (noActiveButtonTimer < 0.15f) // 150ms hover-bridge to avoid flickers
            {
                activeBtn = lastActiveButton;
            }
            else
            {
                lastActiveButton = null;
            }
        }

        bool anyActive = (activeBtn != null);
        float transitionSpeed = 8f;

        foreach (Button btn in allButtons)
        {
            if (btn == null || !btn.gameObject.activeInHierarchy) continue;

            CanvasGroup cg = GetOrCreateCanvasGroup(btn);
            if (cg == null) continue;

            float targetAlpha = 1f;
            float targetScale = 1f;

            if (anyActive)
            {
                if (btn == activeBtn)
                {
                    targetAlpha = 1f;
                    targetScale = 1.05f;
                }
                else
                {
                    targetAlpha = 0.4f;
                    targetScale = 0.85f;
                }
            }

            // Smoothly transition alpha
            cg.alpha = Mathf.MoveTowards(cg.alpha, targetAlpha, Time.deltaTime * transitionSpeed);

            // Smoothly transition scale
            Vector3 currentScale = btn.transform.localScale;
            float newScale = Mathf.MoveTowards(currentScale.x, targetScale, Time.deltaTime * transitionSpeed * 0.8f);
            btn.transform.localScale = new Vector3(newScale, newScale, 1f);
        }
    }

    private CanvasGroup fadeOverlayCanvasGroup;
    private Image fadeOverlayImage;
    private Coroutine screenFadeCoroutine;

    private void InitializeFadeOverlay()
    {
        if (fadeOverlayCanvasGroup != null) return;

        Transform existing = transform.Find("VNS_FadeOverlay");
        GameObject overlayObj;
        if (existing != null)
        {
            overlayObj = existing.gameObject;
        }
        else
        {
            overlayObj = new GameObject("VNS_FadeOverlay");
            overlayObj.transform.SetParent(transform, false);
            
            RectTransform rect = overlayObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            Image img = overlayObj.AddComponent<Image>();
            img.color = Color.black;
            
            CanvasGroup cg = overlayObj.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            overlayObj.transform.SetAsLastSibling();
        }

        fadeOverlayCanvasGroup = overlayObj.GetComponent<CanvasGroup>();
        fadeOverlayImage = overlayObj.GetComponent<Image>();
    }

    public void FadeOutScreen(float duration, Color color, System.Action onComplete = null)
    {
        InitializeFadeOverlay();
        if (screenFadeCoroutine != null) StopCoroutine(screenFadeCoroutine);
        screenFadeCoroutine = StartCoroutine(ScreenFadeCoroutine(0f, 1f, duration, color, onComplete));
    }

    public void FadeInScreen(float duration, System.Action onComplete = null)
    {
        InitializeFadeOverlay();
        if (screenFadeCoroutine != null) StopCoroutine(screenFadeCoroutine);
        screenFadeCoroutine = StartCoroutine(ScreenFadeCoroutine(1f, 0f, duration, fadeOverlayImage.color, onComplete));
    }

    private IEnumerator ScreenFadeCoroutine(float startAlpha, float endAlpha, float duration, Color color, System.Action onComplete)
    {
        if (fadeOverlayImage != null)
        {
            fadeOverlayImage.color = color;
        }

        if (fadeOverlayCanvasGroup != null)
        {
            fadeOverlayCanvasGroup.blocksRaycasts = endAlpha > 0.5f;
        }

        if (duration <= 0.01f || (DialogueManager.Instance != null && DialogueManager.Instance.IsSkipping))
        {
            if (fadeOverlayCanvasGroup != null) fadeOverlayCanvasGroup.alpha = endAlpha;
            onComplete?.Invoke();
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            if (fadeOverlayCanvasGroup != null)
            {
                fadeOverlayCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, progress);
            }
            yield return null;
        }

        if (fadeOverlayCanvasGroup != null)
        {
            fadeOverlayCanvasGroup.alpha = endAlpha;
        }

        onComplete?.Invoke();
    }

    private string InterpolateVariables(string originalText)
    {
        if (string.IsNullOrEmpty(originalText)) return originalText;
        if (VariableManager.Instance == null) return originalText;

        int startIndex = 0;
        while (true)
        {
            int openBrace = originalText.IndexOf('{', startIndex);
            if (openBrace == -1) break;

            int closeBrace = originalText.IndexOf('}', openBrace);
            if (closeBrace == -1) break;

            string varName = originalText.Substring(openBrace + 1, closeBrace - openBrace - 1).Trim();
            
            if (varName.ToLower() == "w" || varName.ToLower().StartsWith("p="))
            {
                startIndex = closeBrace + 1;
                continue;
            }

            object val = VariableManager.Instance.GetVariableObject(varName);
            string valStr = val != null ? val.ToString() : $"[Variable '{varName}' not found]";

            originalText = originalText.Remove(openBrace, closeBrace - openBrace + 1).Insert(openBrace, valStr);
            startIndex = openBrace + valStr.Length;
        }

        return originalText;
    }
}