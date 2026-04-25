using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(RectTransform))]
public class BoardCellUI : MonoBehaviour
{
    private enum MarkRevealDirection
    {
        LeftToRight,
        RightToLeft,
        RandomHorizontal
    }

    [Header("Grid")]
    [SerializeField] private Vector2Int gridPosition;

    [Header("Placed Mark Variation")]
    [SerializeField] private float randomScaleAmount = 0.08f;
    [SerializeField] private float randomRotationDegrees = 7f;
    [SerializeField] private float randomPositionJitter = 0f;

    [Header("Optional Mark Reveal Animation")]
    [SerializeField] private bool useRevealAnimation = true;
    [SerializeField] private MarkRevealDirection revealDirection = MarkRevealDirection.RandomHorizontal;
    [SerializeField] private float revealDuration = 0.25f;
    [SerializeField] private AnimationCurve revealCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Uneven Reveal")]
    [SerializeField, Range(0f, 0.25f)] private float unevenProgressStrength = 0.04f;
    [SerializeField] private float unevenProgressSpeed = 12f;

    [Header("Reveal Motion Polish")]
    [SerializeField] private float revealScalePunch = 0.06f;
    [SerializeField] private float revealPositionWobble = 1.5f;
    [SerializeField, Range(0f, 1f)] private float fadeInPortion = 0.2f;

    private Button button;
    private Image markImage;
    private RectTransform rectTransform;
    private RectTransform markRectTransform;

    private Action<Vector2Int> clickCallback;
    private Coroutine revealRoutine;

    private bool refsCached;
    private bool defaultsCached;
    private bool hasMark;

    private Sprite currentSprite;

    private Image.Type originalImageType;
    private Image.FillMethod originalFillMethod;
    private int originalFillOrigin;
    private bool originalFillClockwise;
    private float originalFillAmount;

    private Vector3 originalMarkScale;
    private Quaternion originalMarkRotation;
    private Vector2 originalMarkAnchoredPosition;

    private Vector3 placedBaseScale;
    private Quaternion placedBaseRotation;
    private Vector2 placedBaseAnchoredPosition;

    public Vector2Int GridPosition => gridPosition;
    public RectTransform RectTransform => rectTransform;

    private void Awake()
    {
        EnsureReferences();
        CacheMarkDefaults();
        ForceHiddenMark();
    }

    private void OnValidate()
    {
        randomScaleAmount = Mathf.Max(0f, randomScaleAmount);
        randomRotationDegrees = Mathf.Max(0f, randomRotationDegrees);
        randomPositionJitter = Mathf.Max(0f, randomPositionJitter);

        revealDuration = Mathf.Max(0f, revealDuration);
        unevenProgressSpeed = Mathf.Max(0f, unevenProgressSpeed);
        revealScalePunch = Mathf.Max(0f, revealScalePunch);
        revealPositionWobble = Mathf.Max(0f, revealPositionWobble);
    }

