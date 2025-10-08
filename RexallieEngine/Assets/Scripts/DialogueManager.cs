using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Collections;

// ==================== DATA STRUCTURES ====================

[Serializable]
public abstract class DialogueNode
{
    public string id;
}

[Serializable]
public class DialogueLine : DialogueNode
{
    public string speaker;
    public string expression;
    public string text;
    public string portrait;
}

[Serializable]
public class ActionNode : DialogueNode
{
    public string action;
    public Dictionary<string, string> parameters;
}

[Serializable]
public class ScriptData
{
    public string sceneId;
    public List<DialogueNode> nodes;
}

// ==================== PARSER ====================

public class DialogueScriptParser
{
    [SerializeField]
    private bool debugLogs = false;

    private int nodeCounter = 0;

    public ScriptData ParseScript(string scriptText)
    {
        ScriptData data = new ScriptData();
        data.nodes = new List<DialogueNode>();
        data.sceneId = "Untitled Scene"; // Default scene ID
        nodeCounter = 0;

        string[] lines = scriptText.Split('\n');

        if (debugLogs)
            Debug.Log($"Parsing script with {lines.Length} lines");

        bool foundSceneHeader = false;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            // Skip empty lines
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Skip comments (lines that start with # but aren't the first scene header)
            if (line.StartsWith("#"))
            {
                // Only treat the first # line as scene header if it's substantial
                if (!foundSceneHeader && line.Length > 2)
                {
                    string potentialSceneId = line.Substring(1).Trim();
                    // Check if it looks like a scene header (not a comment)
                    if (!potentialSceneId.StartsWith("#") && potentialSceneId.Split(' ').Length <= 10)
                    {
                        data.sceneId = potentialSceneId;
                        foundSceneHeader = true;

                        if (debugLogs)
                            Debug.Log($"Found scene header: {data.sceneId}");
                    }
                }
                continue;
            }

            // Skip line comments
            if (line.StartsWith("//"))
                continue;

            // Parse action commands
            if (line.StartsWith("@"))
            {
                ActionNode action = ParseAction(line);
                if (action != null)
                {
                    data.nodes.Add(action);

                    if (debugLogs)
                        Debug.Log($"Parsed action: {action.action}");
                }
                continue;
            }

            // Parse dialogue lines (speaker line followed by text on next line)
            if (line.Contains(":"))
            {
                // Check if there's text after the colon on the same line
                int colonIndex = line.LastIndexOf(':');
                string afterColon = line.Substring(colonIndex + 1).Trim();

                string dialogueText = afterColon;

                // If there's no text after colon, get it from the next line
                if (string.IsNullOrWhiteSpace(afterColon) && i + 1 < lines.Length)
                {
                    dialogueText = lines[i + 1].Trim();
                    i++; // Skip the next line since we've consumed it
                }

                // Only parse if we have actual dialogue text
                if (!string.IsNullOrWhiteSpace(dialogueText))
                {
                    DialogueLine dialogue = ParseDialogue(line, dialogueText);
                    if (dialogue != null)
                    {
                        data.nodes.Add(dialogue);

                        if (debugLogs)
                            Debug.Log($"Parsed dialogue: {dialogue.speaker} - {dialogue.text}");
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to parse dialogue line: {line}");
                    }
                }
                continue;
            }
        }

        if (debugLogs)
            Debug.Log($"Parsing complete. Total nodes: {data.nodes.Count}");

        return data;
    }

    private ActionNode ParseAction(string line)
    {
        // Remove @ symbol
        line = line.Substring(1).Trim();

        ActionNode action = new ActionNode();
        action.id = $"node_{nodeCounter++:D3}";
        action.parameters = new Dictionary<string, string>();

        // Split into parts
        string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
            return null;

        action.action = parts[0];

        // Parse parameters
        for (int i = 1; i < parts.Length; i++)
        {
            string part = parts[i];

            // Check if it's a key:value parameter
            if (part.Contains(":"))
            {
                string[] keyValue = part.Split(':');
                action.parameters[keyValue[0]] = keyValue[1];
            }
            else
            {
                // Positional parameters get generic names
                action.parameters[$"param{i}"] = part;
            }
        }

        return action;
    }

