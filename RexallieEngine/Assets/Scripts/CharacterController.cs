using System;
using System.Collections;
using UnityEngine;

public class CharacterController : MonoBehaviour
{
    [Header("References - UI Mode")]
    public UnityEngine.UI.Image portraitImage; // Body/outfit layer
    public UnityEngine.UI.Image expressionImage; // Face overlay layer

    [Header("References - Sprite Mode (Optional)")]
    public SpriteRenderer portraitRenderer; // Body/outfit layer
    public SpriteRenderer expressionRenderer; // Face overlay layer

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
        // Detect which mode we're using
        useUIMode = (portraitImage != null && expressionImage != null);

        if (!useUIMode && (portraitRenderer == null || expressionRenderer == null))
        {
            Debug.LogError("CharacterController requires either UI Images OR SpriteRenderers!");
        }

        // Make sure expression is rendered on top
        if (useUIMode)
        {
            // For UI, we use hierarchy order or set it explicitly
            expressionImage.transform.SetAsLastSibling();
        }
        else if (portraitRenderer != null && expressionRenderer != null)
        {
            expressionRenderer.sortingOrder = portraitRenderer.sortingOrder + 1;
        }

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null && useUIMode)
        {
            // Add CanvasGroup for UI mode if not present
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        DialogueManager.Instance.OnDialogueLineDisplayed += UpdateCharacter;
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
        if (currentCharacter == null)
            return;

        Sprite newPortrait = currentCharacter.GetPortrait(currentPortrait);
        if (newPortrait != null)
        {
            if (useUIMode && portraitImage != null)
            {
                portraitImage.sprite = newPortrait;
            }
            else if (portraitRenderer != null)
            {
                portraitRenderer.sprite = newPortrait;
            }
        }
    }

    private void UpdateExpressionSprite()
    {
        if (currentCharacter == null)
            return;

        Sprite newExpression = currentCharacter.GetExpression(currentExpression);
        if (newExpression != null)
        {
            if (useUIMode && expressionImage != null)
            {
                expressionImage.sprite = newExpression;
            }
            else if (expressionRenderer != null)
            {
                expressionRenderer.sprite = newExpression;
            }
        }
    }

    // Position management
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

    // UI Position management (using RectTransform)
    public void SetUIPosition(Vector2 anchoredPosition, bool instant = false)
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("SetUIPosition called but no RectTransform found!");
            return;
        }

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
        // Check if this character needs to change portrait/expression
        if (line.speaker != null && currentCharacter != null && line.speaker.ToLower() == currentCharacter.characterName.ToLower())
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

    // This new public method will be used for moves that we need to wait for.
    public IEnumerator MoveToUIPositionCoroutine(Vector2 targetPosition, float duration)
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null) yield break;

        Vector2 startPosition = rectTransform.anchoredPosition;
        float elapsed = 0f;

        // Use the duration parameter to control the speed of the movement
        while (elapsed < duration)
        {
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        // Ensure the character is exactly at the target position when done.
        rectTransform.anchoredPosition = targetPosition;
    }

    private System.Collections.IEnumerator MoveToPosition(Vector3 targetPosition)
    {
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPosition;
    }

    private System.Collections.IEnumerator MoveToUIPosition(Vector2 targetPosition)
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

    // Visibility management
    public void Show(bool instant = false)
    {
        gameObject.SetActive(true);

        if (instant)
        {
            SetAlpha(1f);
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(FadeToAlpha(1f));
        }
    }

    public void Hide(bool instant = false)
    {
        if (instant)
        {
            gameObject.SetActive(false);
        }
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
            // UI Mode - use CanvasGroup or Image colors
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
            }
            else
            {
                if (portraitImage != null)
                {
                    Color color = portraitImage.color;
                    color.a = alpha;
                    portraitImage.color = color;
                }

                if (expressionImage != null)
                {
                    Color color = expressionImage.color;
                    color.a = alpha;
                    expressionImage.color = color;
                }
            }
        }
        else
        {
            // Sprite Renderer Mode
            if (portraitRenderer != null)
            {
                Color color = portraitRenderer.color;
                color.a = alpha;
                portraitRenderer.color = color;
            }

            if (expressionRenderer != null)
            {
                Color color = expressionRenderer.color;
                color.a = alpha;
                expressionRenderer.color = color;
            }
        }
    }

    private System.Collections.IEnumerator FadeToAlpha(float targetAlpha, Action onComplete = null)
    {
        float startAlpha = canvasGroup != null ? canvasGroup.alpha :
                          (useUIMode && portraitImage != null ? portraitImage.color.a :
                          (portraitRenderer != null ? portraitRenderer.color.a : 0f));
        float elapsed = 0f;

        while (elapsed < 1f)
        {
            elapsed += fadeSpeed * Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed);
            SetAlpha(alpha);
            yield return null;
        }

        SetAlpha(targetAlpha);
        onComplete?.Invoke();
    }
}
