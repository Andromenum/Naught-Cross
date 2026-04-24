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

    public MatchPlayerData Player1 { get; private set; }
    public MatchPlayerData Player2 { get; private set; }

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
    }

    public void SetMatchSetup(MatchPlayerData player1, MatchPlayerData player2)
    {
        Player1 = CopyPlayerData(player1);
        Player2 = CopyPlayerData(player2);
    }

    public void ClearMatchSetup()
    {
        Player1 = null;
        Player2 = null;
    }

    public void LoadGameplayScene(string sceneName)
    {
        if (!HasValidMatchSetup)
        {
            Debug.LogWarning("Cannot load gameplay scene. Match setup is incomplete.");
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