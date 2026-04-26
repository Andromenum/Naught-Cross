using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class PlayerHUDRefs
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private GameObject turnIndicatorRoot;
    [SerializeField] private TMP_Text turnCounterText;
    [SerializeField] private TMP_Text hardModeTimerText;

    public Image IconImage => iconImage;
    public TMP_Text NameText => nameText;
    public GameObject TurnIndicatorRoot => turnIndicatorRoot;
    public TMP_Text TurnCounterText => turnCounterText;
    public TMP_Text HardModeTimerText => hardModeTimerText;
    public RectTransform HardModeTimerRect => hardModeTimerText != null ? hardModeTimerText.rectTransform : null;
}

[System.Serializable]
public class GameplayHUDView
{
    [SerializeField] private GameObject root;

    [Header("Players")]
    [SerializeField] private PlayerHUDRefs player1;
    [SerializeField] private PlayerHUDRefs player2;

    [Header("Match Info")]
    [SerializeField] private TMP_Text elapsedTimeText;
    [SerializeField] private TMP_Text countdownText;

    public GameObject Root => root;
    public PlayerHUDRefs Player1 => player1;
    public PlayerHUDRefs Player2 => player2;
    public TMP_Text ElapsedTimeText => elapsedTimeText;
    public TMP_Text CountdownText => countdownText;
}

public class GameplayHUDController : MonoBehaviour
{
    [Header("Libraries")]
    [SerializeField] private ProfileIconLibrary iconLibrary;

    [Header("Landscape View")]
    [SerializeField] private GameplayHUDView landscapeView;

    [Header("Portrait View")]
    [SerializeField] private GameplayHUDView portraitView;

    [Header("Gameplay Menu Buttons")]
    [SerializeField] private Button landscapeMenuButton;
    [SerializeField] private Button portraitMenuButton;

    [Header("Icon States")]
    [SerializeField] private Color activeIconColor = Color.white;
    [SerializeField] private Color inactiveIconColor = new Color(0.35f, 0.35f, 0.35f, 1f);

    [Header("Hard Mode Timer Intro")]
    [SerializeField] private float timerIntroStartScale = 2.75f;
    [SerializeField] private float timerIntroOvershootScale = 1.15f;
    [SerializeField] private float timerIntroDuration = 2.6f;
    [SerializeField, Range(0.1f, 0.95f)] private float timerIntroOvershootPoint = 0.78f;

    [Header("Hard Mode Timer Intro Start Offsets")]
    [SerializeField] private Vector2 landscapePlayer1TimerOffset;
    [SerializeField] private Vector2 landscapePlayer2TimerOffset;
    [SerializeField] private Vector2 portraitPlayer1TimerOffset;
    [SerializeField] private Vector2 portraitPlayer2TimerOffset;

    private readonly Dictionary<RectTransform, Vector3> hardModeTimerBaseScales = new Dictionary<RectTransform, Vector3>();
    private readonly Dictionary<RectTransform, Vector2> hardModeTimerBasePositions = new Dictionary<RectTransform, Vector2>();

    private string player1Name;
    private string player2Name;
    private Sprite player1Icon;
    private Sprite player2Icon;

    private bool hasData;
    private int currentTurnPlayer = 1;

    private bool hardModeTimersShouldBeVisible;
    private bool hardModeTimerValuesInitialized;
    private float cachedPlayer1HardModeSeconds;
    private float cachedPlayer2HardModeSeconds;

    private Coroutine hardModeTimerIntroRoutine;
    private bool hardModeTimerIntroPlaying;

    private void Awake()
    {
        if (landscapeView != null && landscapeView.Root != null)
            landscapeView.Root.SetActive(false);

        if (portraitView != null && portraitView.Root != null)
            portraitView.Root.SetActive(false);

        CacheHardModeTimerBaseTransforms();
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

        StopHardModeTimerIntro();
    }

    private void Start()
    {
        RefreshLayoutVisibility();
        RefreshAllViews();
        RefreshTurnIndicators();
        HideCountdown();

        if (!hardModeTimerValuesInitialized)
            SetHardModePlayerTimers(0f, 0f);

        ApplyHardModeTimerStateToAllViews();
    }

    private void OnValidate()
    {
        timerIntroStartScale = Mathf.Max(1f, timerIntroStartScale);
        timerIntroOvershootScale = Mathf.Max(1f, timerIntroOvershootScale);
        timerIntroDuration = Mathf.Max(0f, timerIntroDuration);
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
        ApplyHardModeTimerStateToAllViews();
    }

