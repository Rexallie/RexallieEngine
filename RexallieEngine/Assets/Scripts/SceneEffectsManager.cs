using System.Collections;
using UnityEngine;

public class SceneEffectsManager : MonoBehaviour
{
    public static SceneEffectsManager Instance { get; private set; }

    [Header("References")]
    [Tooltip("Assign the 'WorldContainer' RectTransform from your scene here.")]
    public RectTransform worldContainer;

    private Coroutine zoomCoroutine;

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
        // Calculate target scale. 0% = scale 1, 100% = scale 2.
        float targetScale = 1.0f + (percentage / 100.0f);

        // Calculate the target position to pan to. This moves the container
        // in the opposite direction of the focus point, scaled by the zoom level.
        Vector2 targetPosition = -focusPoint * targetScale;

        // If we are resetting, the targets are scale 1 and position zero.
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
            float easedProgress = Easing.EaseOutQuad(progress); // Using our existing Easing script

            worldContainer.localScale = Vector3.Lerp(startScale, Vector3.one * targetScale, easedProgress);
            worldContainer.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, easedProgress);

            yield return null;
        }

        // Snap to the final values to ensure accuracy
        worldContainer.localScale = Vector3.one * targetScale;
        worldContainer.anchoredPosition = targetPosition;
    }
}