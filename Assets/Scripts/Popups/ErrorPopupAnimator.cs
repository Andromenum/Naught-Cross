using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class ErrorPopupAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text messageText;

    [Header("Audio")]
    [SerializeField] private string errorSfxId = "error";
    [SerializeField, Range(0f, 1f)] private float errorSfxVolumeScale = 1f;

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

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Image backgroundImage;
    private Coroutine animationRoutine;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        backgroundImage = GetComponent<Image>();

        if (messageText == null)
            messageText = GetComponentInChildren<TMP_Text>(true);

        ForceHiddenImmediate();
    }

    private void OnValidate()
    {
        scaleInDuration = Mathf.Max(0f, scaleInDuration);
        flashDuration = Mathf.Max(0f, flashDuration);
        scaleOutDuration = Mathf.Max(0f, scaleOutDuration);
        flashFrequency = Mathf.Max(0f, flashFrequency);
    }

    public void ShowError(string message)
    {
        if (messageText != null)
            messageText.text = message;

        PlayErrorSfx();

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

    private void PlayErrorSfx()
    {
        if (SFXManager.Instance == null)
            return;

        if (string.IsNullOrWhiteSpace(errorSfxId))
            return;

        SFXManager.Instance.PlayById(errorSfxId, errorSfxVolumeScale);
    }

    private void ForceHiddenImmediate()
    {
        if (rectTransform != null)
            rectTransform.localScale = hiddenScale;

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
        if (rectTransform != null)
            rectTransform.localScale = hiddenScale;

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
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            if (rectTransform != null)
                rectTransform.localScale = Vector3.Lerp(hiddenScale, shownScale, easedT);

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, easedT);

            yield return null;
        }

        if (rectTransform != null)
            rectTransform.localScale = shownScale;

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

        Vector3 startScale = rectTransform != null ? rectTransform.localScale : shownScale;
        float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;

        while (elapsed < scaleOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / scaleOutDuration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            if (rectTransform != null)
                rectTransform.localScale = Vector3.Lerp(startScale, hiddenScale, easedT);

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, easedT);

            yield return null;
        }

        ForceHiddenImmediate();
        animationRoutine = null;
    }
}