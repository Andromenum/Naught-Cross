using UnityEngine;

public class GameSelectPopupController : PopupControllerBase
{
    [Header("Scene Names")]
    [SerializeField] private string gameplaySceneName = "Gameplay";
    [SerializeField] private string hardModeSceneName = "HardModeScene";

    private ProfilesPopupController profilesPopupController;

    protected override void Awake()
    {
        profilesPopupController = GetComponent<ProfilesPopupController>();
        base.Awake();
    }

    public void OnStartGamePressed()
    {
        StartGameWithScene(gameplaySceneName);
    }

    public void OnStartHardModePressed()
    {
        StartGameWithScene(hardModeSceneName);
    }

    private void StartGameWithScene(string sceneName)
    {
        if (profilesPopupController == null)
        {
            Debug.LogWarning("ProfilesPopupController not found on the same GameObject.");
            return;
        }

        if (GameSessionManager.Instance == null)
        {
            Debug.LogWarning("GameSessionManager instance not found.");
            return;
        }

        if (!profilesPopupController.AreBothPlayersReady())
        {
            Debug.LogWarning("Both players must be ready before starting the game.");
            return;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("Cannot start game. Scene name is empty.");
            return;
        }

        MatchPlayerData player1 = profilesPopupController.BuildPlayer1MatchData();
        MatchPlayerData player2 = profilesPopupController.BuildPlayer2MatchData();

        if (player1 == null || player2 == null)
        {
            Debug.LogWarning("Failed to build match player data.");
            return;
        }

        GameSessionManager.Instance.SetMatchSetup(player1, player2);
        GameSessionManager.Instance.LoadGameplayScene(sceneName);
    }
}