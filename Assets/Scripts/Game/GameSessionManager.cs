using UnityEngine;
using UnityEngine.SceneManagement;

public enum PlayerMarkType
{
    X,
    O
}

[System.Serializable]
public class MatchPlayerData
{
    public int profileSlotIndex;
    public string playerName;
    public int iconIndex;
    public int markVariantIndex;
    public PlayerMarkType markType;

    public Sprite selectedMarkSprite;
}

public class GameSessionManager : MonoBehaviour
{
    public static GameSessionManager Instance { get; private set; }

    [Header("Default Session Settings")]
    [SerializeField] private TicTacToeGameMode defaultGameMode = TicTacToeGameMode.Classic;

    private TicTacToeGameMode selectedGameMode;

    public MatchPlayerData Player1 { get; private set; }
    public MatchPlayerData Player2 { get; private set; }

    public TicTacToeGameMode SelectedGameMode => selectedGameMode;
    public bool IsHardMode => selectedGameMode == TicTacToeGameMode.Hard;

    public bool HasValidMatchSetup => Player1 != null && Player2 != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        selectedGameMode = defaultGameMode;
    }

    public void SetMatchSetup(MatchPlayerData player1, MatchPlayerData player2)
    {
        SetMatchSetup(player1, player2, defaultGameMode);
    }

    public void SetMatchSetup(MatchPlayerData player1, MatchPlayerData player2, TicTacToeGameMode gameMode)
    {
        Player1 = CopyPlayerData(player1);
        Player2 = CopyPlayerData(player2);
        selectedGameMode = gameMode;
    }

    public void ClearMatchSetup()
    {
        Player1 = null;
        Player2 = null;
        selectedGameMode = defaultGameMode;
    }

    public void LoadGameplayScene(string sceneName)
    {
        if (!HasValidMatchSetup)
        {
            Debug.LogWarning("Cannot load gameplay scene. Match setup is incomplete.");
            return;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("Cannot load gameplay scene. Scene name is empty.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    private MatchPlayerData CopyPlayerData(MatchPlayerData source)
    {
        if (source == null)
            return null;

        return new MatchPlayerData
        {
            profileSlotIndex = source.profileSlotIndex,
            playerName = source.playerName,
            iconIndex = source.iconIndex,
            markVariantIndex = source.markVariantIndex,
            markType = source.markType,
            selectedMarkSprite = source.selectedMarkSprite
        };
    }
}