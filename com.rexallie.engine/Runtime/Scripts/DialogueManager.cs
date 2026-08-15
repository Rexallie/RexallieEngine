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

        string[] lines = scriptText.Split('\n');

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

    [Header("Skip Settings")]
    [Tooltip("The minimum time a line is visible when skipping (in seconds).")]
    [SerializeField] private float skipDelay = 0.1f;

    private ScriptData currentScript;
    private int currentNodeIndex = 0;
    private DialogueScriptParser parser;
    private string currentScriptName;
    private bool isProcessingNode = false;
    private bool isWaitingOnChoice = false;
    // --- THIS IS THE KEY CHANGE ---
    // The IsSkipping property is now public so the UIManager can control it.
    // The local 'readDialogueIDs' set has been removed.
    public bool IsAutoMode { get; set; } = false;
    public bool IsSkipping { get; set; } = false;

    public event Action<DialogueLine> OnDialogueLineDisplayed;
    public event Action<ActionNode> OnActionExecuted;
    public event Action OnDialogueEnded;
    public event Action<List<ChoiceOption>> OnChoicePresented;
    public event Action<string, string[]> OnVNSTrigger;

    public void TriggerVNSEvent(string eventName, string[] parameters)
    {
        Debug.Log($"[DialogueManager] TriggerVNSEvent: {eventName} with {parameters.Length} arguments.");
        OnVNSTrigger?.Invoke(eventName, parameters);
    }

    [System.Serializable]
    public class ReplaySceneInfo
    {
        public string cgId;
        public string scriptName;
        public int startNodeIndex;
        public int endNodeIndex;
    }

    private Dictionary<string, ReplaySceneInfo> replayRegistry = new Dictionary<string, ReplaySceneInfo>();
    private bool isReplaying = false;
    private int activeReplayEndIndex = -1;

    public bool IsReplaying => isReplaying;
    public event Action OnReplayEnded;

    // --- NEW: This flag prevents recording history during a restore operation ---
    private bool isRestoringState = false;
    private Coroutine currentNodeCoroutine;
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

    void Start()
    {
        // --- THIS IS A KEY CHANGE ---
        // We now subscribe a new method to this event to handle history recording.
        OnDialogueLineDisplayed += OnDialogueLineWasDisplayed;

        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChange;
        }

        string lang = PlayerPrefs.GetString("language", "en");
        InitializeReplayRegistry(lang);
    }

    void OnDestroy()
    {
        OnDialogueLineDisplayed -= OnDialogueLineWasDisplayed;

        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChange;
        }
    }

    private void HandleLanguageChange(TMPro.TMP_FontAsset font)
    {
        string currentLanguage = PlayerPrefs.GetString("language", "en");
        InitializeReplayRegistry(currentLanguage);
    }

    // --- THIS IS THE NEW METHOD ---
    // It is called automatically every time a dialogue line is shown to the player.
    private void OnDialogueLineWasDisplayed(DialogueLine line)
    {
        // --- THIS IS THE KEY CHANGE ---
        // Instead of tracking locally, we tell the PersistentDataManager to mark the line as read.
        string lineID = $"{currentScriptName}_{line.lineNumber}";
        PersistentDataManager.Instance.MarkLineAsRead(lineID);

        if (!isRestoringState)
        {
            HistoryManager.Instance.RecordState();
        }
    }

    public void LoadScriptFromFile(string language, string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            Debug.LogError("LoadScriptFromFile called with null or empty script name.");
            return;
        }
        currentScriptName = fileName;
        fileName = System.IO.Path.GetFileNameWithoutExtension(fileName);
        string loadPath = $"Dialogues/{language}/{fileName}";
        TextAsset scriptAsset = Resources.Load<TextAsset>(loadPath);
        if (scriptAsset != null)
        {
            currentScript = parser.ParseScript(scriptAsset.text);
            currentNodeIndex = 0;
            isWaitingOnChoice = false;
            Debug.Log($"Successfully loaded dialogue script: '{loadPath}' with {currentScript.nodes.Count} nodes.");
        }
        else
        {
            Debug.LogError($"Could not find script asset at: Resources/Dialogues/{language}/{fileName}. Please ensure you have imported the Demo Showcase samples or placed your script in the Resources folder.");
        }
    }

    public void AdvanceDialogue()
    {
        if (currentScript == null)
        {
            Debug.LogWarning("AdvanceDialogue called but no script is loaded. Please load a script first.");
            return;
        }
        if (isProcessingNode || isWaitingOnChoice) return;

        if (IsSkipping)
        {
            if (currentNodeIndex >= currentScript.nodes.Count)
            {
                IsSkipping = false; // End of script, stop skipping
            }
            else
            {
                DialogueNode nextNode = currentScript.nodes[currentNodeIndex];
                if (nextNode is ChoiceNode)
                {
                    IsSkipping = false; // Stop skipping if we hit a choice
                }
                else if (nextNode is DialogueLine line)
                {
                    if (SettingsManager.Instance.currentSettings.skipUnreadText)
                    {
                        string lineID = $"{currentScriptName}_{line.lineNumber}";
                        if (PersistentDataManager.Instance != null && !PersistentDataManager.Instance.IsLineRead(lineID))
                        {
                            IsSkipping = false;
                        }
                    }
                }
            }
        }

        if (currentNodeCoroutine != null) StopCoroutine(currentNodeCoroutine);
        currentNodeCoroutine = StartCoroutine(ProcessCurrentNode());
    }

    private IEnumerator ProcessCurrentNode()
    {
        isProcessingNode = true;

        // Check if replay has run past its end marker
        if (isReplaying && activeReplayEndIndex != -1 && currentNodeIndex > activeReplayEndIndex)
        {
            EndReplay();
            yield break;
        }

        if (currentScript == null || currentNodeIndex >= currentScript.nodes.Count)
        {
            if (isReplaying)
            {
                EndReplay();
            }
            else
            {
                OnDialogueEnded?.Invoke();
                isProcessingNode = false;
            }
            yield break;
        }

        DialogueNode node = currentScript.nodes[currentNodeIndex];
        Debug.Log($"[DialogueManager] ProcessCurrentNode: Index {currentNodeIndex}, Node Type: {node.GetType().Name}");
        currentNodeIndex++;

        if (node is DialogueLine dialogueLine)
        {
            Debug.Log($"[DialogueManager] Displaying Dialogue Line: {dialogueLine.speakerID} - '{dialogueLine.text}'");
            OnDialogueLineDisplayed?.Invoke(dialogueLine);

            // NEW: Check if the next node is a choice.
            if (currentNodeIndex < currentScript.nodes.Count && currentScript.nodes[currentNodeIndex] is ChoiceNode)
            {
                // If it is, unlock and immediately advance to show the choices.
                isProcessingNode = false;
                if (isRestoringState) isRestoringState = false;
                AdvanceDialogue();
                yield break; // End this processing step.
            }
        }
        else if (node is ChoiceNode choiceNode)
        {
            Debug.Log($"[DialogueManager] Presenting Choice Node Options Count: {choiceNode.options.Count}");
            isWaitingOnChoice = true;
            OnChoicePresented?.Invoke(choiceNode.options);
            if (isRestoringState) isRestoringState = false;
        }
        else if (node is ActionNode actionNode)
        {
            Debug.Log($"[DialogueManager] Executing Action Node: {actionNode.action}");
            OnActionExecuted?.Invoke(actionNode);

            string actionType = actionNode.action.ToLower();
            bool isFlowControlAction = (actionType == "jump" || actionType == "if");

            if (isFlowControlAction)
            {
                isProcessingNode = false;
                if (isRestoringState) isRestoringState = false;
                AdvanceDialogue();
                yield break;
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
                if (isRestoringState) isRestoringState = false;
                AdvanceDialogue(); // Automatically advance to the next node.
                yield break;
            }
        }
        isProcessingNode = false;
        // Reset the flag after processing is done.
        if (isRestoringState) isRestoringState = false;

        if (IsSkipping)
        {
            // --- THIS IS THE KEY CHANGE ---
            // If the node we just showed was a dialogue line, wait for the minimum delay.
            if (node is DialogueLine)
            {
                yield return new WaitForSeconds(skipDelay);
            }
            else // Actions can still process instantly
            {
                yield return null;
            }
            AdvanceDialogue();
        }
    }


    public void MakeChoice(string targetLabel)
    {
        isWaitingOnChoice = false;
        JumpToLabel(targetLabel);
        isProcessingNode = false;
        AdvanceDialogue();
    }

    public void JumpToLabel(string label)
    {
        if (currentScript.labels.TryGetValue(label, out int lineIndex))
        {
            currentNodeIndex = FindNodeIndexForLine(lineIndex);
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
    public void RestoreState(string scriptName, int nodeIndex, bool advanceAfterRestore = true)
    {
        isRestoringState = true; // Set the flag before restoring

        if (currentNodeCoroutine != null)
        {
            StopCoroutine(currentNodeCoroutine);
            currentNodeCoroutine = null;
        }

        if (ActionExecutor.Instance != null)
        {
            ActionExecutor.Instance.StopAllActions();
        }

        if (CharacterManager.Instance != null)
        {
            CharacterManager.Instance.StopAllCoroutines();
        }

        isProcessingNode = false;
        isWaitingOnChoice = false;

        LoadScriptFromFile("en", scriptName);
        currentNodeIndex = nodeIndex;

        if (advanceAfterRestore)
        {
            AdvanceDialogue();
        }
        else
        {
            currentNodeCoroutine = StartCoroutine(ProcessCurrentNode());
        }
    }

    public bool IsDialogueActive()
    {
        return currentScript != null && currentNodeIndex < currentScript.nodes.Count;
    }

    public void InitializeReplayRegistry(string language)
    {
        replayRegistry.Clear();
        
        TextAsset[] scriptAssets = Resources.LoadAll<TextAsset>($"Dialogues/{language}");
        if (scriptAssets == null || scriptAssets.Length == 0) return;

        foreach (var asset in scriptAssets)
        {
            ScriptData script = parser.ParseScript(asset.text);
            string scriptName = asset.name;

            ReplaySceneInfo currentActiveScene = null;

            for (int i = 0; i < script.nodes.Count; i++)
            {
                if (script.nodes[i] is ActionNode actionNode)
                {
                    string actionName = actionNode.action.ToLower();
                    if (actionName == "replay_start" || actionName == "replaystart")
                    {
                        string cgId = actionNode.parameters.GetValueOrDefault("param1", "");
                        if (!string.IsNullOrEmpty(cgId))
                        {
                            currentActiveScene = new ReplaySceneInfo
                            {
                                cgId = cgId,
                                scriptName = scriptName,
                                startNodeIndex = i,
                                endNodeIndex = -1
                            };
                        }
                    }
                    else if (actionName == "replay_end" || actionName == "replayend")
                    {
                        if (currentActiveScene != null)
                        {
                            currentActiveScene.endNodeIndex = i;
                            replayRegistry[currentActiveScene.cgId.ToLower()] = currentActiveScene;
                            currentActiveScene = null;
                        }
                    }
                }
            }

            if (currentActiveScene != null)
            {
                currentActiveScene.endNodeIndex = script.nodes.Count - 1;
                replayRegistry[currentActiveScene.cgId.ToLower()] = currentActiveScene;
            }
        }

        Debug.Log($"[DialogueManager] Replay registry initialized with {replayRegistry.Count} replayable scenes.");
    }

    public void StartReplay(string cgId)
    {
        string cgKey = cgId.ToLower();
        if (!replayRegistry.TryGetValue(cgKey, out ReplaySceneInfo info))
        {
            Debug.LogError($"[DialogueManager] Replay scene not found for CG ID '{cgId}'");
            return;
        }

        Debug.Log($"[DialogueManager] Starting replay for CG ID '{cgId}' in script '{info.scriptName}' from index {info.startNodeIndex} to {info.endNodeIndex}");

        isReplaying = true;
        activeReplayEndIndex = info.endNodeIndex;

        string currentLanguage = PlayerPrefs.GetString("language", "en");
        LoadScriptFromFile(currentLanguage, info.scriptName);

        currentNodeIndex = info.startNodeIndex;

        if (currentNodeCoroutine != null) StopCoroutine(currentNodeCoroutine);
        currentNodeCoroutine = StartCoroutine(ProcessCurrentNode());
    }

    public void EndReplay()
    {
        if (!isReplaying) return;
        Debug.Log("[DialogueManager] Replay finished. Returning to gallery/menu.");
        isReplaying = false;
        activeReplayEndIndex = -1;
        isProcessingNode = false;
        
        if (currentNodeCoroutine != null) StopCoroutine(currentNodeCoroutine);
        currentNodeCoroutine = null;

        OnReplayEnded?.Invoke();
        OnDialogueEnded?.Invoke();
    }

    public bool IsWaitingForChoice()
    {
        return isWaitingOnChoice;
    }
}