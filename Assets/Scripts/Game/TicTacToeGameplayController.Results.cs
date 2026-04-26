using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class TicTacToeGameplayController
{
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
         * AR-safe check:
         * Only play on the currently visible board.
         * Do not check RevealMask / TipVisual here because the strike script controls those.
         */
        if (!startCell.gameObject.activeInHierarchy || !endCell.gameObject.activeInHierarchy)
            return false;

        RectTransform startRect = startCell.transform as RectTransform;
        RectTransform endRect = endCell.transform as RectTransform;

        if (startRect == null || endRect == null)
            return false;

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
        {
            PlayerProfilesManager.Instance.RecordMatchDraw(
                player1Data.profileSlotIndex,
                player2Data.profileSlotIndex,
                matchDuration);
        }

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
            if (resultSfxLength > 0f)
                yield return new WaitForSecondsRealtime(resultSfxLength);

            AudioManager.Instance?.DuckMusic(
                resultPopupMusicVolumeMultiplier,
                resultPopupMusicFadeInDuration);
        }

        finishRoutine = null;
    }
}