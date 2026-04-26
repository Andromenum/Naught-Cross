using UnityEngine;

public partial class TicTacToeGameplayController
{
    private void HandleLayoutChanged(bool isPortrait)
    {
        RefreshAllBoardViews();
        UpdateGameplayMenuButtonState();
        RefreshHardModeTimerHUD();
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