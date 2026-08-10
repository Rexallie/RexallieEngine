using UnityEngine;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class PersistentData
{
    public List<string> unlockedGalleryCGs = new List<string>();
    public List<string> readDialogueIDs = new List<string>(); // <-- ADDED
}

public class PersistentDataManager : MonoBehaviour
{
    public static PersistentDataManager Instance { get; private set; }

    private PersistentData data;
    private HashSet<string> unlockedCGsLookup;
    private HashSet<string> readDialogueIDsLookup; // <-- ADDED for fast checks
    private string savePath;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            savePath = Path.Combine(Application.persistentDataPath, "persistent_data.json");
            LoadData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- CG Gallery Methods ---
    public void UnlockCG(string cgID)
    {
        if (unlockedCGsLookup.Add(cgID))
        {
            Debug.Log($"Unlocked new CG: {cgID}");
            SaveData();
        }
    }

    public bool IsCGUnlocked(string cgID)
    {
        return unlockedCGsLookup.Contains(cgID);
    }

    // --- NEW: Read Dialogue Methods ---

    /// <summary>
    /// Marks a dialogue line as read and saves the data.
    /// </summary>
    public void MarkLineAsRead(string lineID)
    {
        if (readDialogueIDsLookup.Add(lineID))
        {
            // Only save if a new line was actually added, for efficiency.
            SaveData();
        }
    }

    /// <summary>
    /// Checks if a specific dialogue line has been read.
    /// </summary>
    public bool IsLineRead(string lineID)
    {
        return readDialogueIDsLookup.Contains(lineID);
    }

    // --- Core Data Methods ---
    private void SaveData()
    {
        // Convert the fast lookup sets back to lists for saving.
        data.unlockedGalleryCGs = new List<string>(unlockedCGsLookup);
        data.readDialogueIDs = new List<string>(readDialogueIDsLookup); // <-- ADDED

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
    }

    private void LoadData()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            data = JsonUtility.FromJson<PersistentData>(json);
        }
        else
        {
            data = new PersistentData();
        }

        // Convert the loaded lists into HashSets for fast lookups.
        unlockedCGsLookup = new HashSet<string>(data.unlockedGalleryCGs);
        readDialogueIDsLookup = new HashSet<string>(data.readDialogueIDs); // <-- ADDED
    }
}