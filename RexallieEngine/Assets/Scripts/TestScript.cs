using System.Collections;
using TMPro;
using UnityEngine;

public class TestScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField]
    private TextMeshProUGUI dialogueText;


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
            DialogueManager.Instance.AdvanceDialogue();
        }
    }


}
