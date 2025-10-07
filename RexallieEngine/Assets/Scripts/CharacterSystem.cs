using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CharacterManager : MonoBehaviour
{
    public static CharacterManager Instance { get; private set; }

    [Header("Character Database")]
    public List<CharacterData> availableCharacters = new List<CharacterData>();

    [Header("Display Settings")]
    public RectTransform characterContainer; // Changed to RectTransform for UI
    public GameObject characterPrefab;

    [Header("Position Presets (Anchored Position X)")]
    [Tooltip("For 1920x1080, typical values: Left=-600, Center=0, Right=600")]
    public float leftPositionX = -600f;
    public float centerPositionX = 0f;
    public float rightPositionX = 600f;
    public float farLeftPositionX = -900f;
    public float farRightPositionX = 900f;

    [Header("Character Size")]
    [Tooltip("Scale multiplier for character size")]
    public float characterScale = 1f;

    private Dictionary<string, CharacterController> activeCharacters = new Dictionary<string, CharacterController>();
    private bool useUIMode = true; // Assume UI mode if using RectTransform

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

        // Detect if we're using UI or world space
        useUIMode = (characterContainer != null);
    }

    public CharacterData GetCharacterData(string characterName)
    {
        return availableCharacters.Find(c => c.characterName.ToLower() == characterName.ToLower());
    }

    public void ShowCharacter(string characterName, string position, string portrait, string expression)
    {
        CharacterData data = GetCharacterData(characterName);
        if (data == null)
        {
            Debug.LogError($"Character not found: {characterName}");
            return;
        }

        CharacterController controller;

        // Get or create character controller
        if (activeCharacters.ContainsKey(characterName.ToLower()))
        {
            controller = activeCharacters[characterName.ToLower()];
        }
        else
        {
            GameObject charObj = Instantiate(characterPrefab, characterContainer);
            charObj.name = $"Character_{characterName}";
            controller = charObj.GetComponent<CharacterController>();

            // Set up RectTransform for UI mode
            if (useUIMode)
            {
                RectTransform rectTransform = charObj.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    // Set anchors to bottom-center
                    rectTransform.anchorMin = new Vector2(0.5f, 0f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0f);
                    rectTransform.pivot = new Vector2(0.5f, 0f);

                    // Set scale
                    rectTransform.localScale = Vector3.one * characterScale;

                    // Reset rotation
                    rectTransform.localRotation = Quaternion.identity;
                }
            }

            activeCharacters[characterName.ToLower()] = controller;
        }

        // Set character data and appearance
        controller.SetCharacter(data, portrait, expression);

        if (useUIMode)
        {
            SetUIPosition(controller, position, true);
        }
        else
        {
            controller.SetPosition(GetWorldPositionVector(position), true);
        }

        controller.Show();
    }

    public void HideCharacter(string characterName)
    {
        if (activeCharacters.ContainsKey(characterName.ToLower()))
        {
            activeCharacters[characterName.ToLower()].Hide();
        }
    }

    public void MoveCharacter(string characterName, string position, float duration = 0.5f)
    {
        if (activeCharacters.ContainsKey(characterName.ToLower()))
        {
            if (useUIMode)
            {
                SetUIPosition(activeCharacters[characterName.ToLower()], position, false);
            }
            else
            {
                activeCharacters[characterName.ToLower()].SetPosition(GetWorldPositionVector(position));
            }
        }
    }

    public void SetCharacterExpression(string characterName, string expression)
    {
        if (activeCharacters.ContainsKey(characterName.ToLower()))
        {
            activeCharacters[characterName.ToLower()].SetExpression(expression);
        }
    }

    public void SetCharacterPortrait(string characterName, string portrait, string expression = null)
    {
        if (activeCharacters.ContainsKey(characterName.ToLower()))
        {
            activeCharacters[characterName.ToLower()].SetPortrait(portrait, expression);
        }
    }

    public void ClearAllCharacters()
    {
        foreach (var controller in activeCharacters.Values)
        {
            if (controller != null)
                Destroy(controller.gameObject);
        }
        activeCharacters.Clear();
    }

    // UI positioning using RectTransform
    private void SetUIPosition(CharacterController controller, string position, bool instant)
    {
        RectTransform rectTransform = controller.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("Character doesn't have RectTransform!");
            return;
        }

        float targetX = GetUIPositionX(position);
        Vector2 targetPosition = new Vector2(targetX, rectTransform.anchoredPosition.y);

        if (instant)
        {
            rectTransform.anchoredPosition = targetPosition;
        }
        else
        {
            controller.SetUIPosition(targetPosition);
        }
    }

    private float GetUIPositionX(string position)
    {
        switch (position.ToLower())
        {
            case "left": return leftPositionX;
            case "center": return centerPositionX;
            case "right": return rightPositionX;
            case "farleft": return farLeftPositionX;
            case "farright": return farRightPositionX;
            default:
                Debug.LogWarning($"Unknown position: {position}, defaulting to center");
                return centerPositionX;
        }
    }

    // World space positioning (for non-UI mode)
    private Vector3 GetWorldPositionVector(string position)
    {
        switch (position.ToLower())
        {
            case "left": return new Vector3(leftPositionX / 100f, 0f, 0f);
            case "center": return new Vector3(centerPositionX / 100f, 0f, 0f);
            case "right": return new Vector3(rightPositionX / 100f, 0f, 0f);
            case "farleft": return new Vector3(farLeftPositionX / 100f, 0f, 0f);
            case "farright": return new Vector3(farRightPositionX / 100f, 0f, 0f);
            default:
                Debug.LogWarning($"Unknown position: {position}, defaulting to center");
                return new Vector3(centerPositionX / 100f, 0f, 0f);
        }
    }

    public IEnumerator MoveCharacterAndWait(string characterName, string position, float duration = 0.5f)
    {
        if (activeCharacters.ContainsKey(characterName.ToLower()))
        {
            CharacterController controller = activeCharacters[characterName.ToLower()];
            if (useUIMode)
            {
                float targetX = GetUIPositionX(position);
                RectTransform rectTransform = controller.GetComponent<RectTransform>();
                Vector2 targetPosition = new Vector2(targetX, rectTransform.anchoredPosition.y);

                // Start and wait for the controller's movement coroutine to finish
                yield return controller.StartCoroutine(controller.MoveToUIPositionCoroutine(targetPosition, duration));
            }
            else
            {
                // This is where a world-space version would go
                yield return null;
            }
        }
    }
}