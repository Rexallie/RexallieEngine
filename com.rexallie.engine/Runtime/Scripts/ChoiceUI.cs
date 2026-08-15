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

    private void HideChoicesOnLine(DialogueLine line)
    {
        HideChoices();
    }

    private List<Coroutine> activeFadeCoroutines = new List<Coroutine>();

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

        // Make the container visible.
        choiceContainer.gameObject.SetActive(true);

        List<GameObject> buttons = new List<GameObject>();

        // Create a new button for each choice.
        foreach (var choice in choices)
        {
            GameObject buttonObj = Instantiate(choiceButtonPrefab, choiceContainer);
            buttons.Add(buttonObj);

            // Configure CanvasGroup for fade-in
            CanvasGroup cg = buttonObj.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = buttonObj.AddComponent<CanvasGroup>();
            }
            cg.alpha = 0f;
            cg.interactable = false;

            // Set the button's text.
            buttonObj.GetComponentInChildren<TextMeshProUGUI>().text = choice.Text;

            // Add a listener to the button's click event.
            buttonObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                // When clicked, hide the choices and tell the DialogueManager which choice was made.
                HideChoices();
                DialogueManager.Instance.MakeChoice(choice.TargetLabel);
            });
        }

        // Run the staggered animations
        activeFadeCoroutines.Add(StartCoroutine(AnimateChoicesStaggered(buttons)));

        if (buttons.Count > 0 && EventSystem.current != null)
        {
            // Focus the first button for keyboard/gamepad navigation
            EventSystem.current.SetSelectedGameObject(buttons[0]);
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
        Vector3 startLocalPos = cg.transform.localPosition;

        while (elapsed < duration)
        {
            if (cg == null) yield break;
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = Easing.EaseOutQuad(progress);

            cg.alpha = eased;

            // Subtle slide up offset (15 units)
            float offset = Mathf.Lerp(-15f, 0f, eased);
            cg.transform.localPosition = new Vector3(startLocalPos.x, startLocalPos.y + offset, startLocalPos.z);

            yield return null;
        }

        if (cg != null)
        {
            cg.alpha = 1f;
            cg.transform.localPosition = startLocalPos;
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

        choiceContainer.gameObject.SetActive(false);
    }
}