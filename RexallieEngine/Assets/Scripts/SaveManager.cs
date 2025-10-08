using UnityEngine;
using System.IO;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    // We'll use a simple slot system, you can save to different files.
    private int currentSlot = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep the manager alive between scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SaveGame(int slotNumber)
    {
        currentSlot = slotNumber;
        SaveData data = new SaveData();

        // 1. Gather all the data from the other managers
        data = GatherSaveData();

        // 2. Convert the data to a JSON string
        string json = JsonUtility.ToJson(data, true); // The 'true' formats it nicely

        // 3. Write the JSON string to a file
        File.WriteAllText(GetSaveFilePath(slotNumber), json);

        Debug.Log($"Game Saved to slot {slotNumber}");
    }

    public void LoadGame(int slotNumber)
    {
        string path = GetSaveFilePath(slotNumber);

        if (File.Exists(path))
        {
            // 1. Read the JSON from the file
            string json = File.ReadAllText(path);

            // 2. Convert the JSON back to a SaveData object
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            // 3. Restore the game state using the loaded data
            RestoreGameState(data);

            Debug.Log($"Game Loaded from slot {slotNumber}");
        }
        else
        {
            Debug.LogWarning($"No save file found for slot {slotNumber}");
        }
    }

    private SaveData GatherSaveData()
    {
        SaveData data = new SaveData();

        // Get Dialogue State
        if (DialogueManager.Instance != null)
        {
            data.currentScriptName = DialogueManager.Instance.GetCurrentScriptName();
            data.currentNodeIndex = DialogueManager.Instance.GetCurrentNodeIndex();
        }

        // Get Character State
        if (CharacterManager.Instance != null)
        {
            data.activeCharacters = CharacterManager.Instance.GetCharactersState();
        }

        // Get Background State
        if (BackgroundManager.Instance != null)
        {
            data.currentBackgroundName = BackgroundManager.Instance.GetCurrentBackgroundName();
        }

        // Get Audio State
        if (AudioManager.Instance != null)
        {
            data.currentMusicTrackName = AudioManager.Instance.GetCurrentMusicTrack();
        }

        // Get Variable State
        if (VariableManager.Instance != null)
        {
            data.variables = VariableManager.Instance.GetVariableData();
        }

        return data;
    }

    private void RestoreGameState(SaveData data)
    {
        // Restore managers in a logical order (e.g., background first, then characters)

        if (VariableManager.Instance != null)
        {
            VariableManager.Instance.RestoreVariableData(data.variables);
        }

        if (BackgroundManager.Instance != null)
        {
            BackgroundManager.Instance.RestoreState(data.currentBackgroundName);
        }

        if (CharacterManager.Instance != null)
        {
            CharacterManager.Instance.RestoreState(data.activeCharacters);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.RestoreState(data.currentMusicTrackName);
        }

        // IMPORTANT: Restore DialogueManager last, as it will trigger the UI update for the loaded line.
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.RestoreState(data.currentScriptName, data.currentNodeIndex);
        }
    }

    private string GetSaveFilePath(int slotNumber)
    {
        // Application.persistentDataPath is a safe, writable directory on any platform
        return Path.Combine(Application.persistentDataPath, $"savegame_{slotNumber}.json");
    }
}