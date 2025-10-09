using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Collections;

// ==================== DATA STRUCTURES ====================

[System.Serializable]
public abstract class DialogueNode
{
    public string id;
    public int lineNumber;
}

[System.Serializable]
public class DialogueLine : DialogueNode
{
    public string speakerID;
    public string expression;
    public string text;
    public string portrait;
}

[System.Serializable]
public class ActionNode : DialogueNode
{
    public string action;
    public Dictionary<string, string> parameters;
}

[System.Serializable]
public class ChoiceNode : DialogueNode
{
    public List<ChoiceOption> options = new List<ChoiceOption>();
}

[System.Serializable]
public class ChoiceOption
{
    public string Text { get; set; }
    public string TargetLabel { get; set; }
}

[System.Serializable]
public class ScriptData
{
    public string sceneId;
    public List<DialogueNode> nodes;
    public Dictionary<string, int> labels;
}

// ==================== PARSER ====================

public class DialogueScriptParser
{
    private int nodeCounter = 0;

    public ScriptData ParseScript(string scriptText)
    {
        ScriptData data = new ScriptData
        {
            nodes = new List<DialogueNode>(),
            labels = new Dictionary<string, int>(),
            sceneId = "Untitled Scene"
        };
        nodeCounter = 0;

        string[] lines = scriptText.Split(new[] { '\r', '\n' }, StringSplitOptions.None);

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (line.EndsWith(":") && !line.Contains(" ") && !line.Contains("[") && !line.Contains("<"))
            {
                string label = line.Substring(0, line.Length - 1).Trim();
                if (!data.labels.ContainsKey(label))
                {
                    data.labels.Add(label, i);
                }
            }
        }

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#") || line.StartsWith("//") || (line.EndsWith(":") && !line.Contains(" ") && !line.Contains("[") && !line.Contains("<")))
                continue;

            if (line.StartsWith("@choice"))
            {
                ChoiceNode choiceNode = new ChoiceNode { id = $"node_{nodeCounter++:D3}", lineNumber = i };
                for (int j = i + 1; j < lines.Length; j++)
                {
                    string choiceLine = lines[j].Trim();
                    if (choiceLine.StartsWith("@endchoice"))
                    {
                        i = j;
                        break;
                    }
                    Match match = Regex.Match(choiceLine, "\"(.*?)\"\\s*->\\s*(\\w+)");
                    if (match.Success)
                    {
                        choiceNode.options.Add(new ChoiceOption
                        {
                            Text = match.Groups[1].Value,
                            TargetLabel = match.Groups[2].Value
                        });
                    }
                }
                data.nodes.Add(choiceNode);
                continue;
            }

            if (line.StartsWith("@"))
            {
                ActionNode action = ParseAction(line, i);
                if (action != null) data.nodes.Add(action);
                continue;
            }

            if (line.Contains(":"))
            {
                int colonIndex = line.LastIndexOf(':');
                string speakerPart = line.Substring(0, colonIndex);
                string firstLineText = line.Substring(colonIndex + 1).Trim();

                System.Text.StringBuilder dialogueBuilder = new System.Text.StringBuilder();
                if (!string.IsNullOrEmpty(firstLineText))
                {
                    dialogueBuilder.Append(firstLineText);
                }

                int lookaheadIndex = i + 1;
                while (lookaheadIndex < lines.Length)
                {
                    string nextLine = lines[lookaheadIndex].Trim();

                    bool isTerminator =
                        string.IsNullOrWhiteSpace(nextLine) ||
                        nextLine.StartsWith("@") ||
                        nextLine.Contains("->") ||
                        (nextLine.EndsWith(":") && !nextLine.Contains(" "));

                    if (isTerminator) break;

                    if (nextLine.Contains(":"))
                    {
                        string potentialSpeaker = nextLine.Substring(0, nextLine.IndexOf(':')).Trim();
                        if (potentialSpeaker.Length > 0 && !potentialSpeaker.Contains(" "))
                        {
                            break;
                        }
                    }

                    if (dialogueBuilder.Length > 0)
                    {
                        dialogueBuilder.Append("\n"); // Join with a newline character.
                    }
                    dialogueBuilder.Append(nextLine);
                    lookaheadIndex++;
                }

                i = lookaheadIndex - 1;

                DialogueLine dialogue = ParseDialogue(speakerPart, dialogueBuilder.ToString(), i);
                if (dialogue != null) data.nodes.Add(dialogue);
                continue;
            }
        }
        return data;
    }

    private ActionNode ParseAction(string line, int lineNumber)
    {
        ActionNode action = new ActionNode { id = $"node_{nodeCounter++:D3}", lineNumber = lineNumber, parameters = new Dictionary<string, string>() };
        string[] parts = line.Substring(1).Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return null;
        action.action = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            if (parts[i].Contains(":"))
            {
                string[] kvp = parts[i].Split(new[] { ':' }, 2);
                action.parameters[kvp[0]] = kvp[1];
            }
            else
            {
                action.parameters[$"param{i}"] = parts[i];
            }
        }
        return action;
    }

    private DialogueLine ParseDialogue(string speakerPart, string dialogueText, int lineNumber)
    {
        string portrait = null;
        string expression = null;

        Match portraitMatch = Regex.Match(speakerPart, @"<([^>]+)>");
        if (portraitMatch.Success)
        {
            portrait = portraitMatch.Groups[1].Value.Trim();
            speakerPart = speakerPart.Replace(portraitMatch.Value, "").Trim();
        }

        Match expressionMatch = Regex.Match(speakerPart, @"\[([^\]]+)\]");
        if (expressionMatch.Success)
        {
            expression = expressionMatch.Groups[1].Value.Trim();
            speakerPart = speakerPart.Replace(expressionMatch.Value, "").Trim();
        }

        string speaker = speakerPart.Trim();
        if (string.IsNullOrEmpty(speaker)) return null;

        return new DialogueLine
        {
            id = $"node_{nodeCounter++:D3}",
            lineNumber = lineNumber,
            speakerID = speaker,
            portrait = portrait,
            expression = expression,
            text = dialogueText
        };
    }
}

