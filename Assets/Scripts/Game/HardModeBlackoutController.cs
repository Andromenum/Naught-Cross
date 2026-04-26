using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public partial class HardModeBlackoutController : MonoBehaviour, IPointerClickHandler
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

    /// <summary>
    /// Starts the hard-mode blackout phase.
    /// Fades in, enables click input after fade-in, waits for an empty cell click,
    /// then plays the clicked-hole reveal animation before hiding.
    /// </summary>

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
}