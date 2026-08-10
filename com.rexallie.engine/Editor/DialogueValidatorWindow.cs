using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class DialogueValidatorWindow : EditorWindow
{
    private struct ValidationResult
    {
        public string filePath;
        public string fileName;
        public int lineNumber;
        public string lineContent;
        public string message;
        public MessageType type;
    }

    private List<ValidationResult> results = new List<ValidationResult>();
    private Vector2 scrollPosition;
    private bool hasScanned = false;

    [MenuItem("Tools/VNS Script Validator")]
    public static void ShowWindow()
    {
        GetWindow<DialogueValidatorWindow>("VNS Script Validator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Visual Novel Script Validator", EditorStyles.boldLabel);
        GUILayout.Label("Scans all .vns files inside Assets/Resources/Dialogues for syntax errors and broken references.", EditorStyles.wordWrappedLabel);

        EditorGUILayout.Space();

        if (GUILayout.Button("Validate Dialogue Scripts", GUILayout.Height(30)))
        {
            ValidateScripts();
        }

        EditorGUILayout.Space();

        if (hasScanned)
        {
            if (results.Count == 0)
            {
                EditorGUILayout.HelpBox("All scripts validated successfully! No issues found.", MessageType.Info);
            }
            else
            {
                int errors = results.FindAll(r => r.type == MessageType.Error).Count;
                int warnings = results.FindAll(r => r.type == MessageType.Warning).Count;
                GUILayout.Label($"Found {errors} error(s) and {warnings} warning(s):", EditorStyles.boldLabel);

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                foreach (var res in results)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    GUIStyle style = new GUIStyle(EditorStyles.label);
                    if (res.type == MessageType.Error) style.normal.textColor = Color.red;
                    else if (res.type == MessageType.Warning) style.normal.textColor = Color.yellow;
                    
                    GUILayout.Label($"[{res.type}] {res.fileName} (Line {res.lineNumber})", EditorStyles.boldLabel);
                    GUILayout.Label($"Content: \"{res.lineContent}\"", EditorStyles.miniLabel);
                    GUILayout.Label(res.message, style);

                    if (GUILayout.Button("Ping Script Asset", GUILayout.Width(120)))
                    {
                        var obj = AssetDatabase.LoadAssetAtPath<TextAsset>(res.filePath);
                        if (obj != null)
                        {
                            EditorGUIUtility.PingObject(obj);
                            Selection.activeObject = obj;
                        }
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(5);
                }
                EditorGUILayout.EndScrollView();
            }
        }
    }

    private void ValidateScripts()
    {
        results.Clear();
        hasScanned = true;

        string folderPath = Path.Combine(Application.dataPath, "Resources/Dialogues");
        if (!Directory.Exists(folderPath))
        {
            results.Add(new ValidationResult
            {
                filePath = "",
                fileName = "N/A",
                lineNumber = 0,
                lineContent = "",
                message = $"Dialogue directory not found at: Assets/Resources/Dialogues",
                type = MessageType.Error
            });
            return;
        }

        string[] files = Directory.GetFiles(folderPath, "*.vns", SearchOption.AllDirectories);

        // Gather all character IDs from CharacterManager if available
        HashSet<string> registeredCharacters = new HashSet<string>();
        CharacterManager charManager = FindFirstObjectByType<CharacterManager>();
        if (charManager != null)
        {
            foreach (var character in charManager.availableCharacters)
            {
                if (character != null && !string.IsNullOrEmpty(character.characterID))
                {
                    registeredCharacters.Add(character.characterID.ToLower());
                }
            }
        }

        foreach (string file in files)
        {
            string relativePath = "Assets" + file.Substring(Application.dataPath.Length).Replace('\\', '/');
            string fileName = Path.GetFileName(file);
            string[] lines = File.ReadAllLines(file);

            HashSet<string> definedLabels = new HashSet<string>();

            // Pass 1: Gather all declared labels
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("//") || string.IsNullOrWhiteSpace(line)) continue;

                if (line.EndsWith(":") && !line.Contains(" ") && !line.Contains("[") && !line.Contains("<"))
                {
                    string label = line.Substring(0, line.Length - 1).Trim();
                    if (!definedLabels.Contains(label))
                    {
                        definedLabels.Add(label);
                    }
                }
            }

            // Pass 2: Verify references and instructions
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                int displayLineNumber = i + 1;

                if (line.StartsWith("//") || string.IsNullOrWhiteSpace(line)) continue;

                // 1. Label Jump validation
                if (line.StartsWith("@jump"))
                {
                    string[] parts = line.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2)
                    {
                        results.Add(new ValidationResult
                        {
                            filePath = relativePath,
                            fileName = fileName,
                            lineNumber = displayLineNumber,
                            lineContent = line,
                            message = "@jump action has no target label name.",
                            type = MessageType.Error
                        });
                    }
                    else
                    {
                        string target = parts[1].Trim();
                        if (!definedLabels.Contains(target))
                        {
                            results.Add(new ValidationResult
                            {
                                filePath = relativePath,
                                fileName = fileName,
                                lineNumber = displayLineNumber,
                                lineContent = line,
                                message = $"Broken @jump! Target label '{target}' does not exist in this script.",
                                type = MessageType.Error
                            });
                        }
                    }
                }

                // 2. Conditional branch validation
                if (line.StartsWith("@if"))
                {
                    string[] parts = line.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                    int jumpIndex = -1;
                    for (int p = 0; p < parts.Length; p++)
                    {
                        if (parts[p].ToLower() == "jump")
                        {
                            jumpIndex = p;
                            break;
                        }
                    }

                    if (jumpIndex == -1 || jumpIndex + 1 >= parts.Length)
                    {
                        results.Add(new ValidationResult
                        {
                            filePath = relativePath,
                            fileName = fileName,
                            lineNumber = displayLineNumber,
                            lineContent = line,
                            message = "Malformed @if condition! Missing target jump label.",
                            type = MessageType.Error
                        });
                    }
                    else
                    {
                        string target = parts[jumpIndex + 1].Trim();
                        if (!definedLabels.Contains(target))
                        {
                            results.Add(new ValidationResult
                            {
                                filePath = relativePath,
                                fileName = fileName,
                                lineNumber = displayLineNumber,
                                lineContent = line,
                                message = $"Broken @if condition! Target label '{target}' does not exist in this script.",
                                type = MessageType.Error
                            });
                        }
                    }
                }

                // 3. Speaker verification
                if (line.Contains(":") && !line.StartsWith("@"))
                {
                    int colonIndex = line.LastIndexOf(':');
                    string speakerPart = line.Substring(0, colonIndex).Trim();

                    string speakerId = speakerPart;
                    if (speakerId.Contains("<")) speakerId = speakerId.Substring(0, speakerId.IndexOf('<')).Trim();
                    if (speakerId.Contains("[")) speakerId = speakerId.Substring(0, speakerId.IndexOf('[')).Trim();

                    if (registeredCharacters.Count > 0 && !registeredCharacters.Contains(speakerId.ToLower()))
                    {
                        results.Add(new ValidationResult
                        {
                            filePath = relativePath,
                            fileName = fileName,
                            lineNumber = displayLineNumber,
                            lineContent = line,
                            message = $"Warning: Speaker '{speakerId}' is not defined in the CharacterManager list of available characters.",
                            type = MessageType.Warning
                        });
                    }
                }
            }
        }
    }
}
