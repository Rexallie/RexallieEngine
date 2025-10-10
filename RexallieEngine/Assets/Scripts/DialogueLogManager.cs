using System.Collections.Generic;
using UnityEngine;

// This struct holds the data for a single line of dialogue in the history.
public struct LogEntry
{
    public string speakerName;
    public string dialogueText;
}

public class DialogueLogManager : MonoBehaviour
{
    public static DialogueLogManager Instance { get; private set; }

    private List<LogEntry> log = new List<LogEntry>();

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
    /// Adds a new entry to the dialogue log.
    /// </summary>
    public void AddLog(string speaker, string dialogue)
    {
        log.Add(new LogEntry { speakerName = speaker, dialogueText = dialogue });
    }

    /// <summary>
    /// Returns the entire dialogue history.
    /// </summary>
    public List<LogEntry> GetHistory()
    {
        return new List<LogEntry>(log);
    }
}