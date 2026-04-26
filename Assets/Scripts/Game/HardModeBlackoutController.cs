using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class HardModeBlackoutController : MonoBehaviour, IPointerClickHandler
{
    private const int MaxHoleCount = 9;

    [Header("Optional Auto Reference")]
    [SerializeField] private Image blackoutImage;

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.65f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Chance")]
    [SerializeField, Range(0f, 1f)] private float blackoutChance = 0.4f;
    [SerializeField] private bool preventBackToBackBlackouts = true;

    [Header("Circle Holes")]
    [SerializeField, Range(0.1f, 1.5f)] private float holeRadiusMultiplier = 0.72f;
    [SerializeField, Range(0.01f, 1f)] private float holeSoftnessMultiplier = 0.25f;
    [SerializeField, Range(0.8f, 1.5f)] private float clickRadiusMultiplier = 1.1f;

    [Header("Morphing Edge")]
    [SerializeField, Range(0f, 0.2f)] private float edgeWobbleStrength = 0.06f;
    [SerializeField] private float edgeWobbleFrequency = 16f;
    [SerializeField] private float edgeWobbleSpeed = 6f;
    [SerializeField, Range(0f, 1f)] private float edgeRandomness = 0.85f;
    [SerializeField] private float edgeNoiseScale = 4.5f;

    [Header("Click Reveal")]
    [SerializeField] private bool useClickedHoleReveal = true;
    [SerializeField] private float clickedRevealWobbleTarget = 0.12f;
    [SerializeField] private float clickedRevealRadiusMultiplier = 40f;
    [SerializeField] private float clickedRevealSoftnessMultiplier = 8f;
    [SerializeField]
    private AnimationCurve clickedRevealRampCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.45f, 0.08f),
        new Keyframe(0.75f, 0.45f),
        new Keyframe(1f, 1f)
    );

    [Header("Audio")]
    [SerializeField] private string blackoutOpenSfxId = "";
    [SerializeField] private string blackoutSelectSfxId = "";

    private readonly Vector4[] holeData = new Vector4[MaxHoleCount];
    private readonly Vector2Int[] holeGridPositions = new Vector2Int[MaxHoleCount];
    private readonly Vector3[] worldCorners = new Vector3[4];

    private RectTransform overlayRect;
    private Canvas targetCanvas;
    private CanvasGroup canvasGroup;
    private Material runtimeMaterial;

    private bool previousBlackoutPlayed;
    private bool acceptingInput;
    private bool selectionMade;
    private bool blackoutCancelled;
    private bool blackoutRoutineActive;

    private int activeHoleCount;
    private int selectedHoleIndex = -1;

    private Action onInputReady;
    private Action<Vector2Int> onCellSelected;

    private Dictionary<Vector2Int, BoardCellUI> activeLandscapeCells;
    private Dictionary<Vector2Int, BoardCellUI> activePortraitCells;
    private Dictionary<Vector2Int, int> activeBoardState;

    public bool HasSelectedCell => selectionMade;
    public Vector2Int SelectedCell { get; private set; }

    private void Awake()
    {
        EnsureReferences();
        CreateRuntimeMaterial();
        ResetBlackoutState();
    }

    private void OnValidate()
    {
        fadeInDuration = Mathf.Max(0f, fadeInDuration);
        fadeOutDuration = Mathf.Max(0f, fadeOutDuration);

        edgeWobbleFrequency = Mathf.Max(0f, edgeWobbleFrequency);
        edgeWobbleSpeed = Mathf.Max(0f, edgeWobbleSpeed);
        edgeNoiseScale = Mathf.Max(0f, edgeNoiseScale);

        clickedRevealRadiusMultiplier = Mathf.Max(1f, clickedRevealRadiusMultiplier);
        clickedRevealSoftnessMultiplier = Mathf.Max(1f, clickedRevealSoftnessMultiplier);

        if (clickedRevealRampCurve == null || clickedRevealRampCurve.length == 0)
        {
            clickedRevealRampCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.45f, 0.08f),
                new Keyframe(0.75f, 0.45f),
                new Keyframe(1f, 1f)
            );
        }
    }

    private void OnRectTransformDimensionsChange()
    {
        RefreshHoleDataForCurrentLayout();
    }

    public void ResetBlackoutState()
    {
        previousBlackoutPlayed = false;
        selectionMade = false;
        acceptingInput = false;
        blackoutCancelled = true;
        blackoutRoutineActive = false;
        activeHoleCount = 0;
        selectedHoleIndex = -1;
        SelectedCell = Vector2Int.zero;
        onInputReady = null;
        onCellSelected = null;

        ClearActiveSources();
        HideImmediate();
    }

    public IEnumerator PlayInteractiveBlackout(
        Dictionary<Vector2Int, BoardCellUI> landscapeCells,
        Dictionary<Vector2Int, BoardCellUI> portraitCells,
        Dictionary<Vector2Int, int> boardState,
        Action onReadyForInput = null,
        Action<Vector2Int> onSelected = null)
    {
        EnsureReferences();
        CreateRuntimeMaterial();

        selectionMade = false;
        acceptingInput = false;
        blackoutCancelled = false;
        blackoutRoutineActive = false;
        SelectedCell = Vector2Int.zero;
        selectedHoleIndex = -1;
        activeHoleCount = 0;

        onInputReady = onReadyForInput;
        onCellSelected = onSelected;

        CacheActiveSources(landscapeCells, portraitCells, boardState);

        if (blackoutImage == null || overlayRect == null || runtimeMaterial == null)
        {
            previousBlackoutPlayed = false;
            ClearActiveSources();
            yield break;
        }

        if (blackoutChance <= 0f)
        {
            previousBlackoutPlayed = false;
            ClearActiveSources();
            yield break;
        }

        if (preventBackToBackBlackouts && previousBlackoutPlayed)
        {
            previousBlackoutPlayed = false;
            ClearActiveSources();
            yield break;
        }

        if (UnityEngine.Random.value > blackoutChance)
        {
            previousBlackoutPlayed = false;
            ClearActiveSources();
            yield break;
        }

        blackoutRoutineActive = true;

        activeHoleCount = BuildHoleData(activeLandscapeCells, activePortraitCells, activeBoardState);

        if (activeHoleCount <= 0)
        {
            previousBlackoutPlayed = false;
            blackoutRoutineActive = false;
            ClearActiveSources();
            yield break;
        }

        previousBlackoutPlayed = true;

        ApplyHoleDataToMaterial(activeHoleCount);
        ApplyBaseWobbleToMaterial();

        ShowAtZeroAlpha();

        if (!string.IsNullOrWhiteSpace(blackoutOpenSfxId))
            SFXManager.Instance?.PlayById(blackoutOpenSfxId);

        yield return FadeCanvasGroup(0f, 1f, fadeInDuration);

        acceptingInput = true;
        RefreshHoleDataForCurrentLayout();
        onInputReady?.Invoke();

        while (!selectionMade && !blackoutCancelled)
        {
            RefreshHoleDataForCurrentLayout();
            yield return null;
        }

        acceptingInput = false;

        if (blackoutCancelled)
        {
            HideImmediate();
            yield break;
        }

        if (!string.IsNullOrWhiteSpace(blackoutSelectSfxId))
            SFXManager.Instance?.PlayById(blackoutSelectSfxId);

        if (useClickedHoleReveal && selectedHoleIndex >= 0)
        {
            ApplyOnlySelectedHoleToMaterial();
            yield return PlayClickedHoleRevealExpansion(fadeOutDuration);
        }
        else
        {
            yield return FadeCanvasGroup(1f, 0f, fadeOutDuration);
        }

        HideImmediate();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!acceptingInput || selectionMade)
            return;

        RefreshHoleDataForCurrentLayout();

        if (TryGetHoleClicked(eventData.position, out Vector2Int clickedCell, out int clickedHoleIndex))
        {
            SelectedCell = clickedCell;
            selectedHoleIndex = clickedHoleIndex;
            selectionMade = true;

            onCellSelected?.Invoke(clickedCell);
        }
    }

    private void CacheActiveSources(
        Dictionary<Vector2Int, BoardCellUI> landscapeCells,
        Dictionary<Vector2Int, BoardCellUI> portraitCells,
        Dictionary<Vector2Int, int> boardState)
    {
        activeLandscapeCells = landscapeCells;
        activePortraitCells = portraitCells;
        activeBoardState = boardState;
    }

    private void ClearActiveSources()
    {
        activeLandscapeCells = null;
        activePortraitCells = null;
        activeBoardState = null;
    }

    private void RefreshHoleDataForCurrentLayout()
    {
        if (!blackoutRoutineActive)
            return;

        if (selectionMade || blackoutCancelled)
            return;

        if (runtimeMaterial == null || overlayRect == null)
            return;

        int refreshedHoleCount = BuildHoleData(activeLandscapeCells, activePortraitCells, activeBoardState);

        activeHoleCount = refreshedHoleCount;

        ApplyHoleDataToMaterial(activeHoleCount);
        ApplyBaseWobbleToMaterial();
    }

    private bool TryGetHoleClicked(Vector2 screenPosition, out Vector2Int clickedCell, out int clickedHoleIndex)
    {
        clickedCell = Vector2Int.zero;
        clickedHoleIndex = -1;

        if (overlayRect == null || activeHoleCount <= 0)
            return false;

        Rect overlayLocalRect = overlayRect.rect;

        if (overlayLocalRect.width <= 0f || overlayLocalRect.height <= 0f)
            return false;

        Camera canvasCamera = GetCanvasCamera();

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                overlayRect,
                screenPosition,
                canvasCamera,
                out Vector2 localPoint))
        {
            return false;
        }

        float uvX = Mathf.InverseLerp(overlayLocalRect.xMin, overlayLocalRect.xMax, localPoint.x);
        float uvY = Mathf.InverseLerp(overlayLocalRect.yMin, overlayLocalRect.yMax, localPoint.y);

        Vector2 clickUv = new Vector2(uvX, uvY);
        float aspect = overlayLocalRect.height > 0f ? overlayLocalRect.width / overlayLocalRect.height : 1f;

        for (int i = 0; i < activeHoleCount; i++)
        {
            Vector4 hole = holeData[i];

            Vector2 delta = clickUv - new Vector2(hole.x, hole.y);
            delta.x *= aspect;

            float distance = delta.magnitude;
            float clickableRadius = hole.z * clickRadiusMultiplier;

            if (distance <= clickableRadius)
            {
                clickedCell = holeGridPositions[i];
                clickedHoleIndex = i;
                return true;
            }
        }

        return false;
    }

    private void EnsureReferences()
    {
        if (overlayRect == null)
            overlayRect = GetComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (targetCanvas == null)
            targetCanvas = GetComponentInParent<Canvas>(true);

        if (blackoutImage == null)
            blackoutImage = GetComponentInChildren<Image>(true);
    }

    private void CreateRuntimeMaterial()
    {
        if (runtimeMaterial != null)
            return;

        if (blackoutImage == null)
            return;

        Material sourceMaterial = blackoutImage.material;

        if (sourceMaterial == null)
        {
            Debug.LogWarning("HardModeBlackoutController: Blackout Image has no material assigned.");
            return;
        }

        runtimeMaterial = new Material(sourceMaterial);
        blackoutImage.material = runtimeMaterial;
    }

    private int BuildHoleData(
        Dictionary<Vector2Int, BoardCellUI> landscapeCells,
        Dictionary<Vector2Int, BoardCellUI> portraitCells,
        Dictionary<Vector2Int, int> boardState)
    {
        ClearHoleData();

        int holeCount = 0;

        AddHolesFromLookup(landscapeCells, boardState, ref holeCount);
        AddHolesFromLookup(portraitCells, boardState, ref holeCount);

        return holeCount;
    }

    private void AddHolesFromLookup(
        Dictionary<Vector2Int, BoardCellUI> lookup,
        Dictionary<Vector2Int, int> boardState,
        ref int holeCount)
    {
        if (lookup == null)
            return;

        foreach (KeyValuePair<Vector2Int, BoardCellUI> pair in lookup)
        {
            if (holeCount >= MaxHoleCount)
                return;

            if (boardState != null && boardState.ContainsKey(pair.Key))
                continue;

            BoardCellUI cell = pair.Value;

            if (cell == null || !cell.gameObject.activeInHierarchy)
                continue;

            RectTransform cellRect = cell.transform as RectTransform;

            if (cellRect == null)
                continue;

            if (TryBuildHoleFromCell(cellRect, out Vector4 hole))
            {
                holeData[holeCount] = hole;
                holeGridPositions[holeCount] = pair.Key;
                holeCount++;
            }
        }
    }

    private bool TryBuildHoleFromCell(RectTransform cellRect, out Vector4 hole)
    {
        hole = Vector4.zero;

        if (cellRect == null || overlayRect == null)
            return false;

        Rect overlayLocalRect = overlayRect.rect;

        if (overlayLocalRect.width <= 0f || overlayLocalRect.height <= 0f)
            return false;

        Camera canvasCamera = GetCanvasCamera();

        cellRect.GetWorldCorners(worldCorners);

        bool hasPoint = false;
        Vector2 min = Vector2.zero;
        Vector2 max = Vector2.zero;

        for (int i = 0; i < worldCorners.Length; i++)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(canvasCamera, worldCorners[i]);

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    overlayRect,
                    screenPoint,
                    canvasCamera,
                    out Vector2 localPoint))
            {
                continue;
            }

            if (!hasPoint)
            {
                min = localPoint;
                max = localPoint;
                hasPoint = true;
            }
            else
            {
                min = Vector2.Min(min, localPoint);
                max = Vector2.Max(max, localPoint);
            }
        }

        if (!hasPoint)
            return false;

        Vector2 centerLocal = (min + max) * 0.5f;
        Vector2 sizeLocal = max - min;

        float radiusLocal = Mathf.Max(sizeLocal.x, sizeLocal.y) * 0.5f * holeRadiusMultiplier;

        if (radiusLocal <= 0f)
            return false;

        float radiusNormalized = radiusLocal / overlayLocalRect.height;
        float softnessNormalized = radiusNormalized * holeSoftnessMultiplier;

        float uvX = Mathf.InverseLerp(overlayLocalRect.xMin, overlayLocalRect.xMax, centerLocal.x);
        float uvY = Mathf.InverseLerp(overlayLocalRect.yMin, overlayLocalRect.yMax, centerLocal.y);

        hole = new Vector4(uvX, uvY, radiusNormalized, softnessNormalized);
        return true;
    }

    private void ApplyHoleDataToMaterial(int holeCount)
    {
        if (runtimeMaterial == null || overlayRect == null)
            return;

        Rect rect = overlayRect.rect;
        float aspect = rect.height > 0f ? rect.width / rect.height : 1f;

        runtimeMaterial.SetFloat("_Aspect", aspect);
        runtimeMaterial.SetFloat("_HoleCount", holeCount);

        runtimeMaterial.SetVector("_Hole0", holeData[0]);
        runtimeMaterial.SetVector("_Hole1", holeData[1]);
        runtimeMaterial.SetVector("_Hole2", holeData[2]);
        runtimeMaterial.SetVector("_Hole3", holeData[3]);
        runtimeMaterial.SetVector("_Hole4", holeData[4]);
        runtimeMaterial.SetVector("_Hole5", holeData[5]);
        runtimeMaterial.SetVector("_Hole6", holeData[6]);
        runtimeMaterial.SetVector("_Hole7", holeData[7]);
        runtimeMaterial.SetVector("_Hole8", holeData[8]);
    }

    private void ApplyOnlySelectedHoleToMaterial()
    {
        if (selectedHoleIndex < 0 || selectedHoleIndex >= activeHoleCount)
            return;

        Vector4 selectedHole = holeData[selectedHoleIndex];

        ClearHoleData();

        holeData[0] = selectedHole;
        activeHoleCount = 1;

        ApplyHoleDataToMaterial(1);
    }

    private void ApplyBaseWobbleToMaterial()
    {
        if (runtimeMaterial == null)
            return;

        runtimeMaterial.SetFloat("_EdgeWobbleStrength", edgeWobbleStrength);
        runtimeMaterial.SetFloat("_EdgeWobbleFrequency", edgeWobbleFrequency);
        runtimeMaterial.SetFloat("_EdgeWobbleSpeed", edgeWobbleSpeed);
        runtimeMaterial.SetFloat("_EdgeRandomness", edgeRandomness);
        runtimeMaterial.SetFloat("_EdgeNoiseScale", edgeNoiseScale);
    }

    private IEnumerator PlayClickedHoleRevealExpansion(float duration)
    {
        if (runtimeMaterial == null)
            yield break;

        Vector4 startHole = holeData[0];

        float startWobbleStrength = edgeWobbleStrength;
        float targetWobbleStrength = clickedRevealWobbleTarget;

        float startRadius = startHole.z;

        float targetRadiusByMultiplier = startRadius * Mathf.Max(1f, clickedRevealRadiusMultiplier);
        float targetRadiusFullScreen = 2.5f;
        float targetRadius = Mathf.Max(targetRadiusByMultiplier, targetRadiusFullScreen);

        float startSoftness = startHole.w;

        float targetSoftness = Mathf.Max(
            startSoftness * Mathf.Max(1f, clickedRevealSoftnessMultiplier),
            0.45f
        );

        if (duration <= 0f)
        {
            runtimeMaterial.SetFloat("_EdgeWobbleStrength", targetWobbleStrength);

            holeData[0] = new Vector4(
                startHole.x,
                startHole.y,
                targetRadius,
                targetSoftness);

            ApplyHoleDataToMaterial(1);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += GetDeltaTime();

            float normalized = Mathf.Clamp01(elapsed / duration);

            float ramped = clickedRevealRampCurve != null
                ? Mathf.Clamp01(clickedRevealRampCurve.Evaluate(normalized))
                : normalized * normalized * normalized;

            float wobbleStrength = Mathf.Lerp(startWobbleStrength, targetWobbleStrength, ramped);
            float radius = Mathf.Lerp(startRadius, targetRadius, ramped);
            float softness = Mathf.Lerp(startSoftness, targetSoftness, ramped);

            runtimeMaterial.SetFloat("_EdgeWobbleStrength", wobbleStrength);

            holeData[0] = new Vector4(
                startHole.x,
                startHole.y,
                radius,
                softness);

            ApplyHoleDataToMaterial(1);

            yield return null;
        }

        runtimeMaterial.SetFloat("_EdgeWobbleStrength", targetWobbleStrength);

        holeData[0] = new Vector4(
            startHole.x,
            startHole.y,
            targetRadius,
            targetSoftness);

        ApplyHoleDataToMaterial(1);
    }

    private void ClearHoleData()
    {
        for (int i = 0; i < holeData.Length; i++)
        {
            holeData[i] = Vector4.zero;
            holeGridPositions[i] = Vector2Int.zero;
        }

        activeHoleCount = 0;
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