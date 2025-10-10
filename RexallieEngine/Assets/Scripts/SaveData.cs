using System.Collections.Generic;
using UnityEngine;

// This file contains all data structures for saving, loading, and history.

// NEW: This class holds all the metadata for a save file.
[System.Serializable]
public class SaveMetadata
{
    public string saveName;
    public string timestamp;
    public float totalPlaytime;
    public string screenshotPath;
}

[System.Serializable]
public class CharacterSaveData
{
    public string characterName;
    public Vector2 anchoredPosition;
    public string portrait;
    public string expression;
    public bool isHighlighted;
}

[System.Serializable]
public class VariableSaveData
{
    public string name;
    public string value;
    public string type;
}

[System.Serializable]
public class SceneEffectsSaveData
{
    public Vector2 worldContainerPosition;
    public Vector3 worldContainerScale;
}

[System.Serializable]
public class UISaveData
{
    public bool dialoguePanelVisible;
    public bool speakerNamePanelVisible;
    public bool quickMenuPanelVisible;
}

// HistoryState now includes the new metadata.
[System.Serializable]
public class HistoryState
{
    public SaveMetadata metadata; // <-- ADDED

    public string currentScriptName;
    public int currentNodeIndex;
    public List<CharacterSaveData> activeCharacters;
    public string currentBackgroundName;
    public string currentMusicTrackName;
    public List<VariableSaveData> variables;
    public SceneEffectsSaveData sceneEffectsState;
    public UISaveData uiState;

    public HistoryState()
    {
        metadata = new SaveMetadata(); // <-- ADDED
        activeCharacters = new List<CharacterSaveData>();
        variables = new List<VariableSaveData>();
        sceneEffectsState = new SceneEffectsSaveData();
        uiState = new UISaveData();
    }
}