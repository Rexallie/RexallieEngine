using UnityEngine;
using System.IO;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

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

    public void SaveGame(int slotNumber)
    {
        // Use the comprehensive HistoryState as our save data format.
        HistoryState data = GatherCurrentState();

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetSaveFilePath(slotNumber), json);

        Debug.Log($"Game Saved to slot {slotNumber}");
    }

    public void LoadGame(int slotNumber)
    {
        string path = GetSaveFilePath(slotNumber);

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            HistoryState data = JsonUtility.FromJson<HistoryState>(json);

            RestoreGameState(data);

            Debug.Log($"Game Loaded from slot {slotNumber}");
        }
        else
        {
            Debug.LogWarning($"No save file found for slot {slotNumber}");
        }
    }

    private HistoryState GatherCurrentState()
    {
        HistoryState data = new HistoryState();

        // Gather data from all managers, just like HistoryManager does.
        data.currentScriptName = DialogueManager.Instance.GetCurrentScriptName();
        data.currentNodeIndex = DialogueManager.Instance.GetCurrentNodeIndex();
        data.activeCharacters = CharacterManager.Instance.GetCharactersState();
        data.currentBackgroundName = BackgroundManager.Instance.GetCurrentBackgroundName();
        data.currentMusicTrackName = AudioManager.Instance.GetCurrentMusicTrack();
        data.variables = VariableManager.Instance.GetVariableData();
        data.sceneEffectsState = SceneEffectsManager.Instance.GetState();
        data.uiState = UIManager.Instance.GetState();

        return data;
    }

    private void RestoreGameState(HistoryState data)
    {
        // Restore all managers to the saved state.
        SceneEffectsManager.Instance.RestoreState(data.sceneEffectsState);
        UIManager.Instance.RestoreState(data.uiState);
        BackgroundManager.Instance.RestoreState(data.currentBackgroundName);
        CharacterManager.Instance.RestoreState(data.activeCharacters);
        AudioManager.Instance.RestoreState(data.currentMusicTrackName);
        VariableManager.Instance.RestoreVariableData(data.variables);

        // Restore DialogueManager last. The 'true' flag tells it to auto-advance to the loaded line.
        DialogueManager.Instance.RestoreState(data.currentScriptName, data.currentNodeIndex, true);
    }

    private string GetSaveFilePath(int slotNumber)
    {
        return Path.Combine(Application.persistentDataPath, $"savegame_{slotNumber}.json");
    }
}