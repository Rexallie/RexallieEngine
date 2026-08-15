using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CharacterController : MonoBehaviour
{
    [Header("References - UI Mode")]
    public Image portraitImage;
    public Image expressionImage;

    [Header("References - Sprite Mode (Optional)")]
    public SpriteRenderer portraitRenderer;
    public SpriteRenderer expressionRenderer;

    [Header("Settings")]
    public float moveSpeed = 2f;
    public float fadeSpeed = 1f;

    [Header("Breathing Animation")]
    public bool isBreathing = true;
    public float breatheSpeed = 1.5f;       // Oscillations frequency
    public float breatheIntensityY = 0.015f; // Vertical stretch
    public float breatheIntensityX = 0.005f; // Horizontal squash

    [Header("Highlight Settings")]
    [Tooltip("The color tint to apply to the character when they are not speaking.")]
    [SerializeField] private Color dimColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [Tooltip("The duration of the highlight/dim animation in seconds.")]
    [SerializeField] private float highlightFadeDuration = 0.25f;

    private CharacterData currentCharacter;
    private string currentPortrait;
    private string currentExpression;
    private CanvasGroup canvasGroup;
    private bool useUIMode;
    private Coroutine highlightCoroutine;

    void Awake()
    {
        useUIMode = (portraitImage != null && expressionImage != null);

        if (!useUIMode && (portraitRenderer == null || expressionRenderer == null))
        {
            Debug.LogError("CharacterController requires either UI Images OR SpriteRenderers!");
        }

        if (useUIMode)
        {
            expressionImage.transform.SetAsLastSibling();
        }
        else if (portraitRenderer != null && expressionRenderer != null)
        {
            expressionRenderer.sortingOrder = portraitRenderer.sortingOrder + 1;
        }

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null && useUIMode)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private Vector3 baseScale = Vector3.one;

    private void Start()
    {
        baseScale = transform.localScale;

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueLineDisplayed += UpdateCharacter;
        }
    }

    public void RestoreBaseScale()
    {
        transform.localScale = baseScale;
    }

    void OnDestroy()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueLineDisplayed -= UpdateCharacter;
        }
    }

    public void SetHighlightState(bool isHighlighted)
    {
        if (!gameObject.activeInHierarchy)
        {
            Color targetColor = isHighlighted ? Color.white : dimColor;
            if (portraitImage != null) portraitImage.color = targetColor;
            if (expressionImage != null) expressionImage.color = targetColor;
            return;
        }

        if (highlightCoroutine != null)
        {
            StopCoroutine(highlightCoroutine);
        }
        highlightCoroutine = StartCoroutine(AnimateHighlight(isHighlighted));
    }

    private IEnumerator AnimateHighlight(bool isHighlighted)
    {
        Color targetColor = isHighlighted ? Color.white : dimColor;

        Color startPortraitColor = portraitImage != null ? portraitImage.color : Color.clear;
        Color startExpressionColor = expressionImage != null ? expressionImage.color : Color.clear;

        float elapsed = 0f;
        while (elapsed < highlightFadeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / highlightFadeDuration);
            float easedProgress = Easing.EaseOutQuad(progress);

            if (portraitImage != null)
            {
                portraitImage.color = Color.Lerp(startPortraitColor, targetColor, easedProgress);
            }
            if (expressionImage != null)
            {
                expressionImage.color = Color.Lerp(startExpressionColor, targetColor, easedProgress);
            }

            yield return null;
        }

        if (portraitImage != null) portraitImage.color = targetColor;
        if (expressionImage != null) expressionImage.color = targetColor;
    }

    public void SetCharacter(CharacterData character, string portrait, string expression)
    {
        currentCharacter = character;
        currentPortrait = portrait;
        currentExpression = expression;
        UpdateSprites();
    }

    public void SetExpression(string expression)
    {
        currentExpression = expression;
        UpdateExpressionSprite();
    }

    public void SetPortrait(string portrait, string expression = null)
    {
        currentPortrait = portrait;
        if (!string.IsNullOrEmpty(expression))
            currentExpression = expression;
        UpdateSprites();
    }

    private void UpdateSprites()
    {
        UpdatePortraitSprite();
        UpdateExpressionSprite();
    }

    private void UpdatePortraitSprite()
    {
        if (currentCharacter == null) return;
        Sprite newPortrait = currentCharacter.GetPortrait(currentPortrait);
        if (newPortrait != null)
        {
            if (useUIMode && portraitImage != null) portraitImage.sprite = newPortrait;
            else if (portraitRenderer != null) portraitRenderer.sprite = newPortrait;
        }
    }

    private void UpdateExpressionSprite()
    {
        if (currentCharacter == null) return;
        Sprite newExpression = currentCharacter.GetExpression(currentExpression);
        if (newExpression != null)
        {
            if (useUIMode && expressionImage != null) expressionImage.sprite = newExpression;
            else if (expressionRenderer != null) expressionRenderer.sprite = newExpression;
        }
    }

    public void SetPosition(Vector3 position, bool instant = false)
    {
        if (instant)
        {
            transform.position = position;
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(MoveToPosition(position));
        }
    }

    private IEnumerator MoveToPosition(Vector3 targetPosition)
    {
        // Note: This is a simple linear move for world-space objects.
        // Easing could be added here similarly to MoveToUIPositionCoroutine if needed.
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPosition;
    }

    public void SetUIPosition(Vector2 anchoredPosition, bool instant = false)
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null) return;

        if (instant)
        {
            rectTransform.anchoredPosition = anchoredPosition;
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(MoveToUIPosition(anchoredPosition));
        }
    }

    private void UpdateCharacter(DialogueLine line)
    {
        if (line.speakerID != null && currentCharacter != null && line.speakerID.ToLower() == currentCharacter.characterID.ToLower())
        {
            if (!string.IsNullOrEmpty(line.portrait) && line.portrait.ToLower() != currentPortrait.ToLower())
            {
                SetPortrait(line.portrait, line.expression);
            }
            else if (!string.IsNullOrEmpty(line.expression) && line.expression.ToLower() != currentExpression.ToLower())
            {
                SetExpression(line.expression);
            }
        }
    }

    private IEnumerator MoveToUIPosition(Vector2 targetPosition)
    {
        // This is a simple linear move. The timed coroutine below is used for VNS commands.
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null) yield break;

        while (Vector2.Distance(rectTransform.anchoredPosition, targetPosition) > 0.01f)
        {
            if (this == null || rectTransform == null) yield break;
            rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }
        if (this == null || rectTransform == null) yield break;
        rectTransform.anchoredPosition = targetPosition;
    }

    public IEnumerator MoveToUIPositionCoroutine(Vector2 targetPosition, float duration)
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null) yield break;

        Vector2 startPosition = rectTransform.anchoredPosition;
        float startTime = Time.time;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (this == null || rectTransform == null) yield break;
            elapsed = Time.time - startTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            // --- EASING APPLIED ---
            float easedProgress = Easing.EaseOutQuad(progress);

            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, easedProgress);
            yield return null;
        }

        if (this == null || rectTransform == null) yield break;
        rectTransform.anchoredPosition = targetPosition;
    }

    public IEnumerator AnimateAppearance(bool isShowing, float fadeDuration, float moveDuration, Vector2 moveTargetPosition)
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null) yield break;

        if (isShowing)
        {
            SetAlpha(0f);
            gameObject.SetActive(true);
        }

        float startAlpha = isShowing ? 0f : canvasGroup.alpha;
        float endAlpha = isShowing ? 1f : 0f;
        Vector2 startPosition = rectTransform.anchoredPosition;

        float maxDuration = Mathf.Max(fadeDuration, moveDuration);
        if (maxDuration <= 0.001f)
        {
            SetAlpha(endAlpha);
            rectTransform.anchoredPosition = moveTargetPosition;
            if (!isShowing)
            {
                gameObject.SetActive(false);
            }
            yield break;
        }

        float startTime = Time.time;
        float elapsed = 0f;

        while (elapsed < maxDuration)
        {
            if (this == null || rectTransform == null) yield break;
            elapsed = Time.time - startTime;

            if (fadeDuration > 0)
            {
                float fadeProgress = Mathf.Clamp01(elapsed / fadeDuration);
                float easedFade = Easing.EaseOutQuad(fadeProgress);
                SetAlpha(Mathf.Lerp(startAlpha, endAlpha, easedFade));
            }

            if (moveDuration > 0)
            {
                float moveProgress = Mathf.Clamp01(elapsed / moveDuration);
                float easedMove = Easing.EaseOutQuad(moveProgress);
                rectTransform.anchoredPosition = Vector2.Lerp(startPosition, moveTargetPosition, easedMove);
            }

            yield return null;
        }

        if (this == null || rectTransform == null) yield break;
        SetAlpha(endAlpha);
        rectTransform.anchoredPosition = moveTargetPosition;

        if (!isShowing)
        {
            gameObject.SetActive(false);
        }
    }

    public void Show(bool instant = false)
    {
        gameObject.SetActive(true);
        if (instant) SetAlpha(1f);
        else
        {
            StopAllCoroutines();
            StartCoroutine(FadeToAlpha(1f));
        }
    }

    public void Hide(bool instant = false)
    {
        if (instant) gameObject.SetActive(false);
        else
        {
            StopAllCoroutines();
            StartCoroutine(FadeToAlpha(0f, () => gameObject.SetActive(false)));
        }
    }

    private void SetAlpha(float alpha)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }
    }

    private IEnumerator FadeToAlpha(float targetAlpha, Action onComplete = null)
    {
        float startAlpha = canvasGroup.alpha;
        float startTime = Time.time;
        float duration = 1f / fadeSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (this == null || canvasGroup == null) yield break;
            elapsed = Time.time - startTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            // --- EASING APPLIED ---
            float easedProgress = Easing.EaseOutQuad(progress);
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, easedProgress);
            SetAlpha(alpha);
            yield return null;
        }

        if (this == null || canvasGroup == null) yield break;
        SetAlpha(targetAlpha);
        onComplete?.Invoke();
    }

    public string GetCharacterName() { return currentCharacter.characterID; }
    public string GetCurrentPortrait() { return currentPortrait; }
    public string GetCurrentExpression() { return currentExpression; }

    private float breatheTime = 0f;

    void Update()
    {
        if (isBreathing)
        {
            breatheTime += Time.deltaTime * breatheSpeed;
            float scaleY = 1.0f + Mathf.Sin(breatheTime) * breatheIntensityY;
            float scaleX = 1.0f - Mathf.Sin(breatheTime) * breatheIntensityX;

            transform.localScale = new Vector3(baseScale.x * scaleX, baseScale.y * scaleY, baseScale.z);
        }
    }
}