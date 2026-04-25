using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class UIFadeInOverlay : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private bool playOnStart = true;

    private CanvasGroup canvasGroup;
    private Coroutine fadeRoutine;

    public float FadeDuration => fadeDuration;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        canvasGroup.alpha = 1f;
        SetRaycastBlocking(true);
    }

    private void Start()
    {
        if (playOnStart)
            PlayFadeIn();
    }

    public void PlayFadeIn()
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        gameObject.SetActive(true);
        fadeRoutine = StartCoroutine(FadeFromBlack());
    }

    public IEnumerator PlayFadeInAndWait()
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        gameObject.SetActive(true);
        fadeRoutine = StartCoroutine(FadeFromBlack());

        yield return fadeRoutine;
    }

    public void PlayFadeOut()
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        gameObject.SetActive(true);
        fadeRoutine = StartCoroutine(FadeToBlack());
    }

    public IEnumerator PlayFadeOutAndWait()
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        gameObject.SetActive(true);
        fadeRoutine = StartCoroutine(FadeToBlack());

        yield return fadeRoutine;
    }

    private IEnumerator FadeFromBlack()
    {
        float elapsed = 0f;

        canvasGroup.alpha = 1f;
        SetRaycastBlocking(true);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            SetRaycastBlocking(canvasGroup.alpha > 0f);

            yield return null;
        }

        canvasGroup.alpha = 0f;
        SetRaycastBlocking(false);

        fadeRoutine = null;
    }

    private IEnumerator FadeToBlack()
    {
        float elapsed = 0f;

        canvasGroup.alpha = 0f;
        SetRaycastBlocking(true);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            SetRaycastBlocking(true);

            yield return null;
        }

        canvasGroup.alpha = 1f;
        SetRaycastBlocking(true);

        fadeRoutine = null;
    }

    private void SetRaycastBlocking(bool shouldBlock)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.blocksRaycasts = shouldBlock;
        canvasGroup.interactable = shouldBlock;
    }
}