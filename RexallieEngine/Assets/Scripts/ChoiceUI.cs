using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class ChoiceUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The parent object that will hold the choice buttons.")]
    public RectTransform choiceContainer;
    [Tooltip("The prefab for a single choice button.")]
    public GameObject choiceButtonPrefab;

    void Start()
    {
        // Subscribe to the DialogueManager's event for presenting choices.
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnChoicePresented += ShowChoices;
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
        }
    }

    private void ShowChoices(List<ChoiceOption> choices)
    {
        // First, clear any old choice buttons.
        foreach (Transform child in choiceContainer)
        {
            Destroy(child.gameObject);
        }

        // Make the container visible.
        choiceContainer.gameObject.SetActive(true);

        // Create a new button for each choice.
        GameObject firstButton = null;
        foreach (var choice in choices)
        {
            GameObject buttonObj = Instantiate(choiceButtonPrefab, choiceContainer);
            if (firstButton == null) firstButton = buttonObj;

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

        if (firstButton != null && EventSystem.current != null)
        {
            // Focus the first button for keyboard/gamepad navigation
            EventSystem.current.SetSelectedGameObject(firstButton);
        }
    }

    private void HideChoices()
    {
        choiceContainer.gameObject.SetActive(false);
    }
}