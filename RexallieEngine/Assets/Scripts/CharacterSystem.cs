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
    public RectTransform characterContainer;
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
    private bool useUIMode = true;

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
        useUIMode = (characterContainer != null);
    }

    public CharacterData GetCharacterData(string characterName)
    {
        return availableCharacters.Find(c => c.characterName.ToLower() == characterName.ToLower());
    }

    public void ShowCharacter(string characterName, string position, string portrait, string expression)
    {
        CharacterController controller = GetOrCreateController(characterName);
        CharacterData data = GetCharacterData(characterName);
        if (data == null)
        {
            Debug.LogError($"Character not found: {characterName}");
            return;
        }

        controller.SetCharacter(data, portrait, expression);

        if (useUIMode)
        {
            SetUIPosition(controller, position, true);
        }
        else
        {
            controller.SetPosition(GetWorldPositionVector(position), true);
        }

        controller.Show(true);
    }

    public void HideCharacter(string characterName)
    {
        if (activeCharacters.ContainsKey(characterName.ToLower()))
        {
            activeCharacters[characterName.ToLower()].Hide(true);
        }
    }

    public void MoveCharacter(string characterName, string position, float duration = 0.5f)
    {
        if (activeCharacters.ContainsKey(characterName.ToLower()))
        {
            CharacterController controller = activeCharacters[characterName.ToLower()];
            if (useUIMode)
            {
                float targetX = GetUIPositionX(position);
                Vector2 targetPos = new Vector2(targetX, controller.GetComponent<RectTransform>().anchoredPosition.y);
                controller.StartCoroutine(controller.MoveToUIPositionCoroutine(targetPos, duration));
            }
            else
            {
                controller.SetPosition(GetWorldPositionVector(position), false);
            }
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
                Vector2 targetPosition = new Vector2(targetX, controller.GetComponent<RectTransform>().anchoredPosition.y);
                yield return controller.StartCoroutine(controller.MoveToUIPositionCoroutine(targetPosition, duration));
            }
            else
            {
                yield return null;
            }
        }
    }

    public IEnumerator ShowCharacterWithEffect(ActionNode action)
    {
        string characterName = action.parameters.GetValueOrDefault("param1", "");
        string positionStr = action.parameters.GetValueOrDefault("param2", "center");
        string portrait = action.parameters.GetValueOrDefault("param3", $"{characterName.ToLower()}_base");
        string expression = action.parameters.GetValueOrDefault("param4", "neutral");

        CharacterController controller = GetOrCreateController(characterName);
        controller.SetCharacter(GetCharacterData(characterName), portrait, expression);

        float fadeDuration = float.Parse(action.parameters.GetValueOrDefault("fadeIn", "0"), System.Globalization.CultureInfo.InvariantCulture);
        string slideFromDir = action.parameters.GetValueOrDefault("slideFrom", "");
        float slideDuration = float.Parse(action.parameters.GetValueOrDefault("slideDuration", fadeDuration.ToString()), System.Globalization.CultureInfo.InvariantCulture);

        Vector2 finalPos = new Vector2(GetUIPositionX(positionStr), controller.GetComponent<RectTransform>().anchoredPosition.y);
        Vector2 startPos = finalPos;

        if (!string.IsNullOrEmpty(slideFromDir))
        {
            startPos = GetOffscreenPosition(finalPos, slideFromDir);
            controller.GetComponent<RectTransform>().anchoredPosition = startPos;
        }

        yield return controller.StartCoroutine(controller.AnimateAppearance(true, fadeDuration, slideDuration, finalPos));
    }

    public IEnumerator HideCharacterWithEffect(ActionNode action)
    {
        string characterName = action.parameters.GetValueOrDefault("param1", "");
        if (!activeCharacters.ContainsKey(characterName.ToLower())) yield break;

        CharacterController controller = activeCharacters[characterName.ToLower()];

        float fadeDuration = float.Parse(action.parameters.GetValueOrDefault("fadeOut", "0"), System.Globalization.CultureInfo.InvariantCulture);
        string slideToDir = action.parameters.GetValueOrDefault("slideTo", "");
        float slideDuration = float.Parse(action.parameters.GetValueOrDefault("slideDuration", fadeDuration.ToString()), System.Globalization.CultureInfo.InvariantCulture);

        Vector2 currentPos = controller.GetComponent<RectTransform>().anchoredPosition;
        Vector2 finalPos = currentPos;

        if (!string.IsNullOrEmpty(slideToDir))
        {
            finalPos = GetOffscreenPosition(currentPos, slideToDir);
        }

        yield return controller.StartCoroutine(controller.AnimateAppearance(false, fadeDuration, slideDuration, finalPos));
    }

    private Vector2 GetOffscreenPosition(Vector2 basePos, string direction)
    {
        float horizontalOffset = (Screen.width / 2f) + (basePos.x * 0.5f) + 200f;

        switch (direction.ToLower())
        {
            case "farleft":
                return new Vector2(-horizontalOffset - 300f, basePos.y);
            case "left":
                return new Vector2(-horizontalOffset, basePos.y);
            case "right":
                return new Vector2(horizontalOffset, basePos.y);
            case "farright":
                return new Vector2(horizontalOffset + 300f, basePos.y);
            default:
                return basePos;
        }
    }

    private CharacterController GetOrCreateController(string characterName)
    {
        if (activeCharacters.ContainsKey(characterName.ToLower()))
        {
            return activeCharacters[characterName.ToLower()];
        }

        GameObject charObj = Instantiate(characterPrefab, characterContainer);
        charObj.name = $"Character_{characterName}";
        CharacterController controller = charObj.GetComponent<CharacterController>();

        RectTransform rectTransform = charObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.localScale = Vector3.one * characterScale;
            rectTransform.localRotation = Quaternion.identity;
        }

        activeCharacters[characterName.ToLower()] = controller;
        return controller;
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

    private void SetUIPosition(CharacterController controller, string position, bool instant)
    {
        RectTransform rectTransform = controller.GetComponent<RectTransform>();
        if (rectTransform == null) return;

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

    private Vector3 GetWorldPositionVector(string position)
    {
        float xPos = 0;
        switch (position.ToLower())
        {
            case "left": xPos = leftPositionX; break;
            case "center": xPos = centerPositionX; break;
            case "right": xPos = rightPositionX; break;
            case "farleft": xPos = farLeftPositionX; break;
            case "farright": xPos = farRightPositionX; break;
            default:
                Debug.LogWarning($"Unknown position: {position}, defaulting to center");
                xPos = centerPositionX;
                break;
        }
        return new Vector3(xPos / 100f, 0f, 0f); // Example conversion
    }

    // This method gathers the state of all active characters.
    public List<CharacterSaveData> GetCharactersState()
    {
        List<CharacterSaveData> characterStates = new List<CharacterSaveData>();

        foreach (var characterPair in activeCharacters)
        {
            if (characterPair.Value.gameObject.activeSelf) // Only save active characters
            {
                CharacterController controller = characterPair.Value;
                characterStates.Add(new CharacterSaveData
                {
                    characterName = controller.GetCharacterName(),
                    // You'll need to implement a way to get the string name of the position
                    // For now, we'll save the exact UI position.
                    anchoredPosition = controller.GetComponent<RectTransform>().anchoredPosition,
                    portrait = controller.GetCurrentPortrait(),
                    expression = controller.GetCurrentExpression()
                });
            }
        }
        return characterStates;
    }

    // This method restores the state of all characters from a save file.
    public void RestoreState(List<CharacterSaveData> characterStates)
    {
        ClearAllCharacters();

        foreach (var charData in characterStates)
        {
            // Use the ShowCharacter function to recreate the character
            ShowCharacter(charData.characterName, "center", charData.portrait, charData.expression);
            // Then, immediately set its exact saved position
            if (activeCharacters.ContainsKey(charData.characterName.ToLower()))
            {
                activeCharacters[charData.characterName.ToLower()].GetComponent<RectTransform>().anchoredPosition = charData.anchoredPosition;
            }
        }
    }
}