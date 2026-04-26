using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonTooltip : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler,
    ISelectHandler,
    IDeselectHandler
{
    [Header("Tooltip")]
    [SerializeField] private TMP_Text tooltipText;

    [TextArea(2, 5)]
    [SerializeField]
    private string message =
        "Hard Mode: each player has a shrinking turn timer. Expect rotating boards, lights-out moments, and chaotic pressure.";

    [Header("Fade")]
    [SerializeField] private bool useFade = true;
    [SerializeField] private float fadeDuration = 0.12f;

    private CanvasGroup tooltipCanvasGroup;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        tooltipCanvasGroup = GetComponent<CanvasGroup>();

        if (tooltipCanvasGroup == null)
            tooltipCanvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (tooltipText == null)
            tooltipText = GetComponentInChildren<TMP_Text>(true);

        HideImmediate();
    }

    private void OnDisable()
    {
        HideImmediate();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        HideTooltip();
    }

    public void OnSelect(BaseEventData eventData)
    {
        ShowTooltip();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        HideTooltip();
    }

    public void ShowTooltip()
    {
        if (tooltipText != null)
            tooltipText.text = message;

        gameObject.SetActive(true);

        if (!useFade || tooltipCanvasGroup == null || fadeDuration <= 0f)
        {
            if (tooltipCanvasGroup != null)
                tooltipCanvasGroup.alpha = 1f;

            return;
        }

        StartFade(1f);
    }

    public void HideTooltip()
    {
        if (!useFade || tooltipCanvasGroup == null || fadeDuration <= 0f)
        {
            HideImmediate();
            return;
        }

        StartFade(0f);
    }

    private void StartFade(float targetAlpha)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha));
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        if (tooltipCanvasGroup == null)
            yield break;

        float startAlpha = tooltipCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            tooltipCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, easedT);

            yield return null;
        }

        tooltipCanvasGroup.alpha = targetAlpha;

        if (Mathf.Approximately(targetAlpha, 0f))
            gameObject.SetActive(false);

        fadeRoutine = null;
    }

    private void HideImmediate()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        if (tooltipCanvasGroup != null)
            tooltipCanvasGroup.alpha = 0f;

        gameObject.SetActive(false);
    }
}