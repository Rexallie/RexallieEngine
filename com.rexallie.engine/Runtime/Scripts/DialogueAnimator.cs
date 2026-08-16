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

    // A public property to check if the animation is running.
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

        // If instant or near-instant text speed, reveal immediately without frame delays
        if (instant || fastSpeed >= 1000f)
        {
            List<InlinePause> pauses;
            string cleanText = ParseInlinePauses(text, out pauses);
            textField.text = cleanText;
            textField.maxVisibleCharacters = 99999;
            IsAnimating = false;
        }
        else
        {
            typewriterCoroutine = StartCoroutine(TypewriterEffect(text, voiceBlip));
        }
    }

    public void SetTypeSpeed(float speed)
    {
        if (speed >= 0.98f)
        {
            fastSpeed = 2000f; // Instantly reveals
            slowSpeed = 2000f;
            return;
        }

        // Map normalized slider (0..1) to characters per second (15..200)
        float mappedSpeed = Mathf.Lerp(15f, 200f, speed);
        fastSpeed = mappedSpeed;
        slowSpeed = Mathf.Max(10f, mappedSpeed * 0.5f);
    }

    // Public method to instantly finish the animation.
    public void FinishAnimation()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }

        textField.maxVisibleCharacters = textField.textInfo.characterCount;
        IsAnimating = false;
    }

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
        IsAnimating = true;
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
        float accumulatedTime = 0f;
        int currentCharIndex = 0;

        while (currentCharIndex < totalVisibleCharacters)
        {
            float progress = (totalVisibleCharacters > 1) ? (float)currentCharIndex / (totalVisibleCharacters - 1) : 1;
            float easedProgress = progress * progress;
            float currentSpeed = Mathf.Lerp(fastSpeed, slowSpeed, easedProgress);
            if (currentSpeed <= 0) currentSpeed = 0.001f;
            float timePerChar = 1f / currentSpeed;

            accumulatedTime += Time.deltaTime;

            while (accumulatedTime >= timePerChar && currentCharIndex < totalVisibleCharacters)
            {
                accumulatedTime -= timePerChar;
                currentCharIndex++;
                textField.maxVisibleCharacters = currentCharIndex;

                if (voiceBlip != null && currentCharIndex % 2 == 0)
                {
                    if (AudioManager.Instance != null && AudioManager.Instance.voiceSource != null)
                    {
                        AudioManager.Instance.voiceSource.pitch = Random.Range(0.95f, 1.05f);
                        AudioManager.Instance.voiceSource.PlayOneShot(voiceBlip);
                    }
                }

                // Check for inline pauses
                while (pauseIndex < pauses.Count && pauses[pauseIndex].characterIndex == currentCharIndex)
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
                    accumulatedTime = 0f; // Reset accumulator after pause
                }
            }

            yield return null;
        }

        textField.maxVisibleCharacters = totalVisibleCharacters;
        IsAnimating = false;
    }
}