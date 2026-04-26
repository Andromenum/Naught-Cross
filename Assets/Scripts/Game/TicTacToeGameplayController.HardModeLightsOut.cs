using System.Collections;
using UnityEngine;

public partial class TicTacToeGameplayController
{
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
}