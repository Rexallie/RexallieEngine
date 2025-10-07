using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

// File: VNScriptImporter.cs
// Place this in Assets/Editor/VNScriptImporter.cs

public class VNScriptImporter : AssetPostprocessor
{
    static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        foreach (string asset in importedAssets)
        {
            if (asset.EndsWith(".vns") || asset.EndsWith(".vnscript"))
            {
                Debug.Log($"Imported VN Script: {asset}");
            }
        }
    }
}

[ScriptedImporter(1, "vns")]
public class VNSImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        TextAsset subAsset = new TextAsset(File.ReadAllText(ctx.assetPath));
        ctx.AddObjectToAsset("text", subAsset);
        ctx.SetMainObject(subAsset);
    }
}

// Add a custom icon for .vns files in the Unity Project window
[InitializeOnLoad]
public class VNScriptIconHandler
{
    static VNScriptIconHandler()
    {
        EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
    }

    static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
    {
        string assetPath = AssetDatabase.GUIDToAssetPath(guid);

        if (assetPath.EndsWith(".vns") || assetPath.EndsWith(".vnscript"))
        {
            // Draw a small label to identify VN scripts
            if (selectionRect.width > selectionRect.height)
            {
                // List view
                Rect labelRect = selectionRect;
                labelRect.x = selectionRect.xMax - 35;
                labelRect.width = 35;

                GUI.Label(labelRect, "[VN]", EditorStyles.miniLabel);
            }
        }
    }
}