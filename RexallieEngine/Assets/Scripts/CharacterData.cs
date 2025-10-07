using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Character", menuName = "Visual Novel/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Basic Info")]
    public string characterName;
    public Color nameColor = Color.white;

    [Header("Base Portraits (Body/Outfit)")]
    public List<Portrait> portraits = new List<Portrait>();

    [Header("Expressions (Face Overlays)")]
    public List<ExpressionOverlay> expressions = new List<ExpressionOverlay>();

    [Header("Audio")]
    public AudioClip voiceBlip; // Optional: text blip sound for this character

    // Helper method to get a specific portrait
    public Sprite GetPortrait(string portraitName)
    {
        Portrait portrait = portraits.Find(p => p.portraitName == portraitName);
        if (portrait != null)
            return portrait.sprite;

        Debug.LogWarning($"Portrait not found: {portraitName} for character {characterName} (this can also appear if you're using a portrait name that is different from the sprite name)");
        return null;
    }

    // Helper method to get a specific expression overlay
    public Sprite GetExpression(string expressionName)
    {
        ExpressionOverlay expression = expressions.Find(e => e.expressionName == expressionName);
        if (expression != null)
            return expression.sprite;

        Debug.LogWarning($"Expression not found: {expressionName} for character {characterName} (this can also appear if you're using an expression name that is different from the sprite name)");
        return null;
    }

    // Get default portrait
    public Sprite GetDefaultPortrait()
    {
        if (portraits.Count > 0)
            return portraits[0].sprite;
        return null;
    }

    // Get default expression
    public Sprite GetDefaultExpression()
    {
        if (expressions.Count > 0)
            return expressions[0].sprite;
        return null;
    }
}

[Serializable]
public class Portrait
{
    public string portraitName; // e.g., "alice_school_uniform", "alice_casual", "alice_party_dress"
    public Sprite sprite; // The body/outfit sprite
}

[Serializable]
public class ExpressionOverlay
{
    public string expressionName; // e.g., "happy", "sad", "angry", "surprised"
    public Sprite sprite; // The facial expression overlay
}