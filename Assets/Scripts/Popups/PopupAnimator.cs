using System.Collections;
using UnityEngine;

public class PopupAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform panel;
    private CanvasGroup panelGroup;
    [SerializeField] private CanvasGroup darkTintGroup;

    [Header("Open Animation")]
    [SerializeField] private float openDuration = 0.25f;
    [SerializeField] private float openStartScale = 0.5f;

    [Header("Close Animation")]
    [SerializeField] private float closeDuration = 0.14f;
    [SerializeField] private float closeEndScale = 0.85f;

    [Header("Tint")]
    [SerializeField] private float tintTargetAlpha = 1f;

    private Coroutine animationRoutine;

    private void Awake()
    {
        panelGroup = panel.GetComponent<CanvasGroup>();
    }

    public void PlayOpen()
    {
        gameObject.SetActive(true);

        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        animationRoutine = StartCoroutine(OpenRoutine());
    }

    public void PlayClose()
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        animationRoutine = StartCoroutine(CloseRoutine());
    }

    public void ForceShown()
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        gameObject.SetActive(true);

        if (panel != null)
            panel.localScale = Vector3.one;

        if (panelGroup != null)
            panelGroup.alpha = 1f;

        if (darkTintGroup != null)
            darkTintGroup.alpha = tintTargetAlpha;

        animationRoutine = null;
    }

    public void ForceHidden()
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        if (panel != null)
            panel.localScale = Vector3.one;

        if (panelGroup != null)
            panelGroup.alpha = 0f;

        if (darkTintGroup != null)
            darkTintGroup.alpha = 0f;

        gameObject.SetActive(false);
        animationRoutine = null;
    }

    private IEnumerator OpenRoutine()
    {
        if (panel != null)
            panel.localScale = Vector3.one * openStartScale;

        if (panelGroup != null)
            panelGroup.alpha = 0f;

        if (darkTintGroup != null)
            darkTintGroup.alpha = 0f;

        if (openDuration <= 0f)
        {
            ForceShown();
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < openDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / openDuration);
            float eased = EaseOutBack(t);

            if (panel != null)
                panel.localScale = Vector3.LerpUnclamped(
                    Vector3.one * openStartScale,
                    Vector3.one,
                    eased);

            if (panelGroup != null)
                panelGroup.alpha = t;

            if (darkTintGroup != null)
                darkTintGroup.alpha = Mathf.Lerp(0f, tintTargetAlpha, t);

            yield return null;
        }

        ForceShown();
    }

    private IEnumerator CloseRoutine()
    {
        Vector3 startScale = panel != null ? panel.localScale : Vector3.one;
        float startPanelAlpha = panelGroup != null ? panelGroup.alpha : 1f;
        float startTintAlpha = darkTintGroup != null ? darkTintGroup.alpha : tintTargetAlpha;

        if (closeDuration <= 0f)
        {
            ForceHidden();
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < closeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / closeDuration);

            if (panel != null)
                panel.localScale = Vector3.Lerp(startScale, Vector3.one * closeEndScale, t);

            if (panelGroup != null)
                panelGroup.alpha = Mathf.Lerp(startPanelAlpha, 0f, t);

            if (darkTintGroup != null)
                darkTintGroup.alpha = Mathf.Lerp(startTintAlpha, 0f, t);

            yield return null;
        }

        ForceHidden();
    }

    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        float x = t - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }
}