// ==================== DIALOGUE MANAGER ====================

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    private ScriptData currentScript;
    private int currentNodeIndex = 0;
    private DialogueScriptParser parser;
    private string currentScriptName;
    private bool isProcessingNode = false;
    private bool isWaitingOnChoice = false;

    public event Action<DialogueLine> OnDialogueLineDisplayed;
    public event Action<ActionNode> OnActionExecuted;
    public event Action OnDialogueEnded;
    public event Action<List<ChoiceOption>> OnChoicePresented;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            parser = new DialogueScriptParser();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadScriptFromFile(string language, string fileName)
    {
        currentScriptName = fileName;
        fileName = System.IO.Path.GetFileNameWithoutExtension(fileName);
        TextAsset scriptAsset = Resources.Load<TextAsset>($"Dialogues/{language}/{fileName}");
        if (scriptAsset != null)
        {
            currentScript = parser.ParseScript(scriptAsset.text);
            currentNodeIndex = 0;
            isWaitingOnChoice = false;
        }
        else
        {
            Debug.LogError($"Could not find script: Resources/Dialogues/{language}/{fileName}");
        }
    }

    public void AdvanceDialogue()
    {
        if (isProcessingNode || isWaitingOnChoice) return;
        StartCoroutine(ProcessCurrentNode());
    }

    private IEnumerator ProcessCurrentNode()
    {
        isProcessingNode = true;
        if (currentScript == null || currentNodeIndex >= currentScript.nodes.Count)
        {
            OnDialogueEnded?.Invoke();
            isProcessingNode = false;
            yield break;
        }

        DialogueNode node = currentScript.nodes[currentNodeIndex];
        currentNodeIndex++;

        if (node is DialogueLine dialogueLine)
        {
            OnDialogueLineDisplayed?.Invoke(dialogueLine);

            // NEW: Check if the next node is a choice.
            if (currentNodeIndex < currentScript.nodes.Count && currentScript.nodes[currentNodeIndex] is ChoiceNode)
            {
                // If it is, unlock and immediately advance to show the choices.
                isProcessingNode = false;
                AdvanceDialogue();
                yield break; // End this processing step.
            }
        }
        else if (node is ChoiceNode choiceNode)
        {
            isWaitingOnChoice = true;
            OnChoicePresented?.Invoke(choiceNode.options);
        }
        else if (node is ActionNode actionNode)
        {
            OnActionExecuted?.Invoke(actionNode);

            string actionType = actionNode.action.ToLower();
            bool isFlowControlAction = (actionType == "jump" || actionType == "if");

            if (isFlowControlAction)
            {
                // Flow control actions handle their own advancement.
                // Do nothing here and let the coroutine end.
            }
            else // For all other actions (wait, showCharacter, etc.)
            {
                yield return null;
                if (ActionExecutor.Instance != null)
                {
                    while (ActionExecutor.Instance.IsExecutingAction())
                    {
                        yield return null;
                    }
                }
                isProcessingNode = false;
                AdvanceDialogue(); // Automatically advance to the next node.
                yield break;
            }
        }
        isProcessingNode = false;
    }

    public void MakeChoice(string targetLabel)
    {
        isWaitingOnChoice = false;
        JumpToLabel(targetLabel);
    }

    public void JumpToLabel(string label)
    {
        if (currentScript.labels.TryGetValue(label, out int lineIndex))
        {
            currentNodeIndex = FindNodeIndexForLine(lineIndex);
            isProcessingNode = false;
            AdvanceDialogue();
        }
        else
        {
            Debug.LogError($"Label '{label}' not found in script '{currentScriptName}'!");
        }
    }

    private int FindNodeIndexForLine(int targetLineIndex)
    {
        for (int i = 0; i < currentScript.nodes.Count; i++)
        {
            if (currentScript.nodes[i].lineNumber >= targetLineIndex)
            {
                return i;
            }
        }
        return currentScript.nodes.Count;
    }

    public string GetCurrentScriptName() { return currentScriptName; }
    public int GetCurrentNodeIndex() { return Mathf.Max(0, currentNodeIndex - 1); }
    public void RestoreState(string scriptName, int nodeIndex)
    {
        LoadScriptFromFile("en", scriptName);
        currentNodeIndex = nodeIndex;
        AdvanceDialogue();
    }

    public bool IsDialogueActive()
    {
        return currentScript != null && currentNodeIndex < currentScript.nodes.Count;
    }
}