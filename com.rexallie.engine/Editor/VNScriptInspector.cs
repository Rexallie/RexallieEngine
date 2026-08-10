using UnityEngine;
using UnityEditor;
using System.IO;

// File: VNScriptInspector.cs
// Place this in Assets/Editor/VNScriptInspector.cs

[CustomEditor(typeof(TextAsset))]
public class VNScriptInspector : Editor
{
    private Vector2 scrollPos;
    private bool isVNScript;
    private ScriptData parsedData;
    private DialogueScriptParser parser;
    private string scriptText;

    void OnEnable()
    {
        string path = AssetDatabase.GetAssetPath(target);
        isVNScript = path.EndsWith(".vns") || path.EndsWith(".vnscript");

        if (isVNScript)
        {
            parser = new DialogueScriptParser();
            TextAsset textAsset = target as TextAsset;
            scriptText = textAsset.text;

            try
            {
                parsedData = parser.ParseScript(scriptText);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error parsing VN Script: {e.Message}");
                parsedData = null;
            }
        }
    }

    public override void OnInspectorGUI()
    {
        if (!isVNScript)
        {
            // Not a VN script, show default inspector
            base.OnInspectorGUI();
            return;
        }

        // Enable GUI for imported assets
        GUI.enabled = true;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Visual Novel Script", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (parsedData != null)
        {
            // Display parsed information
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Scene ID:", parsedData.sceneId);
            EditorGUILayout.LabelField("Total Nodes:", parsedData.nodes.Count.ToString());

            int dialogueCount = 0;
            int actionCount = 0;

            foreach (var node in parsedData.nodes)
            {
                if (node is DialogueLine) dialogueCount++;
                else if (node is ActionNode) actionCount++;
            }

            EditorGUILayout.LabelField("Dialogue Lines:", dialogueCount.ToString());
            EditorGUILayout.LabelField("Actions:", actionCount.ToString());
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Preview button
            if (GUILayout.Button("Open Preview Window", GUILayout.Height(30)))
            {
                VNScriptPreviewWindow.ShowWindow(target, parsedData, scriptText);
            }

            EditorGUILayout.Space();
        }
        else
        {
            EditorGUILayout.HelpBox("Failed to parse script. Check console for errors.", MessageType.Error);
        }

        EditorGUILayout.LabelField("Script Content:", EditorStyles.boldLabel);

        // Show the raw text in a scrollable area
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(300));
        GUIStyle textStyle = new GUIStyle(EditorStyles.textArea);
        textStyle.wordWrap = true;
        textStyle.normal.textColor = GUI.skin.label.normal.textColor; // Fix text color
        EditorGUILayout.TextArea(scriptText, textStyle, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // Open in external editor button
        if (GUILayout.Button("Edit in VS Code"))
        {
            string path = AssetDatabase.GetAssetPath(target);
            string fullPath = Path.GetFullPath(path);

            try
            {
                System.Diagnostics.Process.Start("code", $"\"{fullPath}\"");
            }
            catch
            {
                // Fallback to default editor
                AssetDatabase.OpenAsset(target);
            }
        }
    }
}

public class VNScriptPreviewWindow : EditorWindow
{
    private Object scriptAsset;
    private ScriptData parsedData;
    private string scriptText;
    private Vector2 scrollPos;

    public static void ShowWindow(Object asset, ScriptData data, string text)
    {
        VNScriptPreviewWindow window = GetWindow<VNScriptPreviewWindow>("VN Script Preview");
        window.scriptAsset = asset;
        window.parsedData = data;
        window.scriptText = text;
        window.minSize = new Vector2(400, 500);
        window.Show();
    }

    void OnGUI()
    {
        if (parsedData == null || scriptAsset == null)
        {
            EditorGUILayout.HelpBox("No script loaded", MessageType.Info);
            return;
        }

        // Header
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Visual Novel Script Preview", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("File:", scriptAsset.name);
        EditorGUILayout.LabelField("Scene:", parsedData.sceneId);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        foreach (var node in parsedData.nodes)
        {
            if (node is DialogueLine dialogue)
            {
                // Dialogue box with colored background
                GUI.backgroundColor = new Color(0.9f, 0.95f, 1f);
                EditorGUILayout.BeginVertical("box");
                GUI.backgroundColor = Color.white;

                // Speaker info
                GUIStyle speakerStyle = new GUIStyle(EditorStyles.boldLabel);
                speakerStyle.fontSize = 14;
                EditorGUILayout.LabelField(dialogue.speakerID, speakerStyle);

                EditorGUI.indentLevel++;

                if (!string.IsNullOrEmpty(dialogue.portrait))
                {
                    GUIStyle portraitStyle = new GUIStyle(EditorStyles.miniLabel);
                    portraitStyle.normal.textColor = new Color(0.4f, 0.4f, 0.6f);
                    EditorGUILayout.LabelField($"Portrait: {dialogue.portrait}", portraitStyle);
                }

                if (!string.IsNullOrEmpty(dialogue.expression))
                {
                    GUIStyle expressionStyle = new GUIStyle(EditorStyles.miniLabel);
                    expressionStyle.normal.textColor = new Color(0.6f, 0.4f, 0.6f);
                    EditorGUILayout.LabelField($"Expression: {dialogue.expression}", expressionStyle);
                }

                EditorGUI.indentLevel--;

                EditorGUILayout.Space(5);

                // Dialogue text
                GUIStyle textStyle = new GUIStyle(EditorStyles.label);
                textStyle.wordWrap = true;
                textStyle.fontSize = 12;
                EditorGUILayout.LabelField(dialogue.text, textStyle);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
            else if (node is ActionNode action)
            {
                // Action box with different colored background
                GUI.backgroundColor = new Color(1f, 0.95f, 0.8f);
                EditorGUILayout.BeginVertical("box");
                GUI.backgroundColor = Color.white;

                GUIStyle actionStyle = new GUIStyle(EditorStyles.boldLabel);
                actionStyle.normal.textColor = new Color(0.8f, 0.4f, 0.0f);
                EditorGUILayout.LabelField($"@ {action.action}", actionStyle);

                if (action.parameters != null && action.parameters.Count > 0)
                {
                    EditorGUI.indentLevel++;
                    foreach (var param in action.parameters)
                    {
                        EditorGUILayout.LabelField($"{param.Key}: {param.Value}", EditorStyles.miniLabel);
                    }
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // Footer with stats
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField($"Total Nodes: {parsedData.nodes.Count}", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
    }
}