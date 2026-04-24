using System;

[Serializable]
public class PlayerProfileData
{
    public bool isUsed;
    public string playerName;
    public int iconIndex;

    public int totalGamesPlayed;
    public int wins;
    public int losses;
    public int draws;
    public float totalMatchDurationSeconds;

    public float GetAverageMatchDuration()
    {
        if (totalGamesPlayed <= 0)
            return 0f;

        return totalMatchDurationSeconds / totalGamesPlayed;
    }

    public void Clear()
    {
        isUsed = false;
        playerName = string.Empty;
        iconIndex = -1;
        totalGamesPlayed = 0;
        wins = 0;
        losses = 0;
        draws = 0;
        totalMatchDurationSeconds = 0f;
    }
}

[Serializable]
public class PlayerProfilesSaveData
{
    public PlayerProfileData[] slots = new PlayerProfileData[6];
}