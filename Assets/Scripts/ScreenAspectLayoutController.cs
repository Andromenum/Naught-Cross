using UnityEngine;
using UnityEngine.UI;

public class ScreenAspectLayoutController : MonoBehaviour
{
    private enum LayoutMode
    {
        Landscape,
        Portrait,
        TallPortrait
    }

    [Header("References")]
    [SerializeField] private VerticalLayoutGroup buttonLayout;
    [SerializeField] private AspectRatioFitter ARFiller;
    [SerializeField] private RectTransform exitButton;

    [Header("Breakpoints")]
    [SerializeField, Range(0.4f, 0.8f)] private float tallPortraitMaxAspect = 0.60f;

    [Header("Main Button Layout")]
    [SerializeField, Min(0)] private int landscapeSpacing = 70;
    [SerializeField, Min(0)] private int portraitSpacing = 250;
    [SerializeField, Min(0)] private int tallPortraitSpacing = 250;

    [SerializeField, Min(0)] private int landscapeTopSpacing = 150;
    [SerializeField, Min(0)] private int portraitTopSpacing = 0;
    [SerializeField, Min(0)] private int tallPortraitTopSpacing = 0;

    [Header("Background Aspect Ratio")]
    [SerializeField, Min(0.1f)] private float fitterRatioPortrait = 1f;
    [SerializeField, Min(0.1f)] private float fitterRatioLandscape = 1.8f;

    [Header("Exit Button Offsets")]
    [SerializeField] private Vector2 landscapeExitAnchoredPosition = new Vector2(-60f, 40f);
    [SerializeField] private Vector2 portraitExitAnchoredPosition = new Vector2(0f, 40f);
    [SerializeField] private Vector2 tallPortraitExitAnchoredPosition = new Vector2(0f, 40f);

    private RectTransform buttonLayoutRect;
    private Vector2Int lastScreenSize = new Vector2Int(-1, -1);
    private LayoutMode? lastAppliedMode;

    private void Awake()
    {
        CacheReferences();
        ApplyLayout(force: true);
    }

    private void OnEnable()
    {
        ApplyLayout(force: true);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheReferences();
    }
#endif

    private void Update()
    {
        Vector2Int currentScreenSize = new Vector2Int(Screen.width, Screen.height);

        if (currentScreenSize != lastScreenSize)
        {
            ApplyLayout();
        }
    }

    private void CacheReferences()
    {
        if (buttonLayout != null)
        {
            buttonLayoutRect = buttonLayout.GetComponent<RectTransform>();
        }
    }

    private void ApplyLayout(bool force = false)
    {
        if (buttonLayout == null || ARFiller == null)
            return;

        if (buttonLayoutRect == null)
            CacheReferences();

        Vector2Int currentScreenSize = new Vector2Int(Screen.width, Screen.height);
        lastScreenSize = currentScreenSize;

        LayoutMode currentMode = GetLayoutMode(currentScreenSize);

        if (!force && lastAppliedMode.HasValue && lastAppliedMode.Value == currentMode)
            return;

        lastAppliedMode = currentMode;

        switch (currentMode)
        {
            case LayoutMode.TallPortrait:
                ApplyLayoutValues(
                    tallPortraitSpacing,
                    tallPortraitTopSpacing,
                    fitterRatioPortrait,
                    tallPortraitExitAnchoredPosition,
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f)
                );
                break;

            case LayoutMode.Portrait:
                ApplyLayoutValues(
                    portraitSpacing,
                    portraitTopSpacing,
                    fitterRatioPortrait,
                    portraitExitAnchoredPosition,
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f)
                );
                break;

            case LayoutMode.Landscape:
                ApplyLayoutValues(
                    landscapeSpacing,
                    landscapeTopSpacing,
                    fitterRatioLandscape,
                    landscapeExitAnchoredPosition,
                    new Vector2(1f, 0f),
                    new Vector2(1f, 0f)
                );
                break;
        }

        if (buttonLayoutRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(buttonLayoutRect);
        }
    }

    private LayoutMode GetLayoutMode(Vector2Int screenSize)
    {
        float aspect = (float)screenSize.x / Mathf.Max(1, screenSize.y);

        if (aspect <= tallPortraitMaxAspect)
            return LayoutMode.TallPortrait;

        if (aspect < 1f)
            return LayoutMode.Portrait;

        return LayoutMode.Landscape;
    }

    private void ApplyLayoutValues(
        int spacing,
        int topPadding,
        float backgroundAspectRatio,
        Vector2 exitAnchoredPosition,
        Vector2 exitAnchor,
        Vector2 exitPivot)
    {
        buttonLayout.spacing = spacing;
        buttonLayout.padding.top = topPadding;
        ARFiller.aspectRatio = backgroundAspectRatio;

        if (exitButton == null)
            return;

        exitButton.anchorMin = exitAnchor;
        exitButton.anchorMax = exitAnchor;
        exitButton.pivot = exitPivot;
        exitButton.anchoredPosition = exitAnchoredPosition;
    }
}