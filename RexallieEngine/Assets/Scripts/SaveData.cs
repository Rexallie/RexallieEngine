using System.Collections.Generic;
using UnityEngine;

// This file now contains all data structures for saving/loading and history.

[System.Serializable]
public class CharacterSaveData
{
    public string characterName;
    public Vector2 anchoredPosition;
    public string portrait;
    public string expression;
    public bool isHighlighted; // To remember who was speaking
}

[System.Serializable]
public class VariableSaveData
{
    public string name;
    public string value;
    public string type;
}

// NEW: Holds the state of the zoom and pan
[System.Serializable]
public class SceneEffectsSaveData
{
    public Vector2 worldContainerPosition;
    public Vector3 worldContainerScale;
}

// NEW: Holds the state of the UI panels
[System.Serializable]
public class UISaveData
{
    public bool dialoguePanelVisible;
    public bool speakerNamePanelVisible;
    public bool quickMenuPanelVisible;
}

// This is the main class that holds a complete snapshot of the game state.
[System.Serializable]
public class HistoryState
{
    public string currentScriptName;
    public int currentNodeIndex;
    public List<CharacterSaveData> activeCharacters;
    public string currentBackgroundName;
    public string currentMusicTrackName;
    public List<VariableSaveData> variables;
    public SceneEffectsSaveData sceneEffectsState; // Added
    public UISaveData uiState; // Added

    public HistoryState()
    {
        activeCharacters = new List<CharacterSaveData>();
        variables = new List<VariableSaveData>();
        sceneEffectsState = new SceneEffectsSaveData();
        uiState = new UISaveData();
    }
}