    public void Setup(Action<Vector2Int> onClicked)
    {
        EnsureReferences();

        clickCallback = onClicked;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClicked);
        }
    }

    public void ClearMark()
    {
        EnsureReferences();
        CacheMarkDefaults();

        if (revealRoutine != null)
        {
            StopCoroutine(revealRoutine);
            revealRoutine = null;
        }

        hasMark = false;
        currentSprite = null;

        if (markImage != null)
        {
            RestoreImageSettings();
            markImage.sprite = null;
            SetImageAlpha(0f);
        }

        ResetMarkTransformToOriginal();
        SetInteractable(true);
    }

    public void SetMark(Sprite sprite)
    {
        EnsureReferences();
        CacheMarkDefaults();

        if (sprite == null)
        {
            ClearMark();
            return;
        }

        // Important: board refreshes call SetMark again.
        // Do not re-randomize or restart the reveal if this mark is already placed.
        if (hasMark && currentSprite == sprite)
        {
            SetInteractable(false);
            return;
        }

        hasMark = true;
        currentSprite = sprite;

        if (revealRoutine != null)
        {
            StopCoroutine(revealRoutine);
            revealRoutine = null;
        }

        GeneratePlacementVariation();

        if (markImage != null)
            markImage.sprite = sprite;

        bool canAnimate =
            useRevealAnimation &&
            revealDuration > 0f &&
            isActiveAndEnabled &&
            gameObject.activeInHierarchy;

        if (canAnimate)
            revealRoutine = StartCoroutine(RevealMarkRoutine());
        else
            ShowMarkInstant();

        SetInteractable(false);
    }

    public void SetInteractable(bool interactable)
    {
        EnsureReferences();

        if (button != null)
            button.interactable = interactable;
    }

    private IEnumerator RevealMarkRoutine()
    {
        if (markImage == null)
            yield break;

        PrepareImageForReveal();

        float elapsed = 0f;
        float shownProgress = 0f;
        float noiseSeed = UnityEngine.Random.Range(0f, 999f);

        while (elapsed < revealDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float normalized = Mathf.Clamp01(elapsed / revealDuration);
            float curved = revealCurve != null ? revealCurve.Evaluate(normalized) : normalized;

            float noise = 0f;

            if (unevenProgressStrength > 0f && unevenProgressSpeed > 0f)
            {
                noise = Mathf.PerlinNoise(noiseSeed, elapsed * unevenProgressSpeed);
                noise = (noise - 0.5f) * 2f;
            }

            float noisyProgress = Mathf.Clamp01(curved + noise * unevenProgressStrength);

            // Prevent the reveal from visually going backwards.
            shownProgress = Mathf.Max(shownProgress, noisyProgress);

            markImage.fillAmount = shownProgress;

            float alpha = fadeInPortion > 0f
                ? Mathf.Clamp01(normalized / fadeInPortion)
                : 1f;

            SetImageAlpha(alpha);

            ApplyRevealMotion(normalized);

            yield return null;
        }

        ShowMarkInstant();
        revealRoutine = null;
    }

    private void GeneratePlacementVariation()
    {
        float scaleMultiplier = UnityEngine.Random.Range(
            1f - randomScaleAmount,
            1f);

        float rotationZ = UnityEngine.Random.Range(
            -randomRotationDegrees,
            randomRotationDegrees);

        Vector2 positionOffset = Vector2.zero;

        if (randomPositionJitter > 0f)
        {
            positionOffset = new Vector2(
                UnityEngine.Random.Range(-randomPositionJitter, randomPositionJitter),
                UnityEngine.Random.Range(-randomPositionJitter, randomPositionJitter));
        }

        placedBaseScale = originalMarkScale * scaleMultiplier;
        placedBaseRotation = originalMarkRotation * Quaternion.Euler(0f, 0f, rotationZ);
        placedBaseAnchoredPosition = originalMarkAnchoredPosition + positionOffset;

        ApplyPlacedBaseTransform();
    }

    private void ApplyPlacedBaseTransform()
    {
        if (markRectTransform == null)
            return;

        markRectTransform.localScale = placedBaseScale;
        markRectTransform.localRotation = placedBaseRotation;
        markRectTransform.anchoredPosition = placedBaseAnchoredPosition;
    }

    private void PrepareImageForReveal()
    {
        if (markImage == null)
            return;

        ApplyPlacedBaseTransform();

        markImage.type = Image.Type.Filled;
        markImage.fillMethod = Image.FillMethod.Horizontal;
        markImage.fillClockwise = true;
        markImage.fillOrigin = GetFillOrigin();
        markImage.fillAmount = 0f;

        SetImageAlpha(0f);
    }

    private int GetFillOrigin()
    {
        switch (revealDirection)
        {
            case MarkRevealDirection.LeftToRight:
                return 0;

            case MarkRevealDirection.RightToLeft:
                return 1;

            case MarkRevealDirection.RandomHorizontal:
                return UnityEngine.Random.value < 0.5f ? 0 : 1;

            default:
                return 0;
        }
    }

    private void ApplyRevealMotion(float normalized)
    {
        if (markRectTransform == null)
            return;

        float punch = Mathf.Sin(normalized * Mathf.PI) * revealScalePunch;
        markRectTransform.localScale = placedBaseScale * (1f + punch);

        float wobble = Mathf.Sin(normalized * Mathf.PI * 2f) * revealPositionWobble;
        markRectTransform.anchoredPosition = placedBaseAnchoredPosition + new Vector2(0f, wobble);

        markRectTransform.localRotation = placedBaseRotation;
    }

    private void ShowMarkInstant()
    {
        if (markImage != null)
        {
            RestoreImageSettings();
            SetImageAlpha(1f);
        }

        ApplyPlacedBaseTransform();
    }

    private void ForceHiddenMark()
    {
        if (markImage == null)
            return;

        RestoreImageSettings();
        SetImageAlpha(0f);
        ResetMarkTransformToOriginal();
    }

    private void SetImageAlpha(float alpha)
    {
        if (markImage == null)
            return;

        Color c = markImage.color;
        c.a = Mathf.Clamp01(alpha);
        markImage.color = c;
    }

    private void ResetMarkTransformToOriginal()
    {
        if (markRectTransform == null)
            return;

        markRectTransform.localScale = originalMarkScale;
        markRectTransform.localRotation = originalMarkRotation;
        markRectTransform.anchoredPosition = originalMarkAnchoredPosition;

        placedBaseScale = originalMarkScale;
        placedBaseRotation = originalMarkRotation;
        placedBaseAnchoredPosition = originalMarkAnchoredPosition;
    }

    private void RestoreImageSettings()
    {
        if (markImage == null || !defaultsCached)
            return;

        markImage.type = originalImageType;
        markImage.fillMethod = originalFillMethod;
        markImage.fillOrigin = originalFillOrigin;
        markImage.fillClockwise = originalFillClockwise;
        markImage.fillAmount = originalFillAmount;
    }

    private void CacheMarkDefaults()
    {
        if (defaultsCached)
            return;

        if (markImage == null)
            return;

        originalImageType = markImage.type;
        originalFillMethod = markImage.fillMethod;
        originalFillOrigin = markImage.fillOrigin;
        originalFillClockwise = markImage.fillClockwise;
        originalFillAmount = markImage.fillAmount;

        markRectTransform = markImage.rectTransform;

        originalMarkScale = markRectTransform.localScale;
        originalMarkRotation = markRectTransform.localRotation;
        originalMarkAnchoredPosition = markRectTransform.anchoredPosition;

        placedBaseScale = originalMarkScale;
        placedBaseRotation = originalMarkRotation;
        placedBaseAnchoredPosition = originalMarkAnchoredPosition;

        defaultsCached = true;
    }

    private void EnsureReferences()
    {
        if (refsCached)
            return;

        button = GetComponent<Button>();
        rectTransform = GetComponent<RectTransform>();

        if (transform.childCount > 0)
            markImage = transform.GetChild(0).GetComponent<Image>();

        if (markImage != null)
            markRectTransform = markImage.rectTransform;

        refsCached = true;
    }

    private void HandleClicked()
    {
        clickCallback?.Invoke(gridPosition);
    }
}