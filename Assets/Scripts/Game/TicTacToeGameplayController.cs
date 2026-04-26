using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TicTacToeGameplayController : MonoBehaviour
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

        Debug.Log("TicTacToeGameplayController: Game mode = " + (hardModeActive ? "Hard" : "Classic"));
    }

    private void CacheBoardCells(
        Transform root,
        Dictionary<Vector2Int, BoardCellUI> targetLookup,
        string label)
    {
        targetLookup.Clear();

        if (root == null)
            return;

        int childCount = root.childCount;

        for (int i = 0; i < childCount; i++)
        {
            BoardCellUI cell = root.GetChild(i).GetComponent<BoardCellUI>();
            if (cell == null)
                continue;

            Vector2Int pos = cell.GridPosition;

            if (targetLookup.ContainsKey(pos))
            {
                Debug.LogWarning($"{label} board has duplicate cell grid position: {pos}");
                continue;
            }

            targetLookup.Add(pos, cell);
            cell.Setup(HandleCellClicked);
        }

        if (targetLookup.Count == 0)
            Debug.LogWarning($"{label} board has no valid BoardCellUI components.");
    }

    private void BeginMatch()
    {
        boardState.Clear();

        isPlayer1Turn = true;
        gameEnded = false;
        matchStarted = false;
        isGameplayPaused = false;
        isChaosTransitionRunning = false;
        allowHardModeTimerDuringChaos = false;

        ResetPendingLightsOutMove();
        ClearHardModeLightsOut();

        moveCount = 0;
        player1TurnCount = 0;
        player2TurnCount = 0;

        ResetHardModeTurnTimer();

        if (resetBoardRotationOnMatchStart)
            ResetBoardRotationRoots();

        ClearAllBoardViews();
        ClearAllStrikeLines();

        SFXManager.Instance?.StopLoop();
        AudioManager.Instance?.RestoreMusicVolume(0f);

        if (finishRoutine != null)
        {
            StopCoroutine(finishRoutine);
            finishRoutine = null;
        }

        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
            countdownRoutine = null;
        }

        if (postMoveRoutine != null)
        {
            StopCoroutine(postMoveRoutine);
            postMoveRoutine = null;
        }

        if (victoryPopupController != null)
            victoryPopupController.ClosePopup();

        if (gameplayHUDController != null)
        {
            gameplayHUDController.LoadFromSession();
            gameplayHUDController.ClearTurnIndicators();
            gameplayHUDController.SetElapsedTime(0f);
            gameplayHUDController.SetPlayerTurnCounters(0, 0);
            gameplayHUDController.HideCountdown();
            gameplayHUDController.SetGameplayMenuButtonInteractable(false);
            gameplayHUDController.SetHardModePlayerTimersVisible(false);
            gameplayHUDController.SetHardModePlayerTimers(player1HardModeTurnLimit, player2HardModeTurnLimit);
        }

        RefreshAllBoardViews();
        UpdateGameplayMenuButtonState();

        if (useStartCountdown)
            countdownRoutine = StartCoroutine(StartCountdownRoutine());
        else
            StartMatchAfterCountdown();
    }

    private IEnumerator StartCountdownRoutine()
    {
        if (gameplayHUDController != null)
            gameplayHUDController.HideCountdown();

        UpdateGameplayMenuButtonState();

        if (initialDelayBeforeCountdown > 0f)
            yield return new WaitForSecondsRealtime(initialDelayBeforeCountdown);

        yield return null;

        if (gameplayHUDController != null)
            gameplayHUDController.ShowCountdown("3");

        yield return new WaitForSecondsRealtime(countdownStepSeconds);

        if (gameplayHUDController != null)
            gameplayHUDController.ShowCountdown("2");

        yield return new WaitForSecondsRealtime(countdownStepSeconds);

        if (gameplayHUDController != null)
            gameplayHUDController.ShowCountdown("1");

        yield return new WaitForSecondsRealtime(countdownStepSeconds);

        if (gameplayHUDController != null)
            gameplayHUDController.ShowCountdown(matchText);

        yield return new WaitForSecondsRealtime(matchTextSeconds);

        if (gameplayHUDController != null)
            gameplayHUDController.HideCountdown();

        StartMatchAfterCountdown();
        countdownRoutine = null;
    }

    private void StartMatchAfterCountdown()
    {
        matchStarted = true;
        gameEnded = false;
        isGameplayPaused = false;
        isChaosTransitionRunning = false;
        allowHardModeTimerDuringChaos = false;
        ResetPendingLightsOutMove();

        matchStartTime = Time.time;

        if (gameplayHUDController != null)
        {
            gameplayHUDController.SetElapsedTime(0f);
            gameplayHUDController.SetPlayerTurnCounters(player1TurnCount, player2TurnCount);
            gameplayHUDController.SetCurrentTurnToPlayer1();
        }

        StartHardModeTurnTimerIfNeeded();

        RefreshAllBoardViews();
        UpdateGameplayMenuButtonState();
    }

    private void HandleCellClicked(Vector2Int cellPosition)
    {
        if (!matchStarted || gameEnded || isGameplayPaused || isChaosTransitionRunning)
            return;

        if (!CellExistsInAnyBoard(cellPosition))
            return;

        if (boardState.ContainsKey(cellPosition))
            return;

        PauseHardModeTurnTimer();

        int currentPlayerValue = isPlayer1Turn ? 1 : 2;
        Sprite currentMarkSprite = isPlayer1Turn ? player1MarkSprite : player2MarkSprite;

        boardState[cellPosition] = currentPlayerValue;
        moveCount++;

        if (isPlayer1Turn)
            player1TurnCount++;
        else
            player2TurnCount++;

        ApplyMarkToAllBoardViews(cellPosition, currentMarkSprite);
        UpdatePlayerTurnCountersHUD();

        if (SFXManager.Instance != null)
        {
            if (isPlayer1Turn)
                SFXManager.Instance.PlayById("place_x");
            else
                SFXManager.Instance.PlayById("place_o");
        }

        if (TryGetWinningLineFromLastMove(cellPosition, currentPlayerValue, out Vector2Int winStart, out Vector2Int winEnd))
        {
            gameEnded = true;
            matchStarted = false;
            isGameplayPaused = false;
            isChaosTransitionRunning = false;
            allowHardModeTimerDuringChaos = false;
            ResetPendingLightsOutMove();

            StopHardModeTurnTimer(true);
            ClearHardModeLightsOut();

            RefreshAllBoardViews();
            UpdateElapsedTimeHUD();
            UpdatePlayerTurnCountersHUD();
            UpdateGameplayMenuButtonState();

            if (gameplayHUDController != null)
                gameplayHUDController.ClearTurnIndicators();

            if (finishRoutine != null)
                StopCoroutine(finishRoutine);

            finishRoutine = StartCoroutine(FinishWithWinnerSequence(
                isPlayer1Turn ? player1Data : player2Data,
                isPlayer1Turn ? player2Data : player1Data,
                winStart,
                winEnd));

            return;
        }

        if (moveCount >= GetBoardCellCount())
        {
            FinishWithDraw();
            return;
        }

        if (postMoveRoutine != null)
            StopCoroutine(postMoveRoutine);

        postMoveRoutine = StartCoroutine(ResolveSuccessfulNonEndingMoveRoutine());
    }

    private IEnumerator ResolveSuccessfulNonEndingMoveRoutine()
    {
        isChaosTransitionRunning = true;
        allowHardModeTimerDuringChaos = false;
        ResetPendingLightsOutMove();

        RefreshAllBoardViews();
        UpdateGameplayMenuButtonState();

        DecreaseCurrentPlayerHardModeTurnLimitAfterMove();

        yield return PlayHardModeBoardRotationIfNeeded();

        isPlayer1Turn = !isPlayer1Turn;

        if (gameplayHUDController != null)
        {
            if (isPlayer1Turn)
                gameplayHUDController.SetCurrentTurnToPlayer1();
            else
                gameplayHUDController.SetCurrentTurnToPlayer2();
        }

        if (ShouldUseHardModeLightsOut())
        {
            yield return PlayHardModeLightsOutIfNeeded();

            allowHardModeTimerDuringChaos = false;

            if (gameEnded)
            {
                postMoveRoutine = null;
                yield break;
            }

            if (lightsOutMovePlaced)
            {
                FinalizeLightsOutPlacedMove();
                postMoveRoutine = null;
                yield break;
            }
        }

        isChaosTransitionRunning = false;

        if (!hardModeTimerRunning)
            StartHardModeTurnTimerIfNeeded();

        RefreshAllBoardViews();
        UpdateGameplayMenuButtonState();

        postMoveRoutine = null;
    }

    private bool TryGetWinningLineFromLastMove(
        Vector2Int lastMove,
        int playerValue,
        out Vector2Int lineStart,
        out Vector2Int lineEnd)
    {
        lineStart = lastMove;
        lineEnd = lastMove;

        if (moveCount < (requiredInRow * 2) - 1)
            return false;

        for (int i = 0; i < CheckDirections.Length; i++)
        {
            Vector2Int dir = CheckDirections[i];

            int forwardCount = CountDirection(lastMove, dir, playerValue, out Vector2Int forwardEnd);
            int backwardCount = CountDirection(lastMove, -dir, playerValue, out Vector2Int backwardEnd);

            int totalConnected = 1 + forwardCount + backwardCount;

            if (totalConnected >= requiredInRow)
            {
                lineStart = backwardCount > 0 ? backwardEnd : lastMove;
                lineEnd = forwardCount > 0 ? forwardEnd : lastMove;
                return true;
            }
        }

        return false;
    }

    private int CountDirection(
        Vector2Int start,
        Vector2Int direction,
        int playerValue,
        out Vector2Int furthestPoint)
    {
        int count = 0;
        furthestPoint = start;

        Vector2Int current = start + direction;

        while (boardState.TryGetValue(current, out int cellValue) && cellValue == playerValue)
        {
            count++;
            furthestPoint = current;
            current += direction;
        }

        return count;
    }

    private IEnumerator FinishWithWinnerSequence(
        MatchPlayerData winner,
        MatchPlayerData loser,
        Vector2Int winStart,
        Vector2Int winEnd)
    {
        float waitTime = 0f;
        bool playedAnyStrike = false;

        if (fadeMusicOutOnResult)
            AudioManager.Instance?.DuckMusic(0f, resultMusicFadeOutDuration);

        yield return null;
        Canvas.ForceUpdateCanvases();

        if (TryPlayStrikeForBoard(
                landscapeStrikeLine,
                landscapeCellLookup,
                winStart,
                winEnd,
                out float landscapeWait))
        {
            playedAnyStrike = true;
            waitTime = Mathf.Max(waitTime, landscapeWait);
        }

        if (TryPlayStrikeForBoard(
                portraitStrikeLine,
                portraitCellLookup,
                winStart,
                winEnd,
                out float portraitWait))
        {
            playedAnyStrike = true;
            waitTime = Mathf.Max(waitTime, portraitWait);
        }

        if (playedAnyStrike)
        {
            SFXManager.Instance?.PlayLoopById("strike");

            if (waitTime > 0f)
                yield return new WaitForSeconds(waitTime);
        }

        SFXManager.Instance?.StopLoop();

        float winSfxLength = 0f;

        if (SFXManager.Instance != null)
        {
            winSfxLength = SFXManager.Instance.GetClipLengthById("win");
            SFXManager.Instance.PlayById("win");
        }

        float matchDuration = Mathf.Max(0f, Time.time - matchStartTime);

        if (PlayerProfilesManager.Instance != null && winner != null && loser != null)
        {
            PlayerProfilesManager.Instance.RecordMatchWinnerLoser(
                winner.profileSlotIndex,
                loser.profileSlotIndex,
                matchDuration);
        }

        if (victoryPopupController != null)
            victoryPopupController.ShowWinner(winner, matchDuration);

        if (restoreMusicAfterResultSfx)
        {
            if (winSfxLength > 0f)
                yield return new WaitForSecondsRealtime(winSfxLength);

            AudioManager.Instance?.DuckMusic(
                resultPopupMusicVolumeMultiplier,
                resultPopupMusicFadeInDuration);
        }

        finishRoutine = null;
    }

    private bool TryPlayStrikeForBoard(
        StrikeLineController strikeLine,
        Dictionary<Vector2Int, BoardCellUI> lookup,
        Vector2Int winStart,
        Vector2Int winEnd,
        out float waitTime)
    {
        waitTime = 0f;

        if (strikeLine == null || lookup == null)
            return false;

        if (!lookup.TryGetValue(winStart, out BoardCellUI startCell))
            return false;

        if (!lookup.TryGetValue(winEnd, out BoardCellUI endCell))
            return false;

        if (startCell == null || endCell == null)
            return false;

        /*
         * This is the AR-safe part:
         * Only play on the currently visible board.
         * Do NOT check RevealMask / TipVisual here, because the strike script controls those.
         */
        if (!startCell.gameObject.activeInHierarchy || !endCell.gameObject.activeInHierarchy)
            return false;

        RectTransform startRect = startCell.transform as RectTransform;
        RectTransform endRect = endCell.transform as RectTransform;

        if (startRect == null || endRect == null)
            return false;

        /*
         * The StrikeLineRoot itself may have been disabled manually or by setup.
         * It is safe to enable it here, but only after we know its board cells are visible.
         */
        if (!strikeLine.gameObject.activeSelf)
            strikeLine.gameObject.SetActive(true);

        if (!strikeLine.enabled)
            strikeLine.enabled = true;

        StartCoroutine(strikeLine.PlayStrikeBetween(startRect, endRect));

        waitTime = strikeLine.TotalDuration;
        return true;
    }

    private void FinishWithDraw()
    {
        gameEnded = true;
        matchStarted = false;
        isGameplayPaused = false;
        isChaosTransitionRunning = false;
        allowHardModeTimerDuringChaos = false;
        ResetPendingLightsOutMove();

        StopHardModeTurnTimer(true);
        ClearHardModeLightsOut();

        RefreshAllBoardViews();
        ClearAllStrikeLines();

        UpdateElapsedTimeHUD();
        UpdatePlayerTurnCountersHUD();
        UpdateGameplayMenuButtonState();

        if (gameplayHUDController != null)
            gameplayHUDController.ClearTurnIndicators();

        if (finishRoutine != null)
            StopCoroutine(finishRoutine);

        finishRoutine = StartCoroutine(FinishWithDrawSequence());
    }

    private IEnumerator FinishWithDrawSequence()
    {
        if (fadeMusicOutOnResult)
            AudioManager.Instance?.DuckMusic(0f, resultMusicFadeOutDuration);

        SFXManager.Instance?.StopLoop();

        float drawSfxLength = 0f;

        if (SFXManager.Instance != null)
        {
            drawSfxLength = SFXManager.Instance.GetClipLengthById("draw");
            SFXManager.Instance.PlayById("draw");
        }

        float matchDuration = Mathf.Max(0f, Time.time - matchStartTime);

        if (PlayerProfilesManager.Instance != null && player1Data != null && player2Data != null)
            PlayerProfilesManager.Instance.RecordMatchDraw(
                player1Data.profileSlotIndex,
                player2Data.profileSlotIndex,
                matchDuration);

        if (victoryPopupController != null)
            victoryPopupController.ShowDraw(matchDuration);

        if (restoreMusicAfterResultSfx)
        {
            if (drawSfxLength > 0f)
                yield return new WaitForSecondsRealtime(drawSfxLength);

            AudioManager.Instance?.DuckMusic(
                resultPopupMusicVolumeMultiplier,
                resultPopupMusicFadeInDuration);
        }

        finishRoutine = null;
    }

    private void FinishWithTimeoutLoss()
    {
        if (gameEnded)
            return;

        gameEnded = true;
        matchStarted = false;
        isGameplayPaused = false;
        isChaosTransitionRunning = false;
        allowHardModeTimerDuringChaos = false;
        ResetPendingLightsOutMove();

        StopHardModeTurnTimer(true);
        ClearHardModeLightsOut();

        RefreshAllBoardViews();
        ClearAllStrikeLines();

        UpdateElapsedTimeHUD();
        UpdatePlayerTurnCountersHUD();
        UpdateGameplayMenuButtonState();

        if (gameplayHUDController != null)
            gameplayHUDController.ClearTurnIndicators();

        if (finishRoutine != null)
            StopCoroutine(finishRoutine);

        if (postMoveRoutine != null)
        {
            StopCoroutine(postMoveRoutine);
            postMoveRoutine = null;
        }

        MatchPlayerData loser = isPlayer1Turn ? player1Data : player2Data;
        MatchPlayerData winner = isPlayer1Turn ? player2Data : player1Data;

        finishRoutine = StartCoroutine(FinishWithTimeoutWinnerSequence(winner, loser));
    }

    private IEnumerator FinishWithTimeoutWinnerSequence(MatchPlayerData winner, MatchPlayerData loser)
    {
        if (fadeMusicOutOnResult)
            AudioManager.Instance?.DuckMusic(0f, resultMusicFadeOutDuration);

        SFXManager.Instance?.StopLoop();

        float resultSfxLength = 0f;

        if (SFXManager.Instance != null)
        {
            resultSfxLength = SFXManager.Instance.GetClipLengthById(timeoutResultSfxId);
            SFXManager.Instance.PlayById(timeoutResultSfxId);
        }

        float matchDuration = Mathf.Max(0f, Time.time - matchStartTime);

        if (PlayerProfilesManager.Instance != null && winner != null && loser != null)
            PlayerProfilesManager.Instance.RecordMatchWinnerLoser(
                winner.profileSlotIndex,
                loser.profileSlotIndex,
                matchDuration);

        if (victoryPopupController != null)
            victoryPopupController.ShowWinner(winner, matchDuration);

        if (restoreMusicAfterResultSfx)
        {
            if (resultSfxLength > 0f)
                yield return new WaitForSecondsRealtime(resultSfxLength);

            AudioManager.Instance?.DuckMusic(
                resultPopupMusicVolumeMultiplier,
                resultPopupMusicFadeInDuration);
        }

        finishRoutine = null;
    }

    private void HandleLayoutChanged(bool isPortrait)
    {
        RefreshAllBoardViews();
        UpdateGameplayMenuButtonState();
        RefreshHardModeTimerHUD();
    }

    private void RefreshAllBoardViews()
    {
        RefreshBoardView(landscapeCellLookup);
        RefreshBoardView(portraitCellLookup);
    }

    private void RefreshBoardView(Dictionary<Vector2Int, BoardCellUI> lookup)
    {
        foreach (KeyValuePair<Vector2Int, BoardCellUI> pair in lookup)
        {
            BoardCellUI cell = pair.Value;
            if (cell == null)
                continue;

            Vector2Int pos = pair.Key;

            if (boardState.TryGetValue(pos, out int playerValue))
            {
                Sprite markSprite = playerValue == 1 ? player1MarkSprite : player2MarkSprite;
                cell.SetMark(markSprite);
            }
            else
            {
                cell.ClearMark();

                bool canInteract =
                    matchStarted &&
                    !gameEnded &&
                    !isGameplayPaused &&
                    !isChaosTransitionRunning;

                cell.SetInteractable(canInteract);
            }
        }
    }

    private void ClearAllBoardViews()
    {
        ClearBoardView(landscapeCellLookup);
        ClearBoardView(portraitCellLookup);
    }

    private void ClearBoardView(Dictionary<Vector2Int, BoardCellUI> lookup)
    {
        foreach (KeyValuePair<Vector2Int, BoardCellUI> pair in lookup)
        {
            if (pair.Value != null)
                pair.Value.ClearMark();
        }
    }

    private void ApplyMarkToAllBoardViews(Vector2Int pos, Sprite markSprite)
    {
        if (landscapeCellLookup.TryGetValue(pos, out BoardCellUI landscapeCell) && landscapeCell != null)
            landscapeCell.SetMark(markSprite);

        if (portraitCellLookup.TryGetValue(pos, out BoardCellUI portraitCell) && portraitCell != null)
            portraitCell.SetMark(markSprite);
    }

    private void ClearAllStrikeLines()
    {
        if (landscapeStrikeLine != null)
            landscapeStrikeLine.ClearStrike();

        if (portraitStrikeLine != null)
            portraitStrikeLine.ClearStrike();
    }

    private bool CellExistsInAnyBoard(Vector2Int pos)
    {
        return landscapeCellLookup.ContainsKey(pos) || portraitCellLookup.ContainsKey(pos);
    }

    private int GetBoardCellCount()
    {
        return landscapeCellLookup.Count > 0 ? landscapeCellLookup.Count : portraitCellLookup.Count;
    }

    private void UpdateElapsedTimeHUD()
    {
        if (gameplayHUDController == null)
            return;

        float elapsed = Mathf.Max(0f, Time.time - matchStartTime);
        gameplayHUDController.SetElapsedTime(elapsed);
    }

    private void UpdatePlayerTurnCountersHUD()
    {
        if (gameplayHUDController == null)
            return;

        gameplayHUDController.SetPlayerTurnCounters(
            player1TurnCount,
            player2TurnCount);
    }

    private bool ShouldUseHardModeTurnTimer()
    {
        return hardModeActive && useHardModeTurnTimer;
    }

    private void ResetHardModeTurnTimer()
    {
        player1HardModeTurnLimit = hardModeStartingTurnSeconds;
        player2HardModeTurnLimit = hardModeStartingTurnSeconds;

        currentHardModeTurnRemaining = player1HardModeTurnLimit;
        hardModeTimerRunning = false;

        if (gameplayHUDController != null)
        {
            gameplayHUDController.SetHardModePlayerTimersVisible(false);
            gameplayHUDController.SetHardModePlayerTimers(
                player1HardModeTurnLimit,
                player2HardModeTurnLimit);
        }
    }

    private void StartHardModeTurnTimerIfNeeded()
    {
        if (!ShouldUseHardModeTurnTimer())
            return;

        currentHardModeTurnRemaining = GetCurrentPlayerHardModeTurnLimit();
        hardModeTimerRunning = true;

        if (gameplayHUDController != null)
        {
            gameplayHUDController.SetHardModePlayerTimersVisible(true);
            RefreshHardModeTimerHUD();
        }
    }

    private void PauseHardModeTurnTimer()
    {
        if (!ShouldUseHardModeTurnTimer())
            return;

        hardModeTimerRunning = false;
    }

    private void StopHardModeTurnTimer(bool hideVisual)
    {
        hardModeTimerRunning = false;

        if (gameplayHUDController == null)
            return;

        if (hideVisual)
            gameplayHUDController.SetHardModePlayerTimersVisible(false);
        else
            RefreshHardModeTimerHUD();
    }

    private void DecreaseCurrentPlayerHardModeTurnLimitAfterMove()
    {
        if (!ShouldUseHardModeTurnTimer())
            return;

        if (isPlayer1Turn)
        {
            player1HardModeTurnLimit -= hardModeTimeLossPerMove;
            player1HardModeTurnLimit = Mathf.Max(hardModeMinimumTurnSeconds, player1HardModeTurnLimit);
        }
        else
        {
            player2HardModeTurnLimit -= hardModeTimeLossPerMove;
            player2HardModeTurnLimit = Mathf.Max(hardModeMinimumTurnSeconds, player2HardModeTurnLimit);
        }

        RefreshHardModeTimerHUD();
    }

    private void UpdateHardModeTurnTimer()
    {
        if (!ShouldUseHardModeTurnTimer() || !hardModeTimerRunning)
            return;

        currentHardModeTurnRemaining -= Time.deltaTime;

        RefreshHardModeTimerHUD();

        if (currentHardModeTurnRemaining > 0f)
            return;

        FinishWithTimeoutLoss();
    }

    private float GetCurrentPlayerHardModeTurnLimit()
    {
        return isPlayer1Turn ? player1HardModeTurnLimit : player2HardModeTurnLimit;
    }

    private void RefreshHardModeTimerHUD()
    {
        if (gameplayHUDController == null)
            return;

        if (!ShouldUseHardModeTurnTimer())
        {
            gameplayHUDController.SetHardModePlayerTimersVisible(false);
            return;
        }

        float player1DisplayTime = player1HardModeTurnLimit;
        float player2DisplayTime = player2HardModeTurnLimit;

        if (hardModeTimerRunning)
        {
            if (isPlayer1Turn)
                player1DisplayTime = currentHardModeTurnRemaining;
            else
                player2DisplayTime = currentHardModeTurnRemaining;
        }

        gameplayHUDController.SetHardModePlayerTimers(player1DisplayTime, player2DisplayTime);
    }

    private bool ShouldUseHardModeBoardRotation()
    {
        return hardModeActive &&
               useHardModeBoardRotation &&
               (landscapeBoardRotationRoot != null || portraitBoardRotationRoot != null);
    }

    private IEnumerator PlayHardModeBoardRotationIfNeeded()
    {
        if (!ShouldUseHardModeBoardRotation())
            yield break;

        int direction = GetBoardRotationDirection();
        float rotationAmount = 90f * direction;

        float landscapeStartZ = GetLocalZRotation(landscapeBoardRotationRoot);
        float portraitStartZ = GetLocalZRotation(portraitBoardRotationRoot);

        float landscapeTargetZ = landscapeStartZ + rotationAmount;
        float portraitTargetZ = portraitStartZ + rotationAmount;

        if (boardRotationDuration <= 0f)
        {
            SetLocalZRotation(landscapeBoardRotationRoot, landscapeTargetZ);
            SetLocalZRotation(portraitBoardRotationRoot, portraitTargetZ);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < boardRotationDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / boardRotationDuration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            SetLocalZRotation(
                landscapeBoardRotationRoot,
                Mathf.Lerp(landscapeStartZ, landscapeTargetZ, easedT));

            SetLocalZRotation(
                portraitBoardRotationRoot,
                Mathf.Lerp(portraitStartZ, portraitTargetZ, easedT));

            yield return null;
        }

        SetLocalZRotation(landscapeBoardRotationRoot, landscapeTargetZ);
        SetLocalZRotation(portraitBoardRotationRoot, portraitTargetZ);
    }

    private int GetBoardRotationDirection()
    {
        if (randomizeBoardRotationDirection)
            return Random.value < 0.5f ? -1 : 1;

        return rotateClockwiseWhenNotRandom ? -1 : 1;
    }

    private void ResetBoardRotationRoots()
    {
        SetLocalZRotation(landscapeBoardRotationRoot, 0f);
        SetLocalZRotation(portraitBoardRotationRoot, 0f);
    }

    private float GetLocalZRotation(RectTransform target)
    {
        if (target == null)
            return 0f;

        return target.localEulerAngles.z;
    }

    private void SetLocalZRotation(RectTransform target, float zRotation)
    {
        if (target == null)
            return;

        Vector3 euler = target.localEulerAngles;
        euler.z = zRotation;
        target.localEulerAngles = euler;
    }

    private bool ShouldUseHardModeLightsOut()
    {
        return hardModeActive &&
               useHardModeLightsOut &&
               hardModeBlackoutController != null;
    }

    private IEnumerator PlayHardModeLightsOutIfNeeded()
    {
        if (!ShouldUseHardModeLightsOut())
            yield break;

        yield return hardModeBlackoutController.PlayInteractiveBlackout(
            landscapeCellLookup,
            portraitCellLookup,
            boardState,
            HandleLightsOutInputReady,
            HandleLightsOutCellSelected);
    }

    private void HandleLightsOutInputReady()
    {
        if (gameEnded)
            return;

        StartHardModeTurnTimerIfNeeded();
        allowHardModeTimerDuringChaos = true;
    }

    private void HandleLightsOutCellSelected(Vector2Int selectedCell)
    {
        PauseHardModeTurnTimer();
        allowHardModeTimerDuringChaos = false;

        PlaceLightsOutMarkImmediately(selectedCell);
    }

    private void ClearHardModeLightsOut()
    {
        if (hardModeBlackoutController != null)
            hardModeBlackoutController.ResetBlackoutState();
    }

    private void ResetPendingLightsOutMove()
    {
        lightsOutMovePlaced = false;
        lightsOutMoveHasWin = false;
        lightsOutMoveIsDraw = false;
        lightsOutWinStart = Vector2Int.zero;
        lightsOutWinEnd = Vector2Int.zero;
    }

    private void PlaceLightsOutMarkImmediately(Vector2Int cellPosition)
    {
        if (gameEnded)
            return;

        if (!CellExistsInAnyBoard(cellPosition))
            return;

        if (boardState.ContainsKey(cellPosition))
            return;

        int currentPlayerValue = isPlayer1Turn ? 1 : 2;
        Sprite currentMarkSprite = isPlayer1Turn ? player1MarkSprite : player2MarkSprite;

        boardState[cellPosition] = currentPlayerValue;
        moveCount++;

        if (isPlayer1Turn)
            player1TurnCount++;
        else
            player2TurnCount++;

        lightsOutMovePlaced = true;

        ApplyMarkToAllBoardViews(cellPosition, currentMarkSprite);
        UpdatePlayerTurnCountersHUD();

        if (SFXManager.Instance != null)
        {
            if (isPlayer1Turn)
                SFXManager.Instance.PlayById("place_x");
            else
                SFXManager.Instance.PlayById("place_o");
        }

        if (TryGetWinningLineFromLastMove(cellPosition, currentPlayerValue, out Vector2Int winStart, out Vector2Int winEnd))
        {
            lightsOutMoveHasWin = true;
            lightsOutWinStart = winStart;
            lightsOutWinEnd = winEnd;
            return;
        }

        if (moveCount >= GetBoardCellCount())
            lightsOutMoveIsDraw = true;
    }

    private void FinalizeLightsOutPlacedMove()
    {
        if (!lightsOutMovePlaced)
            return;

        if (lightsOutMoveHasWin)
        {
            gameEnded = true;
            matchStarted = false;
            isGameplayPaused = false;
            isChaosTransitionRunning = false;
            allowHardModeTimerDuringChaos = false;

            StopHardModeTurnTimer(true);
            ClearHardModeLightsOut();

            RefreshAllBoardViews();
            UpdateElapsedTimeHUD();
            UpdatePlayerTurnCountersHUD();
            UpdateGameplayMenuButtonState();

            if (gameplayHUDController != null)
                gameplayHUDController.ClearTurnIndicators();

            if (finishRoutine != null)
                StopCoroutine(finishRoutine);

            finishRoutine = StartCoroutine(FinishWithWinnerSequence(
                isPlayer1Turn ? player1Data : player2Data,
                isPlayer1Turn ? player2Data : player1Data,
                lightsOutWinStart,
                lightsOutWinEnd));

            ResetPendingLightsOutMove();
            return;
        }

        if (lightsOutMoveIsDraw)
        {
            ResetPendingLightsOutMove();
            FinishWithDraw();
            return;
        }

        DecreaseCurrentPlayerHardModeTurnLimitAfterMove();

        isPlayer1Turn = !isPlayer1Turn;

        if (gameplayHUDController != null)
        {
            if (isPlayer1Turn)
                gameplayHUDController.SetCurrentTurnToPlayer1();
            else
                gameplayHUDController.SetCurrentTurnToPlayer2();
        }

        isChaosTransitionRunning = false;
        allowHardModeTimerDuringChaos = false;

        StartHardModeTurnTimerIfNeeded();

        RefreshAllBoardViews();
        UpdateGameplayMenuButtonState();

        ResetPendingLightsOutMove();
    }

    public void SetGameplayPaused(bool paused)
    {
        if (gameEnded)
            paused = false;

        isGameplayPaused = paused;

        RefreshAllBoardViews();
        UpdateGameplayMenuButtonState();
        RefreshHardModeTimerHUD();
    }

    public bool CanPauseGameplay()
    {
        return matchStarted && !gameEnded && !isGameplayPaused && !isChaosTransitionRunning;
    }

    private void UpdateGameplayMenuButtonState()
    {
        if (gameplayHUDController == null)
            return;

        bool canUseMenuButton =
            matchStarted &&
            !gameEnded &&
            !isGameplayPaused &&
            !isChaosTransitionRunning;

        gameplayHUDController.SetGameplayMenuButtonInteractable(canUseMenuButton);
    }
}