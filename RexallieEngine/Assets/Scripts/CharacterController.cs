using System;
using System.Collections;
using UnityEngine;

public class CharacterController : MonoBehaviour
{
    [Header("References - UI Mode")]
    public UnityEngine.UI.Image portraitImage;
    public UnityEngine.UI.Image expressionImage;

    [Header("References - Sprite Mode (Optional)")]
    public SpriteRenderer portraitRenderer;
    public SpriteRenderer expressionRenderer;

    [Header("Settings")]
    public float moveSpeed = 2f;
    public float fadeSpeed = 1f;

    private CharacterData currentCharacter;
    private string currentPortrait;
    private string currentExpression;
    private CanvasGroup canvasGroup;
    private bool useUIMode;

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

    private void Start()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueLineDisplayed += UpdateCharacter;
        }
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

    // ==================== POSITIONING ====================

    // RESTORED: For non-UI world space positioning
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

    // RESTORED: Coroutine for non-UI world space movement
    private IEnumerator MoveToPosition(Vector3 targetPosition)
    {
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPosition;
    }

    // For UI positioning
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
        if (line.speakerID != null && currentCharacter != null && line.speakerID.ToLower() == currentCharacter.characterName.ToLower())
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
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null) yield break;

        while (Vector2.Distance(rectTransform.anchoredPosition, targetPosition) > 0.01f)
        {
            rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }
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
            // ... inside the while loop
            elapsed = Time.time - startTime;
            float linearProgress = Mathf.Clamp01(elapsed / duration);

            // Apply the easing function
            float easedProgress = Easing.EaseOutQuad(linearProgress);

            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, easedProgress);
            yield return null;
            // ...
        }

        rectTransform.anchoredPosition = targetPosition;
    }

    // ==================== ANIMATIONS & VISIBILITY ====================

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
        if (maxDuration <= 0) maxDuration = float.Epsilon;

        float startTime = Time.time;
        float elapsed = 0f;

        while (elapsed < maxDuration)
        {
            // ... inside the while loop
            elapsed = Time.time - startTime;

            // Calculate the linear progress first
            float linearProgress = Mathf.Clamp01(elapsed / maxDuration);

            // Now, apply the easing function to get a curved progress
            float easedProgress = Easing.EaseOutQuad(linearProgress);

            if (fadeDuration > 0)
            {
                float fadeProgress = Mathf.Clamp01(elapsed / fadeDuration);
                // Note: We could ease fade and move separately, but for simplicity we'll ease them together.
                SetAlpha(Mathf.Lerp(startAlpha, endAlpha, easedProgress));
            }

            if (moveDuration > 0)
            {
                float moveProgress = Mathf.Clamp01(elapsed / moveDuration);
                rectTransform.anchoredPosition = Vector2.Lerp(startPosition, moveTargetPosition, easedProgress);
            }

            yield return null;
            // ...
        }

        SetAlpha(endAlpha);
        if (moveDuration > 0)
        {
            rectTransform.anchoredPosition = moveTargetPosition;
        }

        if (!isShowing)
        {
            //gameObject.SetActive(false);
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
        if (useUIMode)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
            }
            else
            {
                if (portraitImage != null) { Color c = portraitImage.color; c.a = alpha; portraitImage.color = c; }
                if (expressionImage != null) { Color c = expressionImage.color; c.a = alpha; expressionImage.color = c; }
            }
        }
        else
        {
            if (portraitRenderer != null) { Color c = portraitRenderer.color; c.a = alpha; portraitRenderer.color = c; }
            if (expressionRenderer != null) { Color c = expressionRenderer.color; c.a = alpha; expressionRenderer.color = c; }
        }
    }

    private IEnumerator FadeToAlpha(float targetAlpha, Action onComplete = null)
    {
        float startAlpha = canvasGroup != null ? canvasGroup.alpha : (useUIMode && portraitImage != null ? portraitImage.color.a : (portraitRenderer != null ? portraitRenderer.color.a : 0f));
        float startTime = Time.time;
        float duration = 1f / fadeSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed = Time.time - startTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
            SetAlpha(alpha);
            yield return null;
        }

        SetAlpha(targetAlpha);
        onComplete?.Invoke();
    }

    public string GetCharacterName() { return currentCharacter.characterName; }
    public string GetCurrentPortrait() { return currentPortrait; }
    public string GetCurrentExpression() { return currentExpression; }
}