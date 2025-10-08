using System.Collections.Generic;
using UnityEngine;

// This attribute allows Unity's JsonUtility to serialize this class.
[System.Serializable]
public class SaveData
{
    // Dialogue State
    public string currentScriptName;
    public int currentNodeIndex;

    // Scene State
    public string currentBackgroundName;
    public List<CharacterSaveData> activeCharacters;

    // Audio State
    public string currentMusicTrackName;

    // Constructor to initialize the lists
    public SaveData()
    {
        activeCharacters = new List<CharacterSaveData>();
    }
}

// A specific class to hold the state of a single character.
[System.Serializable]
public class CharacterSaveData
{
    public string characterName;
    public string position; // e.g., "left", "center"
    public string portrait;
    public string expression;
    public Vector2 anchoredPosition; // For precise UI position
}