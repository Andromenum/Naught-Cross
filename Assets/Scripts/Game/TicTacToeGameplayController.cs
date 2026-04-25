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

    private float matchStartTime;

    private int moveCount;
    private int player1TurnCount;
    private int player2TurnCount;

    private Coroutine finishRoutine;
    private Coroutine countdownRoutine;

    private static readonly Vector2Int[] CheckDirections =
    {
        new Vector2Int(1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(1, 1),
        new Vector2Int(1, -1)
    };

    private void Start()
    {
        if (!ValidateSettings())
            return;

        if (!LoadMatchData())
            return;

        CacheBoardCells(landscapeBoardCellsRoot, landscapeCellLookup, "Landscape");
        CacheBoardCells(portraitBoardCellsRoot, portraitCellLookup, "Portrait");

        BeginMatch();
    }

    private void Update()
    {
        if (!matchStarted || gameEnded)
            return;

        UpdateElapsedTimeHUD();
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

        ClearAllBoardViews();
        ClearAllStrikeLines();

        SFXManager.Instance?.StopLoop();
        AudioManager.Instance?.RestoreMusicVolume(0f);

        isPlayer1Turn = true;
        gameEnded = false;
        matchStarted = false;

        moveCount = 0;
        player1TurnCount = 0;
        player2TurnCount = 0;

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

        if (victoryPopupController != null)
            victoryPopupController.ClosePopup();

        if (gameplayHUDController != null)
        {
            gameplayHUDController.LoadFromSession();
            gameplayHUDController.ClearTurnIndicators();
            gameplayHUDController.SetElapsedTime(0f);
            gameplayHUDController.SetPlayerTurnCounters(0, 0);
            gameplayHUDController.HideCountdown();
        }

        RefreshAllBoardViews();

        if (useStartCountdown)
            countdownRoutine = StartCoroutine(StartCountdownRoutine());
        else
            StartMatchAfterCountdown();
    }

    private IEnumerator StartCountdownRoutine()
    {
        if (gameplayHUDController != null)
            gameplayHUDController.HideCountdown();

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
        matchStartTime = Time.time;

        if (gameplayHUDController != null)
        {
            gameplayHUDController.SetElapsedTime(0f);
            gameplayHUDController.SetPlayerTurnCounters(player1TurnCount, player2TurnCount);
            gameplayHUDController.SetCurrentTurnToPlayer1();
        }

        RefreshAllBoardViews();
    }

    private void HandleCellClicked(Vector2Int cellPosition)
    {
        if (!matchStarted || gameEnded)
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

            RefreshAllBoardViews();
            UpdateElapsedTimeHUD();
            UpdatePlayerTurnCountersHUD();

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

        isPlayer1Turn = !isPlayer1Turn;

        if (gameplayHUDController != null)
        {
            if (isPlayer1Turn)
                gameplayHUDController.SetCurrentTurnToPlayer1();
            else
                gameplayHUDController.SetCurrentTurnToPlayer2();
        }

        RefreshAllBoardViews();
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

        if (fadeMusicOutOnResult)
            AudioManager.Instance?.DuckMusic(0f, resultMusicFadeOutDuration);

        SFXManager.Instance?.PlayLoopById("strike");

        if (landscapeStrikeLine != null &&
            landscapeCellLookup.TryGetValue(winStart, out BoardCellUI landscapeStartCell) &&
            landscapeCellLookup.TryGetValue(winEnd, out BoardCellUI landscapeEndCell))
        {
            StartCoroutine(landscapeStrikeLine.PlayStrikeBetween(
                landscapeStartCell.transform as RectTransform,
                landscapeEndCell.transform as RectTransform));

            waitTime = Mathf.Max(waitTime, landscapeStrikeLine.TotalDuration);
        }

        if (portraitStrikeLine != null &&
            portraitCellLookup.TryGetValue(winStart, out BoardCellUI portraitStartCell) &&
            portraitCellLookup.TryGetValue(winEnd, out BoardCellUI portraitEndCell))
        {
            StartCoroutine(portraitStrikeLine.PlayStrikeBetween(
                portraitStartCell.transform as RectTransform,
                portraitEndCell.transform as RectTransform));

            waitTime = Mathf.Max(waitTime, portraitStrikeLine.TotalDuration);
        }

        if (waitTime > 0f)
            yield return new WaitForSeconds(waitTime);

        SFXManager.Instance?.StopLoop();

        float winSfxLength = 0f;

        if (SFXManager.Instance != null)
        {
            winSfxLength = SFXManager.Instance.GetClipLengthById("win");
            SFXManager.Instance.PlayById("win");
        }

        float matchDuration = Mathf.Max(0f, Time.time - matchStartTime);

        if (PlayerProfilesManager.Instance != null && winner != null && loser != null)
            PlayerProfilesManager.Instance.RecordMatchWinnerLoser(
                winner.profileSlotIndex,
                loser.profileSlotIndex,
                matchDuration);

        if (victoryPopupController != null)
            victoryPopupController.ShowWinner(winner);

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

    private void FinishWithDraw()
    {
        gameEnded = true;
        matchStarted = false;

        RefreshAllBoardViews();
        ClearAllStrikeLines();

        UpdateElapsedTimeHUD();
        UpdatePlayerTurnCountersHUD();

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
            victoryPopupController.ShowDraw();

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

    private void HandleLayoutChanged(bool isPortrait)
    {
        RefreshAllBoardViews();
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

                bool canInteract = matchStarted && !gameEnded;
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
}