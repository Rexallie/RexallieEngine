using System.Collections.Generic;
using UnityEngine;

// This class holds a snapshot of the game state at a single point in time.
[System.Serializable]
public class HistoryState
{
    // Dialogue State
    public string currentScriptName;
    public int currentNodeIndex;

    // Scene State
    public string currentBackgroundName;
    public List<CharacterSaveData> activeCharacters;

    // Audio State
    public string currentMusicTrackName;

    // Variable State
    public List<VariableSaveData> variables;

    public HistoryState()
    {
        activeCharacters = new List<CharacterSaveData>();
        variables = new List<VariableSaveData>();
    }
}