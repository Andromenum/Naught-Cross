using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
public class GameplayPauseMenuView
{
    [SerializeField] private GameObject root;
    [SerializeField] private Button quitButton;
    [SerializeField] private GameObject quitConfirmRoot;

    public GameObject Root => root;
    public Button QuitButton => quitButton;
    public GameObject QuitConfirmRoot => quitConfirmRoot;
}

public class GameplayPauseMenuController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TicTacToeGameplayController gameplayController;
    [SerializeField] private UIFadeInOverlay sceneFadeOverlay;

    [Header("Landscape View")]
    [SerializeField] private GameplayPauseMenuView landscapeView;

    [Header("Portrait View")]
    [SerializeField] private GameplayPauseMenuView portraitView;

    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";
    [SerializeField] private float returnTransitionDuration = 0.5f;
    [SerializeField] private bool clearMatchSetupOnQuit = true;

    [Header("Audio")]
    [SerializeField] private string pauseOpenSfxId = "pause_open";

    private bool isPaused;
    private bool isTransitioning;
    private bool quitConfirmationOpen;

    private void Awake()
    {
        SetViewRootActive(landscapeView, false);
        SetViewRootActive(portraitView, false);

        ResetQuitConfirmationState();
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

    public void OpenPauseMenu()
    {
        if (isPaused || isTransitioning)
            return;

        if (gameplayController != null && !gameplayController.CanPauseGameplay())
            return;

        isPaused = true;
        quitConfirmationOpen = false;

        RefreshLayoutVisibility();
        ApplyQuitConfirmationStateToAllViews();
        ClearSelectedButton();

        Time.timeScale = 0f;

        if (gameplayController != null)
            gameplayController.SetGameplayPaused(true);

        AudioManager.Instance?.PauseMusicForGameplayPause();

        if (SFXManager.Instance != null)
        {
            if (!string.IsNullOrWhiteSpace(pauseOpenSfxId))
                SFXManager.Instance.PlayById(pauseOpenSfxId);
            else
                SFXManager.Instance.PlayPopupOpen();
        }
    }

    public void ClosePauseMenu()
    {
        if (!isPaused || isTransitioning)
            return;

        isPaused = false;
        quitConfirmationOpen = false;

        ApplyQuitConfirmationStateToAllViews();
        ClearSelectedButton();

        SetViewRootActive(landscapeView, false);
        SetViewRootActive(portraitView, false);

        Time.timeScale = 1f;

        if (gameplayController != null)
            gameplayController.SetGameplayPaused(false);

        AudioManager.Instance?.ResumeMusicFromGameplayPause();
    }

    public void OnQuitPressed()
    {
        if (isTransitioning)
            return;

        quitConfirmationOpen = true;
        ApplyQuitConfirmationStateToAllViews();
        ClearSelectedButton();
    }

    public void OnQuitNoPressed()
    {
        if (isTransitioning)
            return;

        quitConfirmationOpen = false;
        ApplyQuitConfirmationStateToAllViews();
        ClearSelectedButton();
    }

    public void OnQuitYesPressed()
    {
        if (isTransitioning)
            return;

        StartCoroutine(QuitToMainMenuRoutine());
    }

    private IEnumerator QuitToMainMenuRoutine()
    {
        isTransitioning = true;
        quitConfirmationOpen = false;

        ClearSelectedButton();
        ApplyQuitConfirmationStateToAllViews();

        Time.timeScale = 1f;

        if (gameplayController != null)
            gameplayController.SetGameplayPaused(true);

        SFXManager.Instance?.StopLoop();

        AudioManager.Instance?.StopMusic(false);

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

        if (clearMatchSetupOnQuit && GameSessionManager.Instance != null)
            GameSessionManager.Instance.ClearMatchSetup();

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void HandleLayoutChanged(bool isPortrait)
    {
        if (!isPaused)
            return;

        RefreshLayoutVisibility();
        ApplyQuitConfirmationStateToAllViews();
        ClearSelectedButton();
    }

    private void RefreshLayoutVisibility()
    {
        bool isPortrait = UILayoutController.Instance != null && UILayoutController.Instance.IsPortrait;

        SetViewRootActive(landscapeView, !isPortrait && isPaused);
        SetViewRootActive(portraitView, isPortrait && isPaused);
    }

    private void ResetQuitConfirmationState()
    {
        quitConfirmationOpen = false;
        ApplyQuitConfirmationStateToAllViews();
    }

    private void ApplyQuitConfirmationStateToAllViews()
    {
        ApplyQuitConfirmationState(landscapeView);
        ApplyQuitConfirmationState(portraitView);
    }

    private void ApplyQuitConfirmationState(GameplayPauseMenuView view)
    {
        if (view == null)
            return;

        if (view.QuitButton != null)
            view.QuitButton.interactable = !quitConfirmationOpen && !isTransitioning;

        if (view.QuitConfirmRoot != null)
            view.QuitConfirmRoot.SetActive(quitConfirmationOpen && !isTransitioning);
    }

    private void SetViewRootActive(GameplayPauseMenuView view, bool active)
    {
        if (view == null || view.Root == null)
            return;

        view.Root.SetActive(active);
    }

    private void ClearSelectedButton()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }
}