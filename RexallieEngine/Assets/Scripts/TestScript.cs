using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public class TestScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField]
    private TextMeshProUGUI dialogueText;

    [SerializeField]
    private InputSystem_Actions inputActions;

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
        Debug.Log(dialogueText);
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
