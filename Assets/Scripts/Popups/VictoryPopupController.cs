using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
public class VictoryPopupView
{
    [SerializeField] private GameObject root;
    [SerializeField] private Image winnerIconImage;
    [SerializeField] private TMP_Text winnerNameText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text matchTimeText;

    public GameObject Root => root;

    public void ApplyWinner(MatchPlayerData winner, ProfileIconLibrary iconLibrary, float matchDurationSeconds)
    {
        if (resultText != null)
            resultText.text = "WINS!";

        if (winnerNameText != null)
            winnerNameText.text = winner != null ? winner.playerName : string.Empty;

        if (winnerIconImage != null)
        {
            Sprite icon = null;

            if (winner != null && iconLibrary != null)
                icon = iconLibrary.GetIcon(winner.iconIndex);

            winnerIconImage.sprite = icon;
            winnerIconImage.enabled = icon != null;
        }

        ApplyMatchTime(matchDurationSeconds);
    }

    public void ApplyDraw(Sprite drawIcon, float matchDurationSeconds)
    {
        if (resultText != null)
            resultText.text = "DRAW";

        if (winnerNameText != null)
            winnerNameText.text = "NO WINNER";

        if (winnerIconImage != null)
        {
            winnerIconImage.sprite = drawIcon;
            winnerIconImage.enabled = drawIcon != null;
        }

        ApplyMatchTime(matchDurationSeconds);
    }

    private void ApplyMatchTime(float matchDurationSeconds)
    {
        if (matchTimeText == null)
            return;

        matchTimeText.text = "Match time: " + FormatTime(matchDurationSeconds) + " seconds";
    }

    private string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int minutes = totalSeconds / 60;
        int remainingSeconds = totalSeconds % 60;

        return minutes.ToString("00") + ":" + remainingSeconds.ToString("00");
    }
}

public class VictoryPopupController : PopupControllerBase
{
    [Header("Landscape View")]
    [SerializeField] private VictoryPopupView landscapeView;

    [Header("Portrait View")]
    [SerializeField] private VictoryPopupView portraitView;

    [Header("Libraries")]
    [SerializeField] private ProfileIconLibrary iconLibrary;

    [Header("Draw Result")]
    [SerializeField] private Sprite drawIcon;

    [Header("Scene Fade")]
    [SerializeField] private UIFadeInOverlay sceneFadeOverlay;

    [Header("Return To Main Menu")]
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";
    [SerializeField] private float returnTransitionDuration = 0.5f;
    [SerializeField] private bool clearMatchSetupOnReturn = true;

    [Header("Retry Match")]
    [SerializeField] private float retryTransitionDuration = 0.5f;
    [SerializeField] private bool fadeMusicOnRetry = true;
    [SerializeField] private bool useSceneFadeOnRetry = true;

    private bool isTransitioning;

    protected override void Awake()
    {
        if (landscapeView != null)
            landscapePopupRoot = landscapeView.Root;

        if (portraitView != null)
            portraitPopupRoot = portraitView.Root;

        base.Awake();
    }

    public void ShowWinner(MatchPlayerData winner, float matchDurationSeconds)
    {
        if (landscapeView != null)
            landscapeView.ApplyWinner(winner, iconLibrary, matchDurationSeconds);

        if (portraitView != null)
            portraitView.ApplyWinner(winner, iconLibrary, matchDurationSeconds);

        OpenPopup();
    }

    public void ShowDraw(float matchDurationSeconds)
    {
        if (landscapeView != null)
            landscapeView.ApplyDraw(drawIcon, matchDurationSeconds);

        if (portraitView != null)
            portraitView.ApplyDraw(drawIcon, matchDurationSeconds);

        OpenPopup();
    }

    public void OnBackToMainMenuPressed()
    {
        if (isTransitioning)
            return;

        StartCoroutine(ReturnToMainMenuRoutine());
    }

    public void OnRetryPressed()
    {
        if (isTransitioning)
            return;

        StartCoroutine(RetryMatchRoutine());
    }

    private IEnumerator ReturnToMainMenuRoutine()
    {
        isTransitioning = true;
        Time.timeScale = 1f;

        SFXManager.Instance?.StopLoop();

        if (AudioManager.Instance != null)
            AudioManager.Instance.FadeOutMusic(returnTransitionDuration);

        if (sceneFadeOverlay != null)
        {
            sceneFadeOverlay.PlayFadeOut();

            if (returnTransitionDuration > 0f)
                yield return new WaitForSecondsRealtime(returnTransitionDuration);
        }
        else if (returnTransitionDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(returnTransitionDuration);
        }

        if (clearMatchSetupOnReturn && GameSessionManager.Instance != null)
            GameSessionManager.Instance.ClearMatchSetup();

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private IEnumerator RetryMatchRoutine()
    {
        isTransitioning = true;
        Time.timeScale = 1f;

        SFXManager.Instance?.StopLoop();

        if (fadeMusicOnRetry && AudioManager.Instance != null)
            AudioManager.Instance.FadeOutMusic(retryTransitionDuration);

        if (useSceneFadeOnRetry && sceneFadeOverlay != null)
        {
            sceneFadeOverlay.PlayFadeOut();

            if (retryTransitionDuration > 0f)
                yield return new WaitForSecondsRealtime(retryTransitionDuration);
        }
        else if (retryTransitionDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(retryTransitionDuration);
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}