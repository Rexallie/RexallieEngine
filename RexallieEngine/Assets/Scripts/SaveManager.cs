using UnityEngine;
using System.IO;
using System.Collections;

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

    public void SaveGame(int slotNumber, string saveName, System.Action onSaveComplete = null)
    {
        StartCoroutine(SaveGameCoroutine(slotNumber, saveName, onSaveComplete));
    }

    private IEnumerator SaveGameCoroutine(int slotNumber, string saveName, System.Action onSaveComplete)
    {
        // 1. Hide the UI using the new UIManager function
        UIManager.Instance.SetUIActive(false);
        yield return new WaitForEndOfFrame(); // Wait a frame for UI to disappear

        // 2. Capture Screenshot
        Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
        string screenshotPath = GetScreenshotPath(slotNumber);

        // 3. Show the UI again
        UIManager.Instance.SetUIActive(true);

        // 4. Gather Data and Save
        HistoryState data = GatherCurrentState();
        data.metadata.saveName = saveName;
        data.metadata.timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        data.metadata.totalPlaytime = GameplayTimeManager.Instance.GetPlaytime();
        data.metadata.screenshotPath = screenshotPath;

        byte[] bytes = screenshot.EncodeToPNG();
        File.WriteAllBytes(screenshotPath, bytes);

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetSaveFilePath(slotNumber), json);

        Destroy(screenshot);

        Debug.Log($"Game Saved to slot {slotNumber}");

        // 5. Call the callback to notify the UI that the save is finished.
        onSaveComplete?.Invoke();
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