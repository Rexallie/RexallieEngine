using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(LayoutElement))]
public class LogEntryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private LayoutElement layoutElement;

    [Header("Settings")]
    [Tooltip("Extra vertical padding to add to the calculated height.")]
    [SerializeField] private float verticalPadding = 10f;

    void Awake()
    {
        // Automatically find the LayoutElement if it's not assigned.
        if (layoutElement == null)
        {
            layoutElement = GetComponent<LayoutElement>();
        }
    }

    /// <summary>
    /// Populates the log entry and calculates its required height.
    /// </summary>
    public void SetData(string speaker, string dialogue)
    {
        speakerNameText.text = speaker;
        dialogueText.text = dialogue;

        // --- This is the core logic ---
        // Calculate the preferred height of the speaker name and the main dialogue text.
        float speakerHeight = speakerNameText.preferredHeight;
        float dialogueHeight = dialogueText.preferredHeight;

        // Set the preferred height of this entire UI element so the layout group can use it.
        layoutElement.preferredHeight = speakerHeight + dialogueHeight + verticalPadding;
    }
}