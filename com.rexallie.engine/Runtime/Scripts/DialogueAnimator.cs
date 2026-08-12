using System.Collections;
using UnityEngine;
using TMPro;
using System.Collections.Generic;

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
    public bool IsWaitingForInlineClick { get; set; }

    void Awake()
    {
        textField = GetComponent<TextMeshProUGUI>();
    }

    public void ShowText(string text, AudioClip voiceBlip = null, bool instant = false)
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }

        IsWaitingForInlineClick = false;
        textField.text = text;

        if (instant)
        {
            // If instant, reveal all characters immediately and stop.
            textField.maxVisibleCharacters = textField.textInfo.characterCount;
            IsAnimating = false;
        }
        else
        {
            // Otherwise, start the typewriter coroutine.
            typewriterCoroutine = StartCoroutine(TypewriterEffect(text, voiceBlip));
        }
    }

    public void SetTypeSpeed(float speed)
    {
        if (speed >= 0.99f)
        {
            fastSpeed = 1500f; // Effectively instant.
            slowSpeed = 1500f;
            return;
        } else
        {
            speed = speed * 150f;
        }

        fastSpeed = speed; // For simplicity, we'll just set the fast speed for now.
                           // You could have separate sliders or logic if desired.
                           // slowSpeed could also be adjusted here, perhaps proportionally.

        fastSpeed = Mathf.Max(5f, speed); // Ensure a minimum speed.

        slowSpeed = Mathf.Max(5f, speed / 3f); // Example: slow speed is a third of fast speed, with a minimum.
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
        IsWaitingForInlineClick = false;
    }

    private struct InlinePause
    {
        public int characterIndex;
        public bool waitForClick;
        public float duration;
    }

    private string ParseInlinePauses(string originalText, out List<InlinePause> pauses)
    {
        pauses = new List<InlinePause>();
        System.Text.StringBuilder cleanTextBuilder = new System.Text.StringBuilder();
        
        int cleanIndex = 0;
        int i = 0;
        while (i < originalText.Length)
        {
            if (originalText[i] == '{')
            {
                int endIdx = originalText.IndexOf('}', i);
                if (endIdx != -1)
                {
                    string tag = originalText.Substring(i + 1, endIdx - i - 1).Trim();
                    if (tag.ToLower() == "w")
                    {
                        pauses.Add(new InlinePause { characterIndex = cleanIndex, waitForClick = true, duration = 0f });
                        i = endIdx + 1;
                        continue;
                    }
                    else if (tag.ToLower().StartsWith("p="))
                    {
                        string valStr = tag.Substring(2).Trim();
                        if (float.TryParse(valStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float duration))
                        {
                            pauses.Add(new InlinePause { characterIndex = cleanIndex, waitForClick = false, duration = duration });
                        }
                        i = endIdx + 1;
                        continue;
                    }
                }
            }
            
            if (originalText[i] == '<')
            {
                int endTag = originalText.IndexOf('>', i);
                if (endTag != -1)
                {
                    cleanTextBuilder.Append(originalText.Substring(i, endTag - i + 1));
                    i = endTag + 1;
                    continue;
                }
            }

            cleanTextBuilder.Append(originalText[i]);
            cleanIndex++;
            i++;
        }

        return cleanTextBuilder.ToString();
    }

    private IEnumerator TypewriterEffect(string text, AudioClip voiceBlip)
    {
        IsAnimating = true; // Signal that we are starting.
        IsWaitingForInlineClick = false;

        List<InlinePause> pauses;
        string cleanText = ParseInlinePauses(text, out pauses);

        textField.text = cleanText;
        textField.maxVisibleCharacters = 0;

        yield return new WaitForEndOfFrame();

        TMP_TextInfo textInfo = textField.textInfo;
        int totalVisibleCharacters = textInfo.characterCount;
        if (totalVisibleCharacters == 0)
        {
            IsAnimating = false;
            yield break;
        }

        int pauseIndex = 0;

        for (int i = 0; i < totalVisibleCharacters; i++)
        {
            textField.maxVisibleCharacters = i + 1;

            if (voiceBlip != null && i % 2 == 0)
            {
                if (AudioManager.Instance != null && AudioManager.Instance.voiceSource != null)
                {
                    AudioManager.Instance.voiceSource.pitch = Random.Range(0.95f, 1.05f);
                    AudioManager.Instance.voiceSource.PlayOneShot(voiceBlip);
                }
            }

            // Check if there is an inline pause to trigger after this character
            while (pauseIndex < pauses.Count && pauses[pauseIndex].characterIndex == i + 1)
            {
                InlinePause pause = pauses[pauseIndex];
                pauseIndex++;

                if (pause.waitForClick)
                {
                    IsWaitingForInlineClick = true;
                    while (IsWaitingForInlineClick)
                    {
                        yield return null;
                    }
                }
                else if (pause.duration > 0f)
                {
                    yield return new WaitForSeconds(pause.duration);
                }
            }

            float progress = (totalVisibleCharacters > 1) ? (float)i / (totalVisibleCharacters - 1) : 1;
            float easedProgress = progress * progress;
            float currentSpeed = Mathf.Lerp(fastSpeed, slowSpeed, easedProgress);

            if (currentSpeed <= 0) currentSpeed = 0.001f;

            yield return new WaitForSeconds(1f / currentSpeed);
        }

        IsAnimating = false; // Signal that we are finished.
    }
}