using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class ChoiceUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The parent object that will hold the choice buttons.")]
    public RectTransform choiceContainer;
    [Tooltip("The prefab for a single choice button.")]
    public GameObject choiceButtonPrefab;

    [Header("Button Sprites (Theme Override)")]
    [Tooltip("Optional normal sprite for choice buttons.")]
    public Sprite normalSprite;
    [Tooltip("Optional highlighted sprite for choice buttons.")]
    public Sprite highlightedSprite;

    private List<Button> spawnedButtons = new List<Button>();
    private float noActiveButtonTimer = 0f;
    private Button lastActiveButton = null;
    private List<Coroutine> activeFadeCoroutines = new List<Coroutine>();

    void Start()
    {
        // Subscribe to the DialogueManager's events.
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnChoicePresented += ShowChoices;
            DialogueManager.Instance.OnDialogueLineDisplayed += HideChoicesOnLine;
        }

        // Start with the choice container hidden.
        choiceContainer.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks.
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnChoicePresented -= ShowChoices;
            DialogueManager.Instance.OnDialogueLineDisplayed -= HideChoicesOnLine;
        }
    }

    private void Update()
    {
        if (choiceContainer.gameObject.activeInHierarchy && spawnedButtons.Count > 0)
        {
            UpdateChoiceButtonVisuals();
        }
    }

    private void HideChoicesOnLine(DialogueLine line)
    {
        HideChoices();
    }

    private void ShowChoices(List<ChoiceOption> choices)
    {
        // First, clear any old choice buttons and cancel active animations.
        foreach (var coroutine in activeFadeCoroutines)
        {
            if (coroutine != null) StopCoroutine(coroutine);
        }
        activeFadeCoroutines.Clear();

        foreach (Transform child in choiceContainer)
        {
            Destroy(child.gameObject);
        }
        spawnedButtons.Clear();

        // Make the container visible.
        choiceContainer.gameObject.SetActive(true);

        List<GameObject> buttons = new List<GameObject>();

        // Create a new button for each choice.
        foreach (var choice in choices)
        {
            GameObject buttonObj = Instantiate(choiceButtonPrefab, choiceContainer);
            buttons.Add(buttonObj);

            Button btnComponent = buttonObj.GetComponent<Button>();
            if (btnComponent != null)
            {
                btnComponent.navigation = new Navigation { mode = Navigation.Mode.Automatic };
                spawnedButtons.Add(btnComponent);
            }

            // Setup HorizontalLayoutGroup on the button to keep text padded and aligned
            HorizontalLayoutGroup buttonLayout = buttonObj.GetComponent<HorizontalLayoutGroup>();
            if (buttonLayout == null) buttonLayout = buttonObj.AddComponent<HorizontalLayoutGroup>();
            buttonLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonLayout.childControlWidth = true;
            buttonLayout.childControlHeight = true;
            buttonLayout.childForceExpandWidth = false;
            buttonLayout.childForceExpandHeight = false;
            buttonLayout.padding = new RectOffset(30, 30, 15, 15);

            // Ensure choice button expands dynamically horizontally to fit longer text
            ContentSizeFitter fitter = buttonObj.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = buttonObj.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            // Ensure the TextMeshPro child is configured correctly to not wrap, forcing horizontal expansion
            TextMeshProUGUI textMesh = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (textMesh != null)
            {
                textMesh.enableWordWrapping = false;
                textMesh.overflowMode = TextOverflowModes.Overflow;
            }

            // Ensure the parent container centers choice buttons horizontally and doesn't force widths
            VerticalLayoutGroup containerLayout = choiceContainer.GetComponent<VerticalLayoutGroup>();
            if (containerLayout != null)
            {
                containerLayout.childAlignment = TextAnchor.MiddleCenter;
                containerLayout.childControlWidth = false;
                containerLayout.childForceExpandWidth = false;
            }

            // Configure CanvasGroup for fade-in
            CanvasGroup cg = buttonObj.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = buttonObj.AddComponent<CanvasGroup>();
            }
            cg.alpha = 0f;
            cg.interactable = false;

            // Set the button's text.
            if (textMesh != null)
            {
                textMesh.text = choice.Text;
            }

            // Add a listener to the button's click event.
            if (btnComponent != null)
            {
                btnComponent.onClick.AddListener(() =>
                {
                    // When clicked, hide the choices and tell the DialogueManager which choice was made.
                    HideChoices();
                    DialogueManager.Instance.MakeChoice(choice.TargetLabel);
                });
            }
        }

        // Run the staggered animations
        activeFadeCoroutines.Add(StartCoroutine(AnimateChoicesStaggered(buttons)));

        if (buttons.Count > 0 && EventSystem.current != null)
        {
            if (UIManager.WasLastInputGamepadOrKeyboard)
            {
                // Focus the first button for keyboard/gamepad navigation
                EventSystem.current.SetSelectedGameObject(buttons[0]);
            }
            else
            {
                // Clear selection so no option is highlighted by default for mouse/touch inputs
                EventSystem.current.SetSelectedGameObject(null);
            }
        }
    }

    private IEnumerator AnimateChoicesStaggered(List<GameObject> buttons)
    {
        float fadeDuration = 0.3f;
        float staggerDelay = 0.12f;
        bool isSkipping = DialogueManager.Instance != null && DialogueManager.Instance.IsSkipping;

        if (isSkipping)
        {
            foreach (var btn in buttons)
            {
                if (btn != null)
                {
                    CanvasGroup cg = btn.GetComponent<CanvasGroup>();
                    if (cg != null)
                    {
                        cg.alpha = 1f;
                        cg.interactable = true;
                    }
                }
            }
            yield break;
        }

        // Wait one frame so the Layout Group can calculate correct positions
        yield return new WaitForEndOfFrame();

        // Iterate backwards (from bottom button to top button)
        for (int i = buttons.Count - 1; i >= 0; i--)
        {
            GameObject btn = buttons[i];
            if (btn == null) continue;

            CanvasGroup cg = btn.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                activeFadeCoroutines.Add(StartCoroutine(FadeInButton(cg, fadeDuration)));
                yield return new WaitForSeconds(staggerDelay);
            }
        }
    }

    private IEnumerator FadeInButton(CanvasGroup cg, float duration)
    {
        float elapsed = 0f;
        float startLocalY = cg.transform.localPosition.y;
        float startLocalZ = cg.transform.localPosition.z;

        while (elapsed < duration)
        {
            if (cg == null) yield break;
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = Easing.EaseOutQuad(progress);

            cg.alpha = eased;

            // Subtle slide up offset (15 units)
            float offset = Mathf.Lerp(-15f, 0f, eased);
            float currentX = cg.transform.localPosition.x;
            cg.transform.localPosition = new Vector3(currentX, startLocalY + offset, startLocalZ);

            yield return null;
        }

        if (cg != null)
        {
            cg.alpha = 1f;
            cg.interactable = true;
        }
    }

    private void HideChoices()
    {
        foreach (var coroutine in activeFadeCoroutines)
        {
            if (coroutine != null) StopCoroutine(coroutine);
        }
        activeFadeCoroutines.Clear();

        spawnedButtons.Clear();
        choiceContainer.gameObject.SetActive(false);
    }

    private Button GetCurrentlyHoveredOrSelectedButton()
    {
        foreach (Button btn in spawnedButtons)
        {
            if (btn == null) continue;

            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == btn.gameObject)
            {
                return btn;
            }

            if (IsMouseOverGameObject(btn.gameObject))
            {
                return btn;
            }
        }
        return null;
    }

    private bool IsMouseOverGameObject(GameObject obj)
    {
        if (EventSystem.current == null) return false;
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = UnityEngine.InputSystem.Mouse.current != null ? UnityEngine.InputSystem.Mouse.current.position.ReadValue() : Vector2.zero
        };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);
        foreach (var result in results)
        {
            if (result.gameObject == obj || result.gameObject.transform.IsChildOf(obj.transform))
            {
                return true;
            }
        }
        return false;
    }

    private CanvasGroup GetOrCreateCanvasGroup(Button btn)
    {
        CanvasGroup cg = btn.GetComponent<CanvasGroup>();
        if (cg == null) cg = btn.gameObject.AddComponent<CanvasGroup>();
        return cg;
    }

    private void UpdateChoiceButtonVisuals()
    {
        Button activeBtn = GetCurrentlyHoveredOrSelectedButton();

        if (activeBtn != null)
        {
            noActiveButtonTimer = 0f;
            lastActiveButton = activeBtn;
        }
        else
        {
            noActiveButtonTimer += Time.deltaTime;
            if (noActiveButtonTimer < 0.15f)
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

        foreach (Button btn in spawnedButtons)
        {
            if (btn == null) continue;

            CanvasGroup cg = GetOrCreateCanvasGroup(btn);
            if (cg == null) continue;

            if (!cg.interactable) continue;

            float targetAlpha = 1f;
            float targetScale = 1f;

            bool hasCustomSprites = (normalSprite != null && highlightedSprite != null);

            if (anyActive)
            {
                if (btn == activeBtn)
                {
                    targetAlpha = 1f;
                    targetScale = 1.05f;
                    if (hasCustomSprites && btn.image != null)
                    {
                        btn.image.sprite = highlightedSprite;
                    }
                }
                else
                {
                    targetAlpha = 0.4f;
                    targetScale = 0.85f;
                    if (hasCustomSprites && btn.image != null)
                    {
                        btn.image.sprite = normalSprite;
                    }
                }
            }
            else
            {
                targetAlpha = 1.0f;
                targetScale = 1f;
                if (hasCustomSprites && btn.image != null)
                {
                    btn.image.sprite = normalSprite;
                }
            }

            cg.alpha = Mathf.MoveTowards(cg.alpha, targetAlpha, Time.deltaTime * transitionSpeed);

            Vector3 currentScale = btn.transform.localScale;
            float newScale = Mathf.MoveTowards(currentScale.x, targetScale, Time.deltaTime * transitionSpeed * 0.8f);
            btn.transform.localScale = new Vector3(newScale, newScale, 1f);
        }
    }
}