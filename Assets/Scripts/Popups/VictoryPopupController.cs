using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
public class VictoryPopupView
{
    public GameObject root;
    public Image winnerIconImage;
    public TMP_Text winnerNameText;
    public TMP_Text resultText;
}

public class VictoryPopupController : PopupControllerBase
{
    [Header("Landscape View")]
    [SerializeField] private VictoryPopupView landscapeView;

    [Header("Portrait View")]
    [SerializeField] private VictoryPopupView portraitView;

    [Header("Libraries")]
    [SerializeField] private ProfileIconLibrary iconLibrary;

    [Header("Return To Main Menu")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private UIFadeInOverlay sceneFadeOverlay;
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
            landscapePopupRoot = landscapeView.root;

        if (portraitView != null)
            portraitPopupRoot = portraitView.root;

        base.Awake();
    }

    public void ShowWinner(MatchPlayerData winner)
    {
        ApplyWinnerToView(landscapeView, winner);
        ApplyWinnerToView(portraitView, winner);
        OpenPopup();
    }

    public void ShowDraw()
    {
        ApplyDrawToView(landscapeView);
        ApplyDrawToView(portraitView);
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

        SFXManager.Instance?.StopLoop();

        if (AudioManager.Instance != null)
            AudioManager.Instance.FadeOutMusic(returnTransitionDuration);

        if (sceneFadeOverlay != null)
        {
            sceneFadeOverlay.PlayFadeOut();
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

        SFXManager.Instance?.StopLoop();

        if (fadeMusicOnRetry && AudioManager.Instance != null)
            AudioManager.Instance.FadeOutMusic(retryTransitionDuration);

        if (useSceneFadeOnRetry && sceneFadeOverlay != null)
        {
            sceneFadeOverlay.PlayFadeOut();
            yield return new WaitForSecondsRealtime(retryTransitionDuration);
        }
        else if (retryTransitionDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(retryTransitionDuration);
        }

        // Important:
        // Do NOT clear GameSessionManager here.
        // This preserves the same two profiles and the same chosen X/O sprites.
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void ApplyWinnerToView(VictoryPopupView view, MatchPlayerData winner)
    {
        if (view == null)
            return;

        if (view.resultText != null)
            view.resultText.text = "WINS!";

        if (view.winnerNameText != null)
            view.winnerNameText.text = winner != null ? winner.playerName : string.Empty;

        if (view.winnerIconImage != null)
        {
            Sprite icon = null;

            if (winner != null && iconLibrary != null)
                icon = iconLibrary.GetIcon(winner.iconIndex);

            view.winnerIconImage.sprite = icon;
            view.winnerIconImage.enabled = icon != null;
        }
    }

    private void ApplyDrawToView(VictoryPopupView view)
    {
        if (view == null)
            return;

        if (view.resultText != null)
            view.resultText.text = "DRAW";

        if (view.winnerNameText != null)
            view.winnerNameText.text = "NO WINNER";

        if (view.winnerIconImage != null)
            view.winnerIconImage.enabled = false;
    }
}