    public void SetupHUD(string newPlayer1Name, Sprite newPlayer1Icon, string newPlayer2Name, Sprite newPlayer2Icon)
    {
        player1Name = newPlayer1Name;
        player1Icon = newPlayer1Icon;

        player2Name = newPlayer2Name;
        player2Icon = newPlayer2Icon;

        hasData = true;

        RefreshLayoutVisibility();
        RefreshAllViews();
        RefreshTurnIndicators();
        ApplyHardModeTimerStateToAllViews();
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

        SetPlayerTurnCounter(landscapeView != null ? landscapeView.Player1 : null, p1Value);
        SetPlayerTurnCounter(landscapeView != null ? landscapeView.Player2 : null, p2Value);

        SetPlayerTurnCounter(portraitView != null ? portraitView.Player1 : null, p1Value);
        SetPlayerTurnCounter(portraitView != null ? portraitView.Player2 : null, p2Value);
    }

    public void SetHardModePlayerTimersVisible(bool visible)
    {
        hardModeTimersShouldBeVisible = visible;

        if (!visible)
            StopHardModeTimerIntro();

        ApplyHardModeTimerVisibilityToAllViews();

        if (!hardModeTimerIntroPlaying)
            ResetAllHardModeTimerTransforms();
    }

    public void SetHardModePlayerTimers(float player1Seconds, float player2Seconds)
    {
        cachedPlayer1HardModeSeconds = Mathf.Max(0f, player1Seconds);
        cachedPlayer2HardModeSeconds = Mathf.Max(0f, player2Seconds);
        hardModeTimerValuesInitialized = true;

        ApplyHardModeTimerTextToAllViews();

        if (!hardModeTimerIntroPlaying)
            ResetAllHardModeTimerTransforms();
    }

    public void SetHardModeTurnTimerVisible(bool visible)
    {
        SetHardModePlayerTimersVisible(visible);
    }

    public void SetHardModeTurnTimer(float seconds)
    {
        SetHardModePlayerTimers(seconds, seconds);
    }

    public void PlayHardModeTimerIntro()
    {
        if (!hardModeTimerValuesInitialized)
            SetHardModePlayerTimers(0f, 0f);

        CacheHardModeTimerBaseTransforms();

        if (hardModeTimerIntroRoutine != null)
            StopCoroutine(hardModeTimerIntroRoutine);

        hardModeTimerIntroPlaying = true;

        SetHardModePlayerTimersVisible(true);
        SetAllHardModeTimerScales(timerIntroStartScale);
        ApplyAllHardModeTimerOffsets(1f);

        if (timerIntroDuration <= 0f)
        {
            hardModeTimerIntroPlaying = false;
            ResetAllHardModeTimerTransforms();
            return;
        }

        hardModeTimerIntroRoutine = StartCoroutine(HardModeTimerIntroRoutine());
    }

