using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class PlayerHUDRefs
{
    public Image iconImage;
    public TMP_Text nameText;
    public GameObject turnIndicatorRoot;
    public TMP_Text turnCounterText;
}

[System.Serializable]
public class GameplayHUDView
{
    public GameObject root;

    [Header("Players")]
    public PlayerHUDRefs player1;
    public PlayerHUDRefs player2;

    [Header("Match Info")]
    public TMP_Text elapsedTimeText;
    public TMP_Text countdownText;
}

public class GameplayHUDController : MonoBehaviour
{
    [Header("Libraries")]
    [SerializeField] private ProfileIconLibrary iconLibrary;

    [Header("Landscape View")]
    [SerializeField] private GameplayHUDView landscapeView;

    [Header("Portrait View")]
    [SerializeField] private GameplayHUDView portraitView;

    [Header("Icon States")]
    [SerializeField] private Color activeIconColor = Color.white;
    [SerializeField] private Color inactiveIconColor = new Color(0.35f, 0.35f, 0.35f, 1f);

    [Header("Gameplay Menu Buttons")]
    [SerializeField] private Button landscapeMenuButton;
    [SerializeField] private Button portraitMenuButton;

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
        HideCountdown();
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

        RefreshLayoutVisibility();
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

    public void SetElapsedTime(float elapsedSeconds)
    {
        string formattedTime = FormatTime(elapsedSeconds);

        SetElapsedTimeForView(landscapeView, formattedTime);
        SetElapsedTimeForView(portraitView, formattedTime);
    }

    public void SetPlayerTurnCounters(int player1Turns, int player2Turns)
    {
        string p1Value = "TURNS " + player1Turns;
        string p2Value = "TURNS " + player2Turns;

        SetPlayerTurnCounter(landscapeView != null ? landscapeView.player1 : null, p1Value);
        SetPlayerTurnCounter(landscapeView != null ? landscapeView.player2 : null, p2Value);

        SetPlayerTurnCounter(portraitView != null ? portraitView.player1 : null, p1Value);
        SetPlayerTurnCounter(portraitView != null ? portraitView.player2 : null, p2Value);
    }

    public void ShowCountdown(string value)
    {
        SetCountdownForView(landscapeView, value, true);
        SetCountdownForView(portraitView, value, true);
    }

    public void HideCountdown()
    {
        SetCountdownForView(landscapeView, string.Empty, false);
        SetCountdownForView(portraitView, string.Empty, false);
    }

    public void ClearTurnIndicators()
    {
        SetIndicatorState(landscapeView != null ? landscapeView.player1 : null, false);
        SetIndicatorState(landscapeView != null ? landscapeView.player2 : null, false);

        SetIndicatorState(portraitView != null ? portraitView.player1 : null, false);
        SetIndicatorState(portraitView != null ? portraitView.player2 : null, false);

        SetIconColor(landscapeView != null ? landscapeView.player1 : null, activeIconColor);
        SetIconColor(landscapeView != null ? landscapeView.player2 : null, activeIconColor);

        SetIconColor(portraitView != null ? portraitView.player1 : null, activeIconColor);
        SetIconColor(portraitView != null ? portraitView.player2 : null, activeIconColor);
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

        bool player1Active = currentTurnPlayer == 1;
        bool player2Active = currentTurnPlayer == 2;

        SetIndicatorState(view.player1, player1Active);
        SetIndicatorState(view.player2, player2Active);

        SetIconColor(view.player1, player1Active ? activeIconColor : inactiveIconColor);
        SetIconColor(view.player2, player2Active ? activeIconColor : inactiveIconColor);
    }

    private void SetIndicatorState(PlayerHUDRefs playerView, bool isActive)
    {
        if (playerView == null || playerView.turnIndicatorRoot == null)
            return;

        if (isActive)
            playerView.turnIndicatorRoot.SetActive(true);

        TurnIndicatorPulse[] pulses = playerView.turnIndicatorRoot.GetComponentsInChildren<TurnIndicatorPulse>(true);

        for (int i = 0; i < pulses.Length; i++)
        {
            if (pulses[i] != null)
                pulses[i].SetVisualActive(isActive);
        }

        if (!isActive)
            playerView.turnIndicatorRoot.SetActive(false);
    }

    private void SetIconColor(PlayerHUDRefs playerView, Color color)
    {
        if (playerView == null || playerView.iconImage == null)
            return;

        playerView.iconImage.color = color;
    }

    private void SetElapsedTimeForView(GameplayHUDView view, string value)
    {
        if (view == null || view.elapsedTimeText == null)
            return;

        view.elapsedTimeText.text = value;
    }

    private void SetPlayerTurnCounter(PlayerHUDRefs playerView, string value)
    {
        if (playerView == null || playerView.turnCounterText == null)
            return;

        playerView.turnCounterText.text = value;
    }

    private void SetCountdownForView(GameplayHUDView view, string value, bool visible)
    {
        if (view == null || view.countdownText == null)
            return;

        view.countdownText.text = value;
        view.countdownText.gameObject.SetActive(visible);
    }

    private string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int minutes = totalSeconds / 60;
        int remainingSeconds = totalSeconds % 60;

        return minutes.ToString("00") + ":" + remainingSeconds.ToString("00");
    }

    public void SetGameplayMenuButtonInteractable(bool interactable)
    {
        if (landscapeMenuButton != null)
            landscapeMenuButton.interactable = interactable;

        if (portraitMenuButton != null)
            portraitMenuButton.interactable = interactable;
    }
}