    private DialogueLine ParseDialogue(string line, string dialogueText)
    {
        // Pattern: SpeakerName <portrait> [expression]: (text on same line or next line)
        // Or: SpeakerName [expression]:
        // Or: SpeakerName <portrait>:
        // Or: SpeakerName:

        // Remove the colon and everything after it from the speaker line
        int colonIndex = line.LastIndexOf(':');
        if (colonIndex == -1)
            return null;

        string speakerPart = line.Substring(0, colonIndex).Trim();

        // Try to extract portrait and expression
        string portrait = null;
        string expression = null;
        string speaker = speakerPart;

        // Check for portrait: <...>
        Regex portraitRegex = new Regex(@"<([^>]+)>");
        Match portraitMatch = portraitRegex.Match(speakerPart);
        if (portraitMatch.Success)
        {
            portrait = portraitMatch.Groups[1].Value.Trim();
            speakerPart = portraitRegex.Replace(speakerPart, "").Trim();
        }

        // Check for expression: [...]
        Regex expressionRegex = new Regex(@"\[([^\]]+)\]");
        Match expressionMatch = expressionRegex.Match(speakerPart);
        if (expressionMatch.Success)
        {
            expression = expressionMatch.Groups[1].Value.Trim();
            speakerPart = expressionRegex.Replace(speakerPart, "").Trim();
        }

        // What's left is the speaker name
        speaker = speakerPart.Trim();

        if (string.IsNullOrWhiteSpace(speaker))
            return null;

        DialogueLine dialogue = new DialogueLine();
        dialogue.id = $"node_{nodeCounter++:D3}";
        dialogue.speaker = speaker;
        dialogue.portrait = portrait ?? $"{speaker.ToLower()}_base";
        dialogue.expression = expression ?? "neutral";
        dialogue.text = dialogueText;

        return dialogue;
    }
}

// ==================== DIALOGUE MANAGER ====================

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    private ScriptData currentScript;
    private int currentNodeIndex = 0;
    private DialogueScriptParser parser;

    // Events for UI updates
    public event Action<DialogueLine> OnDialogueLineDisplayed;
    public event Action<ActionNode> OnActionExecuted;
    public event Action OnDialogueEnded;

    private bool isProcessingNode = false; // To prevent concurrent advances

    private string currentScriptName;

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

    public void LoadScript(string scriptText)
    {
        currentScript = parser.ParseScript(scriptText);
        currentNodeIndex = 0;

        if (currentScript == null || currentScript.nodes.Count == 0)
        {
            Debug.LogError("Failed to load script or script is empty.");
            return;
        }

        Debug.Log($"Loaded script: {currentScript.sceneId} with {currentScript.nodes.Count} nodes");
    }

    public void LoadScriptFromFile(string language, string fileName)
    {
        // Remove any extension if provided - Resources.Load doesn't use extensions
        fileName = System.IO.Path.GetFileNameWithoutExtension(fileName);

        TextAsset scriptAsset = Resources.Load<TextAsset>($"Dialogues/{language}/{fileName}");

        currentScriptName = fileName; // Store the name for saving

        if (scriptAsset != null)
        {
            LoadScript(scriptAsset.text);
        }
        else
        {
            Debug.LogError($"Could not find script: Resources/Dialogues/{language}/{fileName}.vns\n" +
                          "Make sure the file is in the Resources folder and Unity has imported it.");
        }
    }

    public void AdvanceDialogue()
    {
        // Prevent starting a new process if one is already running
        if (isProcessingNode) return;

        StartCoroutine(ProcessCurrentNode());
    }

    private IEnumerator ProcessCurrentNode()
    {
        isProcessingNode = true;

        if (currentScript == null || currentNodeIndex >= currentScript.nodes.Count)
        {
            OnDialogueEnded?.Invoke();
            isProcessingNode = false;
            yield break; // Exit the coroutine
        }

        DialogueNode node = currentScript.nodes[currentNodeIndex];
        currentNodeIndex++;

        if (node is DialogueLine dialogueLine)
        {
            OnDialogueLineDisplayed?.Invoke(dialogueLine);
            // For dialogue lines, we stop and wait for the next user input.
        }
        else if (node is ActionNode actionNode)
        {
            OnActionExecuted?.Invoke(actionNode);

            // Wait a frame to ensure the action has started
            yield return null;

            // Now, wait for the action to complete if it's a "waiting" one.
            if (ActionExecutor.Instance != null)
            {
                while (ActionExecutor.Instance.IsExecutingAction())
                {
                    yield return null;
                }
            }

            // Once the action is done, immediately process the next node.
            isProcessingNode = false; // Unlock for the next call
            AdvanceDialogue();
            yield break;
        }

        isProcessingNode = false; // Unlock after processing
    }

    /*
    private void ExecuteAction(ActionNode action)
    {
        OnActionExecuted?.Invoke(action);

        // You can handle actions here or let other systems subscribe to the event

        //Debug.Log($"Executing action: {action.action}");
        foreach (var param in action.parameters)
        {
            //Debug.Log($"  {param.Key}: {param.Value}");
        }
    }*/

    public bool IsDialogueActive()
    {
        return currentScript != null && currentNodeIndex < currentScript.nodes.Count;
    }

    // Add these new methods to the end of the class
    public string GetCurrentScriptName()
    {
        return currentScriptName;
    }

    public int GetCurrentNodeIndex()
    {
        // We subtract 1 because AdvanceDialogue increments before processing.
        // This ensures we save the node we are currently looking at.
        return Mathf.Max(0, currentNodeIndex - 1);
    }

    public void RestoreState(string scriptName, int nodeIndex)
    {
        // The language is hardcoded to "en" for now, you might want to save this too!
        LoadScriptFromFile("en", scriptName);
        currentNodeIndex = nodeIndex;
        AdvanceDialogue(); // Display the loaded line
    }
}