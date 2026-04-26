using UnityEngine;

public partial class TicTacToeGameplayController
{
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
}