using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class HistoryManager : MonoBehaviour
{
    public static HistoryManager Instance { get; private set; }

    private List<HistoryState> history = new List<HistoryState>();

    [Tooltip("The maximum number of history states to keep in memory.")]
    public int historyCapacity = 100;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RecordState()
    {
        HistoryState newState = new HistoryState();

        newState.currentScriptName = DialogueManager.Instance.GetCurrentScriptName();
        newState.currentNodeIndex = DialogueManager.Instance.GetCurrentNodeIndex();
        newState.activeCharacters = CharacterManager.Instance.GetCharactersState();
        newState.currentBackgroundName = BackgroundManager.Instance.GetCurrentBackgroundName();
        newState.currentMusicTrackName = AudioManager.Instance.GetCurrentMusicTrack();
        newState.variables = VariableManager.Instance.GetVariableData();
        newState.sceneEffectsState = SceneEffectsManager.Instance.GetState();
        newState.uiState = UIManager.Instance.GetState();

        history.Add(newState);

        if (history.Count > historyCapacity)
        {
            history.RemoveAt(0);
        }
    }

    public void Rollback()
    {
        if (history.Count < 2)
        {
            Debug.Log("Not enough history to roll back to.");
            return;
        }

        // Discard the state of the current line.
        history.RemoveAt(history.Count - 1);

        // Get the state of the previous line.
        HistoryState stateToRestore = history.Last();

        // Restore everything instantly.
        SceneEffectsManager.Instance.RestoreState(stateToRestore.sceneEffectsState);
        UIManager.Instance.RestoreState(stateToRestore.uiState);
        BackgroundManager.Instance.RestoreState(stateToRestore.currentBackgroundName);
        CharacterManager.Instance.RestoreState(stateToRestore.activeCharacters);
        AudioManager.Instance.RestoreState(stateToRestore.currentMusicTrackName);
        VariableManager.Instance.RestoreVariableData(stateToRestore.variables);

        // Restore the dialogue state and display the line without creating a new history record.
        DialogueManager.Instance.RestoreState(stateToRestore.currentScriptName, stateToRestore.currentNodeIndex, false);
    }

    public List<HistoryState> GetHistory()
    {
        return history;
    }

    public void ClearHistory()
    {
        history.Clear();
    }

    public void RollbackToState(HistoryState targetState)
    {
        int index = history.IndexOf(targetState);
        if (index < 0)
        {
            Debug.LogWarning("Target history state not found.");
            return;
        }

        // Discard all states after the target state
        while (history.Count > index + 1)
        {
            history.RemoveAt(history.Count - 1);
        }

        // Restore everything instantly
        SceneEffectsManager.Instance.RestoreState(targetState.sceneEffectsState);
        UIManager.Instance.RestoreState(targetState.uiState);
        BackgroundManager.Instance.RestoreState(targetState.currentBackgroundName);
        CharacterManager.Instance.RestoreState(targetState.activeCharacters);
        AudioManager.Instance.RestoreState(targetState.currentMusicTrackName);
        VariableManager.Instance.RestoreVariableData(targetState.variables);

        // Restore dialogue state without re-recording
        DialogueManager.Instance.RestoreState(targetState.currentScriptName, targetState.currentNodeIndex, false);
    }
}