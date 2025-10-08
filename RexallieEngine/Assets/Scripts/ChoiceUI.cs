using UnityEngine;
using UnityEngine.UI;
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
        foreach (var choice in choices)
        {
            GameObject buttonObj = Instantiate(choiceButtonPrefab, choiceContainer);

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
    }

    private void HideChoices()
    {
        choiceContainer.gameObject.SetActive(false);
    }
}