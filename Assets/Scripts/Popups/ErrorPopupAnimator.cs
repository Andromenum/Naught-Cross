using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class ErrorPopupAnimator : MonoBehaviour
{
    [SerializeField] private RectTransform animatedRoot;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Image backgroundImage;

    [Header("Timing")]
    [SerializeField] private float scaleInDuration = 0.12f;
    [SerializeField] private float flashDuration = 1f;
    [SerializeField] private float scaleOutDuration = 0.12f;

    [Header("Flash")]
    [SerializeField] private float flashFrequency = 8f;
    [SerializeField] private Color flashBrightColor = new Color32(255, 255, 255, 255);
    [SerializeField] private Color flashDarkColor = new Color32(150, 150, 150, 255);

    [Header("Scale")]
    [SerializeField] private Vector3 hiddenScale = Vector3.zero;
    [SerializeField] private Vector3 shownScale = Vector3.one;

    private CanvasGroup canvasGroup;
    private Coroutine animationRoutine;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (animatedRoot == null)
            animatedRoot = transform as RectTransform;

        if (messageText == null)
            messageText = GetComponentInChildren<TMP_Text>(true);

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        ForceHiddenImmediate();
    }

    public void ShowError(string message)
    {
        if (messageText != null)
            messageText.text = message;

        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        animationRoutine = StartCoroutine(ShowRoutine());
    }

    public void ForceHidden()
    {
        if (!isActiveAndEnabled)
        {
            ForceHiddenImmediate();
            return;
        }

        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        ForceHiddenImmediate();
    }

    private void ForceHiddenImmediate()
    {
        if (animatedRoot != null)
            animatedRoot.localScale = hiddenScale;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (backgroundImage != null)
            backgroundImage.color = flashBrightColor;
    }

    private IEnumerator ShowRoutine()
    {
        if (animatedRoot != null)
            animatedRoot.localScale = hiddenScale;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (backgroundImage != null)
            backgroundImage.color = flashBrightColor;

        float elapsed = 0f;

        while (elapsed < scaleInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / scaleInDuration);

            if (animatedRoot != null)
                animatedRoot.localScale = Vector3.Lerp(hiddenScale, shownScale, t);

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

            yield return null;
        }

        if (animatedRoot != null)
            animatedRoot.localScale = shownScale;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        elapsed = 0f;

        while (elapsed < flashDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float wave = 0.5f + 0.5f * Mathf.Sin(elapsed * flashFrequency * Mathf.PI * 2f);

            if (backgroundImage != null)
                backgroundImage.color = Color.Lerp(flashDarkColor, flashBrightColor, wave);

            yield return null;
        }

        if (backgroundImage != null)
            backgroundImage.color = flashBrightColor;

        elapsed = 0f;
        Vector3 startScale = animatedRoot != null ? animatedRoot.localScale : shownScale;
        float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;

        while (elapsed < scaleOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / scaleOutDuration);

            if (animatedRoot != null)
                animatedRoot.localScale = Vector3.Lerp(startScale, hiddenScale, t);

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);

            yield return null;
        }

        ForceHiddenImmediate();
        animationRoutine = null;
    }
}