using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class UIFadeInOverlay : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private bool playOnStart = true;

    private CanvasGroup canvasGroup;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
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

        fadeRoutine = StartCoroutine(FadeFromBlack());
    }

    private IEnumerator FadeFromBlack()
    {
        float elapsed = 0f;

        canvasGroup.alpha = 1f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }
}