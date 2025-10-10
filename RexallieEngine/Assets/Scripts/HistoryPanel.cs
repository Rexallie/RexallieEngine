using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class HistoryPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject logEntryPrefab;
    [SerializeField] private RectTransform contentArea;
    [SerializeField] private ScrollRect scrollRect;

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private float startScale = 0.9f;

    private CanvasGroup canvasGroup;
    private Coroutine animationCoroutine;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        transform.localScale = Vector3.one * startScale;
    }

    public void Show()
    {
        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(ShowPanelCoroutine());
    }

    private IEnumerator ShowPanelCoroutine()
    {
        foreach (Transform child in contentArea)
        {
            Destroy(child.gameObject);
        }

        List<LogEntry> history = DialogueLogManager.Instance.GetHistory();

        foreach (var entry in history)
        {
            GameObject newEntryObj = Instantiate(logEntryPrefab, contentArea);

            // --- THIS IS THE KEY CHANGE ---
            // Get the LogEntryUI component and pass the data to it.
            LogEntryUI logEntry = newEntryObj.GetComponent<LogEntryUI>();
            if (logEntry != null)
            {
                logEntry.SetData(entry.speakerName, entry.dialogueText);
            }
        }

        yield return new WaitForEndOfFrame();

        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }

        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / animationDuration);
            float easedProgress = Easing.EaseOutQuad(progress);

            canvasGroup.alpha = easedProgress;
            transform.localScale = Vector3.one * Mathf.Lerp(startScale, 1f, easedProgress);

            yield return null;
        }

        canvasGroup.alpha = 1f;
        transform.localScale = Vector3.one;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public void Hide()
    {
        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(HidePanelCoroutine());
    }

    private IEnumerator HidePanelCoroutine()
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / animationDuration);
            float easedProgress = Easing.EaseOutQuad(progress);

            canvasGroup.alpha = 1f - easedProgress;
            transform.localScale = Vector3.one * Mathf.Lerp(1f, startScale, easedProgress);

            yield return null;
        }

        canvasGroup.alpha = 0f;
        transform.localScale = Vector3.one * startScale;
    }
}