using System.Collections;
using UnityEngine;

public partial class HardModeBlackoutController
{
    private void OnRectTransformDimensionsChange()
    {
        RefreshHoleDataForCurrentLayout();
    }

    private void ShowAtZeroAlpha()
    {
        gameObject.SetActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        if (blackoutImage != null)
            blackoutImage.raycastTarget = true;
    }

    private IEnumerator FadeCanvasGroup(float from, float to, float duration)
    {
        if (canvasGroup == null)
            yield break;

        if (duration <= 0f)
        {
            canvasGroup.alpha = to;
            RefreshHoleDataForCurrentLayout();
            yield break;
        }

        float elapsed = 0f;
        canvasGroup.alpha = from;

        while (elapsed < duration)
        {
            elapsed += GetDeltaTime();

            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            canvasGroup.alpha = Mathf.Lerp(from, to, easedT);

            RefreshHoleDataForCurrentLayout();

            yield return null;
        }

        canvasGroup.alpha = to;
        RefreshHoleDataForCurrentLayout();
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private Camera GetCanvasCamera()
    {
        if (targetCanvas == null)
            return null;

        if (targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return targetCanvas.worldCamera;
    }

    private void HideImmediate()
    {
        acceptingInput = false;
        blackoutCancelled = true;
        blackoutRoutineActive = false;
        onInputReady = null;
        onCellSelected = null;

        ClearActiveSources();

        if (runtimeMaterial != null)
            ApplyBaseWobbleToMaterial();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (blackoutImage != null)
            blackoutImage.raycastTarget = false;

        gameObject.SetActive(false);
    }
}