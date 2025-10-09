using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public class UIManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField]
    private TextMeshProUGUI dialogueText;

    [SerializeField]
    private TextMeshProUGUI speakerNameText;

    [SerializeField]
    private InputSystem_Actions inputActions;

    // A simple dictionary for translating character IDs to display names.
    private Dictionary<string, string> characterNameLocalization = new Dictionary<string, string>
    {
        { "alice", "Alice" },
        // To add Japanese:
        // { "alice", "アリス" }, 
    };

    // At the top of the UIManager class, add a reference to our new animator.
    [Header("Dialogue Animation")]
    [SerializeField] private DialogueAnimator dialogueAnimator;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    void Start()
    {
        DialogueManager.Instance.OnDialogueLineDisplayed += DisplayDialogue;
        LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChange;

        DialogueManager.Instance.LoadScriptFromFile("en", "chapter1_scene1");
        DialogueManager.Instance.AdvanceDialogue();

        StartCoroutine(TestCoroutine());

        LocalizationManager.Instance.LoadLanguage("en");
        // In the same button click event, after loading the new text:
        // Replace the obsolete FindObjectOfType with FindFirstObjectByType
        Object.FindFirstObjectByType<UIManager>().BroadcastMessage("UpdateText", SendMessageOptions.DontRequireReceiver);
    }

    void OnDestroy()
    {
        // It's good practice to unsubscribe when the object is destroyed.
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueLineDisplayed -= DisplayDialogue;
        }
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChange; // <-- ADD THIS
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (inputActions.UI.QuickSave.WasPressedThisFrame())
        {
            SaveManager.Instance.SaveGame(0); // Save to slot 0
        }

        if (inputActions.UI.QuickLoad.WasPressedThisFrame())
        {
            SaveManager.Instance.LoadGame(0); // Save to slot 0
        }

        if (inputActions.UI.Submit.WasPressedThisFrame())
        {
            DialogueManager.Instance.AdvanceDialogue();
        }
    }

    // This is the new method that will handle the font change event.
    private void HandleLanguageChange(TMP_FontAsset newFont)
    {
        if (speakerNameText != null) speakerNameText.font = newFont;
        if (dialogueText != null) dialogueText.font = newFont;
    }

    private void DisplayDialogue(DialogueLine line)
    {
        string nameKey = $"character_{line.speakerID.ToLower()}_name";
        string displayName = LocalizationManager.Instance.GetLocalizedValue(nameKey);

        speakerNameText.text = displayName;

        // Instead of setting text directly, tell the animator to show it.
        if (dialogueAnimator != null)
        {
            dialogueAnimator.ShowText(line.text);
        }
        else
        {
            // Fallback to instant text if no animator is assigned.
            dialogueText.text = line.text;
        }
    }

    private IEnumerator TestCoroutine()
    {
        while (DialogueManager.Instance.IsDialogueActive())
        {
            yield return new WaitForSeconds(2f);
            //DialogueManager.Instance.AdvanceDialogue();
        }
    }


}
