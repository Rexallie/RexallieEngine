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

    void Awake()
    {
        textField = GetComponent<TextMeshProUGUI>();
    }

    public void ShowText(string text)
    {
        // Stop any typewriter effect that might already be running.
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }

        // Start the new, simple typewriter effect.
        typewriterCoroutine = StartCoroutine(TypewriterEffect(text));
    }

    private IEnumerator TypewriterEffect(string text)
    {
        // Set the full text on the component, but start with 0 characters visible.
        textField.text = text;
        textField.maxVisibleCharacters = 0;

        // Wait a frame to ensure TMP has generated the text object.
        yield return new WaitForEndOfFrame();

        TMP_TextInfo textInfo = textField.textInfo;
        int totalVisibleCharacters = textInfo.characterCount;
        if (totalVisibleCharacters == 0) yield break;

        for (int i = 0; i < totalVisibleCharacters; i++)
        {
            // Reveal one character.
            textField.maxVisibleCharacters = i + 1;

            // Calculate the current progress (0.0 to 1.0) through the entire text.
            float progress = (float)i / (totalVisibleCharacters - 1);

            // Use an easing function to make the progress curve. An ease-in curve on the progress
            // will make the speed feel like it's easing-out (starting fast and ending slow).
            float easedProgress = progress * progress; // Simple quadratic ease-in

            // Interpolate the speed based on our eased progress.
            float currentSpeed = Mathf.Lerp(fastSpeed, slowSpeed, easedProgress);

            // Wait for a duration based on the current speed.
            yield return new WaitForSeconds(1f / currentSpeed);
        }
    }
}