using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class HistoryManager : MonoBehaviour
{
    public static HistoryManager Instance { get; private set; }

    // We use a List like a Stack to store our history states.
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

    /// <summary>
    /// Records the current state of all managers into a new history snapshot.
    /// </summary>
    public void RecordState()
    {
        HistoryState newState = new HistoryState();

        // Gather data from all relevant managers
        newState.currentScriptName = DialogueManager.Instance.GetCurrentScriptName();
        newState.currentNodeIndex = DialogueManager.Instance.GetCurrentNodeIndex();
        newState.activeCharacters = CharacterManager.Instance.GetCharactersState();
        newState.currentBackgroundName = BackgroundManager.Instance.GetCurrentBackgroundName();
        newState.currentMusicTrackName = AudioManager.Instance.GetCurrentMusicTrack();
        newState.variables = VariableManager.Instance.GetVariableData();

        // Add the new state to our history
        history.Add(newState);

        // If our history is over capacity, remove the oldest entry
        if (history.Count > historyCapacity)
        {
            history.RemoveAt(0);
        }
    }

    /// <summary>
    /// Rolls the game back to the previously saved history state.
    /// </summary>
    public void Rollback()
    {
        if (history.Count == 0)
        {
            Debug.Log("No history to roll back to.");
            return;
        }

        // Get the last state from the list
        HistoryState stateToRestore = history.Last();
        // Remove it from the history
        history.RemoveAt(history.Count - 1);

        // Restore all managers to the saved state
        BackgroundManager.Instance.RestoreState(stateToRestore.currentBackgroundName);
        CharacterManager.Instance.RestoreState(stateToRestore.activeCharacters);
        AudioManager.Instance.RestoreState(stateToRestore.currentMusicTrackName);
        VariableManager.Instance.RestoreVariableData(stateToRestore.variables);

        // IMPORTANT: Restore DialogueManager last.
        // We add a 'false' flag to prevent it from auto-advancing after loading the state.
        DialogueManager.Instance.RestoreState(stateToRestore.currentScriptName, stateToRestore.currentNodeIndex, false);
    }
}