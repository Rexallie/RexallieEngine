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

        DialogueManager.Instance.LoadScriptFromFile("en", "chapter1_scene1");
        DialogueManager.Instance.AdvanceDialogue();

        StartCoroutine(TestCoroutine());
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

    private void DisplayDialogue(DialogueLine line)
    {
        // Look up the display name using the speakerID.
        string displayName = line.speakerID;
        if (characterNameLocalization.ContainsKey(line.speakerID.ToLower()))
        {
            displayName = characterNameLocalization[line.speakerID.ToLower()];
        }

        speakerNameText.text = displayName; // Set the nameplate text
        dialogueText.text = line.text;
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
