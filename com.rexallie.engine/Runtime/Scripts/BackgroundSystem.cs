using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ==================== BACKGROUND MANAGER ====================

public class BackgroundManager : MonoBehaviour
{
    public static BackgroundManager Instance { get; private set; }

    [Header("Background Images")]
    public Image backgroundImage;
    public Image fadeImage; // For crossfade transitions

    [Header("Background Database")]
    public List<BackgroundData> backgrounds = new List<BackgroundData>();

    [Header("Transition Settings")]
    public float fadeSpeed = 1f;

    private Sprite currentBackground;

    private string currentBackgroundName;
    private float targetOpacity = 1f;
    private Coroutine transitionCoroutine;
    private Coroutine opacityCoroutine;

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

        if (backgroundImage != null)
        {
            targetOpacity = backgroundImage.color.a;
        }
    }

    public void SetBackground(string backgroundName, string transition = "instant", float? durationOverride = null, float? opacityOverride = null)
    {
        BackgroundData bgData = backgrounds.Find(b => b.backgroundName == backgroundName);

        if (bgData == null)
        {
            Debug.LogError($"Background not found: {backgroundName}");
            return;
        }

        currentBackgroundName = backgroundName; // Store for saving

        if (opacityOverride.HasValue)
        {
            targetOpacity = Mathf.Clamp01(opacityOverride.Value);
        }

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        switch (transition.ToLower())
        {
            case "fade":
                transitionCoroutine = StartCoroutine(FadeToBackground(bgData.sprite, durationOverride));
                break;
            case "crossfade":
                transitionCoroutine = StartCoroutine(CrossfadeToBackground(bgData.sprite, durationOverride));
                break;
            case "instant":
            default:
                backgroundImage.sprite = bgData.sprite;
                currentBackground = bgData.sprite;
                ApplyOpacityImmediate(targetOpacity);
                break;
        }
    }

    public void SetBackgroundOpacity(float opacity, float duration = 0f)
    {
        targetOpacity = Mathf.Clamp01(opacity);
        if (opacityCoroutine != null)
        {
            StopCoroutine(opacityCoroutine);
            opacityCoroutine = null;
        }

        if (duration <= 0.001f)
        {
            ApplyOpacityImmediate(targetOpacity);
        }
        else
        {
            opacityCoroutine = StartCoroutine(AnimateOpacityCoroutine(targetOpacity, duration));
        }
    }

    public IEnumerator SetBackgroundOpacityAndWait(float opacity, float duration)
    {
        targetOpacity = Mathf.Clamp01(opacity);
        if (opacityCoroutine != null)
        {
            StopCoroutine(opacityCoroutine);
            opacityCoroutine = null;
        }

        if (duration <= 0.001f)
        {
            ApplyOpacityImmediate(targetOpacity);
            yield break;
        }

        yield return AnimateOpacityCoroutine(targetOpacity, duration);
    }

    private void ApplyOpacityImmediate(float opacity)
    {
        if (backgroundImage != null)
        {
            Color c = backgroundImage.color;
            c.a = opacity;
            backgroundImage.color = c;
        }
    }

    private IEnumerator AnimateOpacityCoroutine(float endAlpha, float duration)
    {
        if (backgroundImage == null) yield break;

        Color color = backgroundImage.color;
        float startAlpha = color.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            color.a = Mathf.Lerp(startAlpha, endAlpha, Easing.EaseOutQuad(progress));
            backgroundImage.color = color;
            yield return null;
        }

        color.a = endAlpha;
        backgroundImage.color = color;
        opacityCoroutine = null;
    }

    private IEnumerator FadeToBackground(Sprite newBackground, float? durationOverride = null)
    {
        float halfDuration = durationOverride.HasValue && durationOverride.Value > 0f
            ? durationOverride.Value * 0.5f
            : (fadeSpeed > 0f ? 1f / fadeSpeed : 0f);

        // Fade out current background
        float elapsed = 0f;
        Color color = backgroundImage.color;
        float startAlpha = color.a;

        if (halfDuration > 0f)
        {
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                color.a = Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(elapsed / halfDuration));
                backgroundImage.color = color;
                yield return null;
            }
        }

        // Change sprite
        backgroundImage.sprite = newBackground;
        currentBackground = newBackground;

        // Fade in new background
        elapsed = 0f;
        if (halfDuration > 0f)
        {
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                color.a = Mathf.Lerp(0f, targetOpacity, Mathf.Clamp01(elapsed / halfDuration));
                backgroundImage.color = color;
                yield return null;
            }
        }

        color.a = targetOpacity;
        backgroundImage.color = color;
        transitionCoroutine = null;
    }

    private IEnumerator CrossfadeToBackground(Sprite newBackground, float? durationOverride = null)
    {
        float duration = durationOverride.HasValue && durationOverride.Value > 0f
            ? durationOverride.Value
            : (fadeSpeed > 0f ? 1f / fadeSpeed : 0f);

        fadeImage.sprite = newBackground;
        fadeImage.gameObject.SetActive(true);

        float elapsed = 0f;
        Color color = fadeImage.color;
        color.a = 0f;
        fadeImage.color = color;

        if (duration > 0f)
        {
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                color.a = Mathf.Lerp(0f, targetOpacity, Mathf.Clamp01(elapsed / duration));
                fadeImage.color = color;
                yield return null;
            }
        }

        // Swap sprites
        backgroundImage.sprite = newBackground;
        currentBackground = newBackground;
        ApplyOpacityImmediate(targetOpacity);

        color.a = 0f;
        fadeImage.color = color;
        fadeImage.gameObject.SetActive(false);
        transitionCoroutine = null;
    }

    // Add these new methods
    public string GetCurrentBackgroundName()
    {
        return currentBackgroundName;
    }

    public float GetCurrentBackgroundOpacity()
    {
        return targetOpacity;
    }

    public void RestoreState(string backgroundName, float opacity = 1f)
    {
        targetOpacity = Mathf.Clamp01(opacity);
        if (!string.IsNullOrEmpty(backgroundName))
        {
            SetBackground(backgroundName, "instant", null, targetOpacity);
        }
        else
        {
            ApplyOpacityImmediate(targetOpacity);
        }
    }
}

[System.Serializable]
public class BackgroundData
{
    public string backgroundName;
    public Sprite sprite;
}