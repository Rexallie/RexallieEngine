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

    public void SetBackground(string backgroundName, string transition = "instant")
    {
        BackgroundData bgData = backgrounds.Find(b => b.backgroundName == backgroundName);

        if (bgData == null)
        {
            Debug.LogError($"Background not found: {backgroundName}");
            return;
        }

        switch (transition.ToLower())
        {
            case "fade":
                StartCoroutine(FadeToBackground(bgData.sprite));
                break;
            case "crossfade":
                StartCoroutine(CrossfadeToBackground(bgData.sprite));
                break;
            case "instant":
            default:
                backgroundImage.sprite = bgData.sprite;
                currentBackground = bgData.sprite;
                break;
        }
    }

    private IEnumerator FadeToBackground(Sprite newBackground)
    {
        // Fade out current background
        float elapsed = 0f;
        Color color = backgroundImage.color;

        while (elapsed < 1f)
        {
            elapsed += fadeSpeed * Time.deltaTime;
            color.a = Mathf.Lerp(1f, 0f, elapsed);
            backgroundImage.color = color;
            yield return null;
        }

        // Change sprite
        backgroundImage.sprite = newBackground;
        currentBackground = newBackground;

        // Fade in new background
        elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += fadeSpeed * Time.deltaTime;
            color.a = Mathf.Lerp(0f, 1f, elapsed);
            backgroundImage.color = color;
            yield return null;
        }

        color.a = 1f;
        backgroundImage.color = color;
    }

    private IEnumerator CrossfadeToBackground(Sprite newBackground)
    {
        fadeImage.sprite = newBackground;
        fadeImage.gameObject.SetActive(true);

        float elapsed = 0f;
        Color color = fadeImage.color;
        color.a = 0f;
        fadeImage.color = color;

        while (elapsed < 1f)
        {
            elapsed += fadeSpeed * Time.deltaTime;
            color.a = Mathf.Lerp(0f, 1f, elapsed);
            fadeImage.color = color;
            yield return null;
        }

        // Swap sprites
        backgroundImage.sprite = newBackground;
        currentBackground = newBackground;

        color.a = 0f;
        fadeImage.color = color;
        fadeImage.gameObject.SetActive(false);
    }
}

[System.Serializable]
public class BackgroundData
{
    public string backgroundName;
    public Sprite sprite;
}