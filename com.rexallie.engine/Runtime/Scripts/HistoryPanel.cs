using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class HistoryPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject logEntryPrefab;
    [SerializeField] private RectTransform contentArea;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Button closeButton;

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

    void Start()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Hide);
        }
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

        List<LogEntry> historyLog = DialogueLogManager.Instance.GetHistory();
        List<HistoryState> historyStates = HistoryManager.Instance.GetHistory();

        for (int i = 0; i < historyLog.Count; i++)
        {
            var entry = historyLog[i];
            HistoryState state = i < historyStates.Count ? historyStates[i] : null;

            GameObject newEntryObj = Instantiate(logEntryPrefab, contentArea);

            // --- THIS IS THE KEY CHANGE ---
            // Get the LogEntryUI component and pass the data to it.
            LogEntryUI logEntry = newEntryObj.GetComponent<LogEntryUI>();
            if (logEntry != null)
            {
                logEntry.SetData(entry.speakerName, entry.dialogueText, () =>
                {
                    if (state != null)
                    {
                        HistoryManager.Instance.RollbackToState(state);
                        Hide();
                    }
                });
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

        if (closeButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
        }
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

    void Update()
    {
        if (canvasGroup != null && canvasGroup.interactable)
        {
            UpdateScrollPositionToSelected(scrollRect, contentArea);
        }
    }

    private void UpdateScrollPositionToSelected(ScrollRect scrollRect, RectTransform contentArea)
    {
        if (scrollRect == null || contentArea == null || EventSystem.current == null) return;

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null || !selected.transform.IsChildOf(contentArea)) return;

        RectTransform selectedRect = selected.GetComponent<RectTransform>();
        if (selectedRect == null) return;

        Vector3 selectedPos = contentArea.InverseTransformPoint(selectedRect.position);
        float selectedY = selectedPos.y;
        float selectedHeight = selectedRect.rect.height;

        RectTransform viewportRect = scrollRect.viewport != null ? scrollRect.viewport : scrollRect.GetComponent<RectTransform>();
        float viewportHeight = viewportRect.rect.height;

        float contentHeight = contentArea.rect.height;
        if (contentHeight <= viewportHeight) return;

        float currentScrollY = contentArea.anchoredPosition.y;
        
        float topBoundary = -selectedY - (selectedHeight * 0.5f);
        float bottomBoundary = -selectedY + (selectedHeight * 0.5f);

        if (topBoundary < currentScrollY)
        {
            Vector2 pos = contentArea.anchoredPosition;
            pos.y = topBoundary;
            contentArea.anchoredPosition = pos;
        }
        else if (bottomBoundary > currentScrollY + viewportHeight)
        {
            Vector2 pos = contentArea.anchoredPosition;
            pos.y = bottomBoundary - viewportHeight;
            contentArea.anchoredPosition = pos;
        }
    }
}