    public void StopHardModeTimerIntro()
    {
        if (hardModeTimerIntroRoutine != null)
        {
            StopCoroutine(hardModeTimerIntroRoutine);
            hardModeTimerIntroRoutine = null;
        }

        hardModeTimerIntroPlaying = false;
        ResetAllHardModeTimerTransforms();
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

    public void SetGameplayMenuButtonInteractable(bool interactable)
    {
        if (landscapeMenuButton != null)
            landscapeMenuButton.interactable = interactable;

        if (portraitMenuButton != null)
            portraitMenuButton.interactable = interactable;
    }

    public void ClearTurnIndicators()
    {
        SetIndicatorState(landscapeView != null ? landscapeView.Player1 : null, false);
        SetIndicatorState(landscapeView != null ? landscapeView.Player2 : null, false);

        SetIndicatorState(portraitView != null ? portraitView.Player1 : null, false);
        SetIndicatorState(portraitView != null ? portraitView.Player2 : null, false);

        SetIconColor(landscapeView != null ? landscapeView.Player1 : null, activeIconColor);
        SetIconColor(landscapeView != null ? landscapeView.Player2 : null, activeIconColor);

        SetIconColor(portraitView != null ? portraitView.Player1 : null, activeIconColor);
        SetIconColor(portraitView != null ? portraitView.Player2 : null, activeIconColor);
    }

    private IEnumerator HardModeTimerIntroRoutine()
    {
        float elapsed = 0f;

        while (elapsed < timerIntroDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float normalized = Mathf.Clamp01(elapsed / timerIntroDuration);

            float scaleMultiplier = EvaluateTimerIntroScale(normalized);
            float offsetMultiplier = EvaluateTimerIntroOffset(normalized);

            SetAllHardModeTimerScales(scaleMultiplier);
            ApplyAllHardModeTimerOffsets(offsetMultiplier);

            yield return null;
        }

        hardModeTimerIntroPlaying = false;
        ResetAllHardModeTimerTransforms();

        hardModeTimerIntroRoutine = null;
    }

    private float EvaluateTimerIntroScale(float normalized)
    {
        float split = Mathf.Clamp(timerIntroOvershootPoint, 0.1f, 0.95f);

        if (normalized <= split)
        {
            float localT = Mathf.Clamp01(normalized / split);
            float easedT = 1f - Mathf.Pow(1f - localT, 3f);

            return Mathf.Lerp(timerIntroStartScale, timerIntroOvershootScale, easedT);
        }

        float settleT = Mathf.Clamp01((normalized - split) / (1f - split));
        float easedSettleT = Mathf.SmoothStep(0f, 1f, settleT);

        return Mathf.Lerp(timerIntroOvershootScale, 1f, easedSettleT);
    }

    private float EvaluateTimerIntroOffset(float normalized)
    {
        float t = Mathf.Clamp01(normalized);
        float easedT = Mathf.SmoothStep(0f, 1f, t);

        return Mathf.Lerp(1f, 0f, easedT);
    }

    private void SetAllHardModeTimerScales(float scaleMultiplier)
    {
        SetHardModeTimerScale(landscapeView != null ? landscapeView.Player1 : null, scaleMultiplier);
        SetHardModeTimerScale(landscapeView != null ? landscapeView.Player2 : null, scaleMultiplier);

        SetHardModeTimerScale(portraitView != null ? portraitView.Player1 : null, scaleMultiplier);
        SetHardModeTimerScale(portraitView != null ? portraitView.Player2 : null, scaleMultiplier);
    }

    private void SetHardModeTimerScale(PlayerHUDRefs playerView, float scaleMultiplier)
    {
        if (playerView == null || playerView.HardModeTimerRect == null)
            return;

        RectTransform timerRect = playerView.HardModeTimerRect;
        CacheHardModeTimerBaseTransform(timerRect);

        if (!hardModeTimerBaseScales.TryGetValue(timerRect, out Vector3 baseScale))
            baseScale = Vector3.one;

        timerRect.localScale = baseScale * scaleMultiplier;
    }

    private void CacheHardModeTimerBaseTransforms()
    {
        CacheHardModeTimerBaseTransform(landscapeView != null ? landscapeView.Player1 : null);
        CacheHardModeTimerBaseTransform(landscapeView != null ? landscapeView.Player2 : null);

        CacheHardModeTimerBaseTransform(portraitView != null ? portraitView.Player1 : null);
        CacheHardModeTimerBaseTransform(portraitView != null ? portraitView.Player2 : null);
    }

    private void CacheHardModeTimerBaseTransform(PlayerHUDRefs playerView)
    {
        if (playerView == null)
            return;

        CacheHardModeTimerBaseTransform(playerView.HardModeTimerRect);
    }

    private void CacheHardModeTimerBaseTransform(RectTransform timerRect)
    {
        if (timerRect == null)
            return;

        if (!hardModeTimerBaseScales.ContainsKey(timerRect))
            hardModeTimerBaseScales.Add(timerRect, timerRect.localScale);

        if (!hardModeTimerBasePositions.ContainsKey(timerRect))
            hardModeTimerBasePositions.Add(timerRect, timerRect.anchoredPosition);
    }

    private void ApplyHardModeTimerStateToAllViews()
    {
        ApplyHardModeTimerTextToAllViews();
        ApplyHardModeTimerVisibilityToAllViews();

        if (!hardModeTimerIntroPlaying)
            ResetAllHardModeTimerTransforms();
    }

    private void ApplyHardModeTimerTextToAllViews()
    {
        string p1Value = "TIME " + cachedPlayer1HardModeSeconds.ToString("0.0") + "s";
        string p2Value = "TIME " + cachedPlayer2HardModeSeconds.ToString("0.0") + "s";

        SetHardModePlayerTimer(landscapeView != null ? landscapeView.Player1 : null, p1Value);
        SetHardModePlayerTimer(landscapeView != null ? landscapeView.Player2 : null, p2Value);

        SetHardModePlayerTimer(portraitView != null ? portraitView.Player1 : null, p1Value);
        SetHardModePlayerTimer(portraitView != null ? portraitView.Player2 : null, p2Value);
    }

    private void ApplyHardModeTimerVisibilityToAllViews()
    {
        SetHardModePlayerTimerVisible(
            landscapeView != null ? landscapeView.Player1 : null,
            hardModeTimersShouldBeVisible);

        SetHardModePlayerTimerVisible(
            landscapeView != null ? landscapeView.Player2 : null,
            hardModeTimersShouldBeVisible);

        SetHardModePlayerTimerVisible(
            portraitView != null ? portraitView.Player1 : null,
            hardModeTimersShouldBeVisible);

        SetHardModePlayerTimerVisible(
            portraitView != null ? portraitView.Player2 : null,
            hardModeTimersShouldBeVisible);
    }

    private void ApplyAllHardModeTimerOffsets(float offsetMultiplier)
    {
        ApplyHardModeTimerOffset(
            landscapeView != null ? landscapeView.Player1 : null,
            landscapePlayer1TimerOffset,
            offsetMultiplier);

        ApplyHardModeTimerOffset(
            landscapeView != null ? landscapeView.Player2 : null,
            landscapePlayer2TimerOffset,
            offsetMultiplier);

        ApplyHardModeTimerOffset(
            portraitView != null ? portraitView.Player1 : null,
            portraitPlayer1TimerOffset,
            offsetMultiplier);

        ApplyHardModeTimerOffset(
            portraitView != null ? portraitView.Player2 : null,
            portraitPlayer2TimerOffset,
            offsetMultiplier);
    }

    private void ApplyHardModeTimerOffset(PlayerHUDRefs playerView, Vector2 offset, float offsetMultiplier)
    {
        if (playerView == null || playerView.HardModeTimerRect == null)
            return;

        RectTransform timerRect = playerView.HardModeTimerRect;
        CacheHardModeTimerBaseTransform(timerRect);

        if (!hardModeTimerBasePositions.TryGetValue(timerRect, out Vector2 basePosition))
            return;

        timerRect.anchoredPosition = basePosition + (offset * offsetMultiplier);
    }

    private void ResetAllHardModeTimerTransforms()
    {
        SetAllHardModeTimerScales(1f);
        ApplyAllHardModeTimerOffsets(0f);
    }

    private void HandleLayoutChanged(bool isPortrait)
    {
        RefreshLayoutVisibility();
        RefreshAllViews();
        RefreshTurnIndicators();
        ApplyHardModeTimerStateToAllViews();
    }

    private void RefreshLayoutVisibility()
    {
        bool isPortrait = UILayoutController.Instance != null && UILayoutController.Instance.IsPortrait;

        if (landscapeView != null && landscapeView.Root != null)
            landscapeView.Root.SetActive(!isPortrait);

        if (portraitView != null && portraitView.Root != null)
            portraitView.Root.SetActive(isPortrait);
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

        ApplyPlayerView(view.Player1, player1Name, player1Icon);
        ApplyPlayerView(view.Player2, player2Name, player2Icon);
    }

    private void ApplyPlayerView(PlayerHUDRefs playerView, string playerNameValue, Sprite playerIconValue)
    {
        if (playerView == null)
            return;

        if (playerView.NameText != null)
            playerView.NameText.text = playerNameValue;

        if (playerView.IconImage != null)
            playerView.IconImage.sprite = playerIconValue;
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

        SetIndicatorState(view.Player1, player1Active);
        SetIndicatorState(view.Player2, player2Active);

        SetIconColor(view.Player1, player1Active ? activeIconColor : inactiveIconColor);
        SetIconColor(view.Player2, player2Active ? activeIconColor : inactiveIconColor);
    }

    private void SetIndicatorState(PlayerHUDRefs playerView, bool isActive)
    {
        if (playerView == null || playerView.TurnIndicatorRoot == null)
            return;

        if (isActive)
            playerView.TurnIndicatorRoot.SetActive(true);

        TurnIndicatorPulse[] pulses = playerView.TurnIndicatorRoot.GetComponentsInChildren<TurnIndicatorPulse>(true);

        for (int i = 0; i < pulses.Length; i++)
        {
            if (pulses[i] != null)
                pulses[i].SetVisualActive(isActive);
        }

        if (!isActive)
            playerView.TurnIndicatorRoot.SetActive(false);
    }

    private void SetIconColor(PlayerHUDRefs playerView, Color color)
    {
        if (playerView == null || playerView.IconImage == null)
            return;

        playerView.IconImage.color = color;
    }

    private void SetElapsedTimeForView(GameplayHUDView view, string value)
    {
        if (view == null || view.ElapsedTimeText == null)
            return;

        view.ElapsedTimeText.text = value;
    }

    private void SetPlayerTurnCounter(PlayerHUDRefs playerView, string value)
    {
        if (playerView == null || playerView.TurnCounterText == null)
            return;

        playerView.TurnCounterText.text = value;
    }

    private void SetCountdownForView(GameplayHUDView view, string value, bool visible)
    {
        if (view == null || view.CountdownText == null)
            return;

        view.CountdownText.text = value;
        view.CountdownText.gameObject.SetActive(visible);
    }

    private void SetHardModePlayerTimerVisible(PlayerHUDRefs playerView, bool visible)
    {
        if (playerView == null || playerView.HardModeTimerText == null)
            return;

        playerView.HardModeTimerText.gameObject.SetActive(visible);
    }

    private void SetHardModePlayerTimer(PlayerHUDRefs playerView, string value)
    {
        if (playerView == null || playerView.HardModeTimerText == null)
            return;

        playerView.HardModeTimerText.text = value;
    }

    private string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int minutes = totalSeconds / 60;
        int remainingSeconds = totalSeconds % 60;

        return minutes.ToString("00") + ":" + remainingSeconds.ToString("00");
    }
}