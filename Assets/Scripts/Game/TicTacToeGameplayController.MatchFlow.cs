using System.Collections;
using UnityEngine;

public partial class TicTacToeGameplayController
{
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

            bool showHardModeTimersDuringCountdown = ShouldUseHardModeTurnTimer();

            gameplayHUDController.SetHardModePlayerTimers(
                player1HardModeTurnLimit,
                player2HardModeTurnLimit);

            gameplayHUDController.SetHardModePlayerTimersVisible(showHardModeTimersDuringCountdown);

            if (showHardModeTimersDuringCountdown)
                gameplayHUDController.PlayHardModeTimerIntro();
            else
                gameplayHUDController.StopHardModeTimerIntro();
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
            gameplayHUDController.StopHardModeTimerIntro();
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
}