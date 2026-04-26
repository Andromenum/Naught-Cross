using System.Collections.Generic;
using UnityEngine;

public partial class TicTacToeGameplayController : MonoBehaviour
{
    [Header("Board Roots")]
    [SerializeField] private Transform landscapeBoardCellsRoot;
    [SerializeField] private Transform portraitBoardCellsRoot;
    [SerializeField] private int requiredInRow = 3;

    [Header("Scene References")]
    [SerializeField] private GameplayHUDController gameplayHUDController;
    [SerializeField] private VictoryPopupController victoryPopupController;

    [Header("Strike Line")]
    [SerializeField] private StrikeLineController landscapeStrikeLine;
    [SerializeField] private StrikeLineController portraitStrikeLine;

    [Header("Start Countdown")]
    [SerializeField] private bool useStartCountdown = true;
    [SerializeField] private float initialDelayBeforeCountdown = 0.5f;
    [SerializeField] private float countdownStepSeconds = 0.75f;
    [SerializeField] private float matchTextSeconds = 0.65f;
    [SerializeField] private string matchText = "MATCH!";

    [Header("Result Music Control")]
    [SerializeField] private bool fadeMusicOutOnResult = true;
    [SerializeField] private float resultMusicFadeOutDuration = 0.5f;

    [SerializeField] private bool restoreMusicAfterResultSfx = true;
    [SerializeField, Range(0f, 1f)] private float resultPopupMusicVolumeMultiplier = 0.5f;
    [SerializeField] private float resultPopupMusicFadeInDuration = 0.5f;

    [Header("Hard Mode Turn Timer")]
    [SerializeField] private bool useHardModeTurnTimer = true;
    [SerializeField] private float hardModeStartingTurnSeconds = 1.5f;
    [SerializeField] private float hardModeTimeLossPerMove = 0.3f;
    [SerializeField] private float hardModeMinimumTurnSeconds = 0.6f;
    [SerializeField] private string timeoutResultSfxId = "win";

    [Header("Hard Mode Board Rotation")]
    [SerializeField] private bool useHardModeBoardRotation = true;
    [SerializeField] private RectTransform landscapeBoardRotationRoot;
    [SerializeField] private RectTransform portraitBoardRotationRoot;
    [SerializeField] private float boardRotationDuration = 0.25f;
    [SerializeField] private bool randomizeBoardRotationDirection = true;
    [SerializeField] private bool rotateClockwiseWhenNotRandom = true;
    [SerializeField] private bool resetBoardRotationOnMatchStart = true;

    [Header("Hard Mode Lights Out")]
    [SerializeField] private bool useHardModeLightsOut = true;
    [SerializeField] private HardModeBlackoutController hardModeBlackoutController;

    private readonly Dictionary<Vector2Int, int> boardState = new Dictionary<Vector2Int, int>();

    private readonly Dictionary<Vector2Int, BoardCellUI> landscapeCellLookup = new Dictionary<Vector2Int, BoardCellUI>();
    private readonly Dictionary<Vector2Int, BoardCellUI> portraitCellLookup = new Dictionary<Vector2Int, BoardCellUI>();

    private MatchPlayerData player1Data;
    private MatchPlayerData player2Data;

    private Sprite player1MarkSprite;
    private Sprite player2MarkSprite;

    private bool isPlayer1Turn = true;
    private bool gameEnded;
    private bool matchStarted;
    private bool isGameplayPaused;
    private bool hardModeActive;
    private bool isChaosTransitionRunning;
    private bool allowHardModeTimerDuringChaos;

    private bool lightsOutMovePlaced;
    private bool lightsOutMoveHasWin;
    private bool lightsOutMoveIsDraw;
    private Vector2Int lightsOutWinStart;
    private Vector2Int lightsOutWinEnd;

    private float matchStartTime;

    private int moveCount;
    private int player1TurnCount;
    private int player2TurnCount;

    private float player1HardModeTurnLimit;
    private float player2HardModeTurnLimit;
    private float currentHardModeTurnRemaining;
    private bool hardModeTimerRunning;

    private Coroutine finishRoutine;
    private Coroutine countdownRoutine;
    private Coroutine postMoveRoutine;

    public bool HardModeActive => hardModeActive;

    private static readonly Vector2Int[] CheckDirections =
    {
        new Vector2Int(1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(1, 1),
        new Vector2Int(1, -1)
    };

    private void OnValidate()
    {
        requiredInRow = Mathf.Max(1, requiredInRow);

        hardModeStartingTurnSeconds = Mathf.Max(0.1f, hardModeStartingTurnSeconds);
        hardModeTimeLossPerMove = Mathf.Max(0f, hardModeTimeLossPerMove);
        hardModeMinimumTurnSeconds = Mathf.Max(0.1f, hardModeMinimumTurnSeconds);
        boardRotationDuration = Mathf.Max(0f, boardRotationDuration);

        if (hardModeMinimumTurnSeconds > hardModeStartingTurnSeconds)
            hardModeMinimumTurnSeconds = hardModeStartingTurnSeconds;
    }

    private void Start()
    {
        if (!ValidateSettings())
            return;

        if (!LoadMatchData())
            return;

        CacheGameModeFromSession();

        CacheBoardCells(landscapeBoardCellsRoot, landscapeCellLookup, "Landscape");
        CacheBoardCells(portraitBoardCellsRoot, portraitCellLookup, "Portrait");

        BeginMatch();
    }

    private void Update()
    {
        if (!matchStarted || gameEnded || isGameplayPaused)
            return;

        UpdateElapsedTimeHUD();

        if (!isChaosTransitionRunning || allowHardModeTimerDuringChaos)
            UpdateHardModeTurnTimer();
    }

    private void OnEnable()
    {
        if (UILayoutController.Instance != null)
            UILayoutController.Instance.LayoutChanged += HandleLayoutChanged;
    }

    private void OnDisable()
    {
        if (UILayoutController.Instance != null)
            UILayoutController.Instance.LayoutChanged -= HandleLayoutChanged;
    }

    private bool ValidateSettings()
    {
        if (requiredInRow <= 0)
        {
            Debug.LogWarning("TicTacToeGameplayController: requiredInRow must be greater than 0.");
            return false;
        }

        if (landscapeBoardCellsRoot == null && portraitBoardCellsRoot == null)
        {
            Debug.LogWarning("TicTacToeGameplayController: Both board roots are missing.");
            return false;
        }

        return true;
    }

    private bool LoadMatchData()
    {
        if (GameSessionManager.Instance == null || !GameSessionManager.Instance.HasValidMatchSetup)
        {
            Debug.LogWarning("TicTacToeGameplayController: Game session data is missing.");
            return false;
        }

        player1Data = GameSessionManager.Instance.Player1;
        player2Data = GameSessionManager.Instance.Player2;

        if (player1Data == null || player2Data == null)
        {
            Debug.LogWarning("TicTacToeGameplayController: Player data is invalid.");
            return false;
        }

        if (player1Data.selectedMarkSprite == null)
        {
            Debug.LogWarning("TicTacToeGameplayController: Player 1 selected mark sprite is missing.");
            return false;
        }

        if (player2Data.selectedMarkSprite == null)
        {
            Debug.LogWarning("TicTacToeGameplayController: Player 2 selected mark sprite is missing.");
            return false;
        }

        player1MarkSprite = player1Data.selectedMarkSprite;
        player2MarkSprite = player2Data.selectedMarkSprite;

        return true;
    }

    private void CacheGameModeFromSession()
    {
        hardModeActive =
            GameSessionManager.Instance != null &&
            GameSessionManager.Instance.IsHardMode;
    }
}