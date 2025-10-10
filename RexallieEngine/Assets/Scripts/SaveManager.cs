using UnityEngine;
using System.IO;
using System.Collections; // Required for Coroutines

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    public int MaxSaveSlots = 10;

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

    public void SaveGame(int slotNumber, string saveName)
    {
        // Start the save process as a coroutine to handle the screenshot correctly.
        StartCoroutine(SaveGameCoroutine(slotNumber, saveName));
    }

    private IEnumerator SaveGameCoroutine(int slotNumber, string saveName)
    {
        // --- 1. Prepare for Screenshot ---
        // Instantly hide the dialogue UI so it's not in the picture.
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetDialoguePanelsActive(false);
        }

        // Wait one frame for the UI to become fully hidden before taking the picture.
        yield return new WaitForEndOfFrame();

        // --- 2. Capture Screenshot ---
        Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
        string screenshotPath = GetScreenshotPath(slotNumber);

        // --- 3. Restore UI ---
        // Instantly show the dialogue UI again. This all happens so fast the player won't see it.
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetDialoguePanelsActive(true);
        }

        // --- 4. Gather Data & Save ---
        HistoryState data = GatherCurrentState();

        data.metadata.saveName = saveName;
        data.metadata.timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        data.metadata.totalPlaytime = GameplayTimeManager.Instance.GetPlaytime();
        data.metadata.screenshotPath = screenshotPath;

        // Encode and save the screenshot file to disk.
        byte[] bytes = screenshot.EncodeToPNG();
        File.WriteAllBytes(screenshotPath, bytes);

        // Save the main data file.
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetSaveFilePath(slotNumber), json);

        // Clean up the texture from memory
        Destroy(screenshot);

        Debug.Log($"Game Saved to slot {slotNumber}");
    }


    public void LoadGame(int slotNumber)
    {
        string path = GetSaveFilePath(slotNumber);
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            HistoryState data = JsonUtility.FromJson<HistoryState>(json);

            if (GameplayTimeManager.Instance != null)
                GameplayTimeManager.Instance.SetPlaytime(data.metadata.totalPlaytime);

            RestoreGameState(data);
            Debug.Log($"Game Loaded from slot {slotNumber}");
        }
    }

    public SaveMetadata GetSaveMetadata(int slotNumber)
    {
        string path = GetSaveFilePath(slotNumber);
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            HistoryState data = JsonUtility.FromJson<HistoryState>(json);
            return data.metadata;
        }
        return null;
    }

    private HistoryState GatherCurrentState()
    {
        HistoryState data = new HistoryState();

        if (DialogueManager.Instance != null)
        {
            data.currentScriptName = DialogueManager.Instance.GetCurrentScriptName();
            data.currentNodeIndex = DialogueManager.Instance.GetCurrentNodeIndex();
        }
        if (CharacterManager.Instance != null)
            data.activeCharacters = CharacterManager.Instance.GetCharactersState();
        if (BackgroundManager.Instance != null)
            data.currentBackgroundName = BackgroundManager.Instance.GetCurrentBackgroundName();
        if (AudioManager.Instance != null)
            data.currentMusicTrackName = AudioManager.Instance.GetCurrentMusicTrack();
        if (VariableManager.Instance != null)
            data.variables = VariableManager.Instance.GetVariableData();
        if (SceneEffectsManager.Instance != null)
            data.sceneEffectsState = SceneEffectsManager.Instance.GetState();
        if (UIManager.Instance != null)
            data.uiState = UIManager.Instance.GetState();

        return data;
    }

    private void RestoreGameState(HistoryState data)
    {
        if (SceneEffectsManager.Instance != null) SceneEffectsManager.Instance.RestoreState(data.sceneEffectsState);
        if (UIManager.Instance != null) UIManager.Instance.RestoreState(data.uiState);
        if (BackgroundManager.Instance != null) BackgroundManager.Instance.RestoreState(data.currentBackgroundName);
        if (CharacterManager.Instance != null) CharacterManager.Instance.RestoreState(data.activeCharacters);
        if (AudioManager.Instance != null) AudioManager.Instance.RestoreState(data.currentMusicTrackName);
        if (VariableManager.Instance != null) VariableManager.Instance.RestoreVariableData(data.variables);

        if (DialogueManager.Instance != null)
            DialogueManager.Instance.RestoreState(data.currentScriptName, data.currentNodeIndex, true);
    }

    private string GetSaveFilePath(int slotNumber)
    {
        return Path.Combine(Application.persistentDataPath, $"savegame_{slotNumber}.json");
    }

    private string GetScreenshotPath(int slotNumber)
    {
        return Path.Combine(Application.persistentDataPath, $"savegame_{slotNumber}.png");
    }
}