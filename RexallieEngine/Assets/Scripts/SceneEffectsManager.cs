using System.Collections;
using UnityEngine;

public class SceneEffectsManager : MonoBehaviour
{
    public static SceneEffectsManager Instance { get; private set; }

    [Header("References")]
    public RectTransform worldContainer;

    private Coroutine zoomCoroutine;

    void Awake()
    {
        if (Instance == null) { Instance = this; } else { Destroy(gameObject); }
    }

    // --- STATE MANAGEMENT ---

    public SceneEffectsSaveData GetState()
    {
        return new SceneEffectsSaveData
        {
            worldContainerPosition = worldContainer.anchoredPosition,
            worldContainerScale = worldContainer.localScale
        };
    }

    public void RestoreState(SceneEffectsSaveData data)
    {
        // Instantly set the position and scale without animation
        worldContainer.anchoredPosition = data.worldContainerPosition;
        worldContainer.localScale = data.worldContainerScale;
    }

    // --- ANIMATIONS ---

    public void Zoom(Vector2 focusPoint, float percentage, float duration)
    {
        if (zoomCoroutine != null)
        {
            StopCoroutine(zoomCoroutine);
        }
        zoomCoroutine = StartCoroutine(ZoomCoroutine(focusPoint, percentage, duration));
    }

    private IEnumerator ZoomCoroutine(Vector2 focusPoint, float percentage, float duration)
    {
        float targetScale = 1.0f + (percentage / 100.0f);
        Vector2 targetPosition = -focusPoint * targetScale;

        if (percentage == 0)
        {
            targetScale = 1f;
            targetPosition = Vector2.zero;
        }

        Vector3 startScale = worldContainer.localScale;
        Vector2 startPosition = worldContainer.anchoredPosition;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = Easing.EaseOutQuad(progress);

            worldContainer.localScale = Vector3.Lerp(startScale, Vector3.one * targetScale, easedProgress);
            worldContainer.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, easedProgress);

            yield return null;
        }

        worldContainer.localScale = Vector3.one * targetScale;
        worldContainer.anchoredPosition = targetPosition;
    }

    public Coroutine Shake(float duration, float magnitude)
    {
        return StartCoroutine(ShakeCoroutine(duration, magnitude));
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        Vector2 originalPos = worldContainer.anchoredPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            worldContainer.anchoredPosition = new Vector2(originalPos.x + x, originalPos.y + y);
            elapsed += Time.deltaTime;
            yield return null;
        }

        worldContainer.anchoredPosition = originalPos;
    }
}