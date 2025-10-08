using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ==================== ACTION EXECUTOR ====================
// This connects the dialogue system actions to the actual game systems

public class ActionExecutor : MonoBehaviour
{
    public static ActionExecutor Instance { get; private set; }

    [Header("Managers")]
    public CharacterManager characterManager;
    public BackgroundManager backgroundManager;
    public AudioManager audioManager;

    private bool isExecutingAction = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Subscribe to DialogueManager events
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnActionExecuted += ExecuteAction;
        }
    }

    void OnDestroy()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnActionExecuted -= ExecuteAction;
        }
    }

    public void ExecuteAction(ActionNode action)
    {
        switch (action.action.ToLower())
        {
            // Character actions
            case "showcharacter":
                StartCoroutine(ExecuteShowCharacter(action));
                break;
            case "hidecharacter":
                StartCoroutine(ExecuteHideCharacter(action));
                break;
            case "movecharacter":
                // If the 4th parameter is "wait", start the blocking coroutine
                if (action.parameters.GetValueOrDefault("param4", "") == "wait")
                {
                    StartCoroutine(ExecuteMoveCharacterAndWait(action));
                }
                else
                {
                    ExecuteMoveCharacter(action); // Otherwise, run the old non-blocking version
                }
                break;
            case "setexpression":
                ExecuteSetExpression(action);
                break;
            case "setportrait":
                ExecuteSetPortrait(action);
                break;

            // Background actions
            case "setbackground":
                ExecuteSetBackground(action);
                break;

            // Audio actions
            case "playmusic":
                ExecutePlayMusic(action);
                break;
            case "stopmusic":
                ExecuteStopMusic(action);
                break;
            case "playsfx":
                ExecutePlaySFX(action);
                break;

            // Utility actions (these will always wait)
            case "wait":
                StartCoroutine(ExecuteWait(action));
                break;
            case "shake":
                StartCoroutine(ExecuteShake(action));
                break;
            case "fade":
                StartCoroutine(ExecuteFade(action));
                break;

            default:
                Debug.LogWarning($"Unknown action: {action.action}");
                break;
        }
    }

    // ==================== CHARACTER ACTIONS ====================

    private IEnumerator ExecuteShowCharacter(ActionNode action)
    {
        // Check if any effect parameters exist to determine if we need to wait
        bool hasEffect = action.parameters.ContainsKey("fadeIn") || action.parameters.ContainsKey("slideFrom");

        if (characterManager != null)
        {
            if (hasEffect)
            {
                isExecutingAction = true;
                yield return characterManager.StartCoroutine(characterManager.ShowCharacterWithEffect(action));
                isExecutingAction = false;
            }
            else // No effects, show instantly
            {
                string character = action.parameters.GetValueOrDefault("param1", "");
                string position = action.parameters.GetValueOrDefault("param2", "center");
                string portrait = action.parameters.GetValueOrDefault("param3", $"{character.ToLower()}_base");
                string expression = action.parameters.GetValueOrDefault("param4", "");
                characterManager.ShowCharacter(character, position, portrait, expression);
            }
        }
    }

    private IEnumerator ExecuteHideCharacter(ActionNode action)
    {
        // Check if any effect parameters exist
        bool hasEffect = action.parameters.ContainsKey("fadeOut") || action.parameters.ContainsKey("slideTo");
        string character = action.parameters.GetValueOrDefault("param1", "");

        if (characterManager != null)
        {
            if (hasEffect)
            {
                isExecutingAction = true;
                yield return characterManager.StartCoroutine(characterManager.HideCharacterWithEffect(action));
                isExecutingAction = false;
            }
            else // No effects, hide instantly
            {
                characterManager.HideCharacter(character);
            }
        }
    }

    private IEnumerator ExecuteMoveCharacterAndWait(ActionNode action)
    {
        isExecutingAction = true; // Signal to DialogueManager that we are busy
        string character = action.parameters.GetValueOrDefault("param1", "");
        string position = action.parameters.GetValueOrDefault("param2", "center");
        float duration = float.Parse(action.parameters.GetValueOrDefault("param3", "0.5"));

        if (characterManager != null)
        {
            // Start and wait for the CharacterManager's move coroutine
            yield return characterManager.StartCoroutine(characterManager.MoveCharacterAndWait(character, position, duration));
        }
        isExecutingAction = false; // Signal that we are finished
    }


    private void ExecuteMoveCharacter(ActionNode action)
    {
        // @moveCharacter alice center 0.5
        string character = action.parameters.GetValueOrDefault("param1", "");
        string position = action.parameters.GetValueOrDefault("param2", "center");
        float duration = float.Parse(action.parameters.GetValueOrDefault("param3", "0.5"));

        if (characterManager != null)
        {
            characterManager.MoveCharacter(character, position, duration);
        }
    }

    private void ExecuteSetExpression(ActionNode action)
    {
        // @setExpression alice happy
        string character = action.parameters.GetValueOrDefault("param1", "");
        string expression = action.parameters.GetValueOrDefault("param2", "neutral");

        if (characterManager != null)
        {
            characterManager.SetCharacterExpression(character, expression);
        }
    }

    private void ExecuteSetPortrait(ActionNode action)
    {
        // @setPortrait alice alice_party_dress
        // or @setPortrait alice alice_party_dress happy
        string character = action.parameters.GetValueOrDefault("param1", "");
        string portraitSet = action.parameters.GetValueOrDefault("param2", "");
        string expression = action.parameters.GetValueOrDefault("param3", null);

        if (characterManager != null)
        {
            characterManager.SetCharacterPortrait(character, portraitSet, expression);
        }
    }

    // ==================== BACKGROUND ACTIONS ====================

    private void ExecuteSetBackground(ActionNode action)
    {
        // @setBackground bg_school_hallway fade
        string backgroundName = action.parameters.GetValueOrDefault("param1", "");
        string transition = action.parameters.GetValueOrDefault("param2", "instant");

        if (backgroundManager != null)
        {
            backgroundManager.SetBackground(backgroundName, transition);
        }
    }

    // ==================== AUDIO ACTIONS ====================

    private void ExecutePlayMusic(ActionNode action)
    {
        // @playMusic morning_theme fadeIn:2.0
        string trackName = action.parameters.GetValueOrDefault("param1", "");
        float fadeIn = float.Parse(action.parameters.GetValueOrDefault("fadeIn", "0"));

        if (audioManager != null)
        {
            audioManager.PlayMusic(trackName, fadeIn);
        }
    }

    private void ExecuteStopMusic(ActionNode action)
    {
        // @stopMusic fadeOut:2.0
        float fadeOut = float.Parse(action.parameters.GetValueOrDefault("fadeOut", "0"));

        if (audioManager != null)
        {
            audioManager.StopMusic(fadeOut);
        }
    }

    private void ExecutePlaySFX(ActionNode action)
    {
        // @playSFX door_close
        string sfxName = action.parameters.GetValueOrDefault("param1", "");

        if (audioManager != null)
        {
            audioManager.PlaySFX(sfxName);
        }
    }

    // ==================== UTILITY ACTIONS ====================

    private IEnumerator ExecuteWait(ActionNode action)
    {
        // @wait 1.0
        float duration = float.Parse(action.parameters.GetValueOrDefault("param1", "1"));
        isExecutingAction = true;
        // USE REALTIME WAIT
        yield return new WaitForSecondsRealtime(duration);
        isExecutingAction = false;
    }

    private IEnumerator ExecuteShake(ActionNode action)
    {
        // @shake intensity:0.5 duration:0.3
        float intensity = float.Parse(action.parameters.GetValueOrDefault("intensity", "0.5"));
        float duration = float.Parse(action.parameters.GetValueOrDefault("duration", "0.3"));

        isExecutingAction = true;
        Camera mainCamera = Camera.main;
        Vector3 originalPosition = mainCamera.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;

            mainCamera.transform.position = originalPosition + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        mainCamera.transform.position = originalPosition;
        isExecutingAction = false;
    }

    private IEnumerator ExecuteFade(ActionNode action)
    {
        // @fade black duration:1.0
        // This is a placeholder - you'd implement with a UI fade panel
        float duration = float.Parse(action.parameters.GetValueOrDefault("duration", "1.0"));

        isExecutingAction = true;
        yield return new WaitForSeconds(duration);
        isExecutingAction = false;
    }

    public bool IsExecutingAction()
    {
        return isExecutingAction;
    }
}