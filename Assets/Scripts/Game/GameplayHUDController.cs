using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class PlayerHUDRefs
{
    public Image iconImage;
    public TMP_Text nameText;
    public GameObject turnIndicatorRoot;
}

[System.Serializable]
public class GameplayHUDView
{
    public GameObject root;
    public PlayerHUDRefs player1;
    public PlayerHUDRefs player2;
}

public class GameplayHUDController : MonoBehaviour
{
    [Header("Libraries")]
    [SerializeField] private ProfileIconLibrary iconLibrary;

    [Header("Landscape View")]
    [SerializeField] private GameplayHUDView landscapeView;

    [Header("Portrait View")]
    [SerializeField] private GameplayHUDView portraitView;

    private string player1Name;
    private string player2Name;
    private Sprite player1Icon;
    private Sprite player2Icon;

    private bool hasData;
    private int currentTurnPlayer = 1;

    private void Awake()
    {
        if (landscapeView != null && landscapeView.root != null)
            landscapeView.root.SetActive(false);

        if (portraitView != null && portraitView.root != null)
            portraitView.root.SetActive(false);
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

    private void Start()
    {
        RefreshLayoutVisibility();
        RefreshAllViews();
        RefreshTurnIndicators();
    }

    public void LoadFromSession()
    {
        if (GameSessionManager.Instance == null || !GameSessionManager.Instance.HasValidMatchSetup)
        {
            Debug.LogWarning("GameplayHUDController: Match setup is invalid.");
            return;
        }

        MatchPlayerData player1Data = GameSessionManager.Instance.Player1;
        MatchPlayerData player2Data = GameSessionManager.Instance.Player2;

        if (player1Data == null || player2Data == null)
        {
            Debug.LogWarning("GameplayHUDController: Player data is invalid.");
            return;
        }

        player1Name = player1Data.playerName;
        player2Name = player2Data.playerName;

        player1Icon = iconLibrary != null ? iconLibrary.GetIcon(player1Data.iconIndex) : null;
        player2Icon = iconLibrary != null ? iconLibrary.GetIcon(player2Data.iconIndex) : null;

        hasData = true;

        RefreshAllViews();
        RefreshTurnIndicators();
    }

    public void SetupHUD(string newPlayer1Name, Sprite newPlayer1Icon, string newPlayer2Name, Sprite newPlayer2Icon)
    {
        player1Name = newPlayer1Name;
        player1Icon = newPlayer1Icon;

        player2Name = newPlayer2Name;
        player2Icon = newPlayer2Icon;

        hasData = true;

        RefreshAllViews();
        RefreshTurnIndicators();
    }

    public void SetCurrentTurnToPlayer1()
    {
        currentTurnPlayer = 1;
        RefreshTurnIndicators();
    }

    public void SetCurrentTurnToPlayer2()
    {
        currentTurnPlayer = 2;
        RefreshTurnIndicators();
    }

    public void SetCurrentTurn(int playerIndex)
    {
        currentTurnPlayer = playerIndex;
        RefreshTurnIndicators();
    }

    public void ClearTurnIndicators()
    {
        SetIndicatorState(landscapeView != null ? landscapeView.player1 : null, false);
        SetIndicatorState(landscapeView != null ? landscapeView.player2 : null, false);

        SetIndicatorState(portraitView != null ? portraitView.player1 : null, false);
        SetIndicatorState(portraitView != null ? portraitView.player2 : null, false);
    }

    private void HandleLayoutChanged(bool isPortrait)
    {
        RefreshLayoutVisibility();
        RefreshAllViews();
        RefreshTurnIndicators();
    }

    private void RefreshLayoutVisibility()
    {
        bool isPortrait = UILayoutController.Instance != null && UILayoutController.Instance.IsPortrait;

        if (landscapeView != null && landscapeView.root != null)
            landscapeView.root.SetActive(!isPortrait);

        if (portraitView != null && portraitView.root != null)
            portraitView.root.SetActive(isPortrait);
    }

    private void RefreshAllViews()
    {
        RefreshView(landscapeView);
        RefreshView(portraitView);
    }

    private void RefreshView(GameplayHUDView view)
    {
        if (view == null)
            return;

        if (!hasData)
            return;

        ApplyPlayerView(view.player1, player1Name, player1Icon);
        ApplyPlayerView(view.player2, player2Name, player2Icon);
    }

    private void ApplyPlayerView(PlayerHUDRefs playerView, string playerNameValue, Sprite playerIconValue)
    {
        if (playerView == null)
            return;

        if (playerView.nameText != null)
            playerView.nameText.text = playerNameValue;

        if (playerView.iconImage != null)
            playerView.iconImage.sprite = playerIconValue;
    }

    private void RefreshTurnIndicators()
    {
        RefreshTurnIndicatorForView(landscapeView);
        RefreshTurnIndicatorForView(portraitView);
    }

    private void RefreshTurnIndicatorForView(GameplayHUDView view)
    {
        if (view == null)
            return;

        SetIndicatorState(view.player1, currentTurnPlayer == 1);
        SetIndicatorState(view.player2, currentTurnPlayer == 2);
    }

    private void SetIndicatorState(PlayerHUDRefs playerView, bool isActive)
    {
        if (playerView == null)
            return;

        if (playerView.turnIndicatorRoot != null)
            playerView.turnIndicatorRoot.SetActive(isActive);
    }
}