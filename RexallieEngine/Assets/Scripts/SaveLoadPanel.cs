using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class SaveLoadPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private RectTransform contentArea;
    [SerializeField] private Button closeButton;

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private float startScale = 0.9f;

    private bool isSaveMode;
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
        closeButton.onClick.AddListener(Hide);
    }

    public void Show(bool isSaving)
    {
        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(ShowPanelCoroutine(isSaving));
    }

    public void Hide()
    {
        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(HidePanelCoroutine());
    }

    // --- NEW: This method will be called to refresh the save slots ---
    public void Refresh()
    {
        PopulateSlots();
    }

    private void PopulateSlots()
    {
        foreach (Transform child in contentArea)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < SaveManager.Instance.MaxSaveSlots; i++)
        {
            GameObject newSlot = Instantiate(slotPrefab, contentArea);
            SaveMetadata metadata = SaveManager.Instance.GetSaveMetadata(i);

            SaveSlotUI slotUI = newSlot.GetComponent<SaveSlotUI>();
            slotUI.Configure(i, isSaveMode, metadata, this);
        }
    }

    private IEnumerator ShowPanelCoroutine(bool isSaving)
    {
        isSaveMode = isSaving;
        PopulateSlots();

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