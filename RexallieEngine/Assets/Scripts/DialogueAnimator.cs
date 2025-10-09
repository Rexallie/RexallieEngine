using System.Collections;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class DialogueAnimator : MonoBehaviour
{
    private TextMeshProUGUI textField;
    private Coroutine typewriterCoroutine;

    [Header("Typewriter Settings")]
    [Tooltip("The speed at the beginning of the line (characters per second).")]
    public float fastSpeed = 50f;
    [Tooltip("The speed at the end of the line (characters per second).")]
    public float slowSpeed = 15f;

    // NEW: A public property to check if the animation is running.
    public bool IsAnimating { get; private set; }

    void Awake()
    {
        textField = GetComponent<TextMeshProUGUI>();
    }

    public void ShowText(string text)
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }
        typewriterCoroutine = StartCoroutine(TypewriterEffect(text));
    }

    // NEW: A public method to instantly finish the animation.
    public void FinishAnimation()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }

        // Instantly reveal all characters.
        textField.maxVisibleCharacters = textField.textInfo.characterCount;
        IsAnimating = false;
    }

    // Add this new public method inside your DialogueAnimator class
    public void Clear()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }
        textField.text = string.Empty;
        IsAnimating = false;
    }

    private IEnumerator TypewriterEffect(string text)
    {
        IsAnimating = true; // Signal that we are starting.

        textField.text = text;
        textField.maxVisibleCharacters = 0;

        yield return new WaitForEndOfFrame();

        TMP_TextInfo textInfo = textField.textInfo;
        int totalVisibleCharacters = textInfo.characterCount;
        if (totalVisibleCharacters == 0)
        {
            IsAnimating = false;
            yield break;
        }

        for (int i = 0; i < totalVisibleCharacters; i++)
        {
            textField.maxVisibleCharacters = i + 1;

            float progress = (totalVisibleCharacters > 1) ? (float)i / (totalVisibleCharacters - 1) : 1;
            float easedProgress = progress * progress;
            float currentSpeed = Mathf.Lerp(fastSpeed, slowSpeed, easedProgress);

            // Ensure speed is not zero to avoid infinite wait
            if (currentSpeed <= 0) currentSpeed = 0.001f;

            yield return new WaitForSeconds(1f / currentSpeed);
        }

        IsAnimating = false; // Signal that we are finished.
    }
}