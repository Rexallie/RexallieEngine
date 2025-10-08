using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Collections; // Added for the coroutine

// ==================== UPDATED DATA STRUCTURES ====================

[System.Serializable]
public abstract class DialogueNode
{
    public string id;
    public int lineNumber; // NEW: Every node now knows its original line number.
}

[System.Serializable]
public class DialogueLine : DialogueNode
{
    public string speaker;
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

// ==================== CORRECTED PARSER ====================

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

        // First Pass: Find all labels and their line numbers.
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

        // Second Pass: Parse the script content.
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
                string dialogueText = line.Substring(colonIndex + 1).Trim();

                if (string.IsNullOrWhiteSpace(dialogueText) && i + 1 < lines.Length)
                {
                    string nextLine = lines[i + 1].Trim();
                    if (!nextLine.StartsWith("@") && !nextLine.Contains("->") && (!nextLine.EndsWith(":") || nextLine.Contains(" ")))
                    {
                        dialogueText = nextLine;
                        i++;
                    }
                }

                DialogueLine dialogue = ParseDialogue(line.Substring(0, colonIndex), dialogueText, i);
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
            speaker = speaker,
            portrait = portrait, // No longer defaulting here to allow null
            expression = expression, // No longer defaulting here to allow null
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

    // Events
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
        }
        else if (node is ChoiceNode choiceNode)
        {
            isWaitingOnChoice = true;
            OnChoicePresented?.Invoke(choiceNode.options);
        }
        else if (node is ActionNode actionNode)
        {
            OnActionExecuted?.Invoke(actionNode);

            // The jump action will change the node index, so we check if it was a jump
            bool wasJump = actionNode.action.ToLower() == "jump";

            if (!wasJump)
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
                AdvanceDialogue();
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
            isProcessingNode = false; // Unlock processing before advancing
            AdvanceDialogue();
        }
        else
        {
            Debug.LogError($"Label '{label}' not found in script '{currentScriptName}'!");
        }
    }

    // THIS IS THE CORRECTED LOGIC
    private int FindNodeIndexForLine(int targetLineIndex)
    {
        for (int i = 0; i < currentScript.nodes.Count; i++)
        {
            // Find the first node that was parsed on or after the label's line number.
            if (currentScript.nodes[i].lineNumber >= targetLineIndex)
            {
                return i;
            }
        }
        return currentScript.nodes.Count; // Jump to end if no nodes are after the label
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