using System;
using System.IO;
using UnityEngine;

public class PlayerProfilesManager : MonoBehaviour
{
    public static PlayerProfilesManager Instance { get; private set; }

    public const string NoFreeSlotsError = "No empty slots left.";

    public event Action ProfilesChanged;

    [SerializeField] private int slotCount = 6;
    [SerializeField] private int maxProfileNameLength = 16;

    private PlayerProfilesSaveData saveData = new PlayerProfilesSaveData();

    private string SavePath => Path.Combine(Application.persistentDataPath, "player_profiles.json");

    public int SlotCount => slotCount;
    public int MaxProfileNameLength => maxProfileNameLength;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadProfiles();
    }

    public PlayerProfileData GetProfileAt(int slotIndex)
    {
        if (!IsValidSlot(slotIndex))
            return null;

        return saveData.slots[slotIndex];
    }

    public bool HasProfileAt(int slotIndex)
    {
        if (!IsValidSlot(slotIndex))
            return false;

        PlayerProfileData profile = saveData.slots[slotIndex];
        return profile != null && profile.isUsed;
    }

    public bool CanCreateProfile()
    {
        EnsureSlotsInitialized();
        return GetFirstEmptySlotIndex() >= 0;
    }

    public bool TryCreateProfile(string rawName, int iconIndex, out string errorMessage)
    {
        EnsureSlotsInitialized();

        string trimmedName = NormalizeName(rawName);

        if (!CanCreateProfile())
        {
            errorMessage = NoFreeSlotsError;
            return false;
        }

        if (iconIndex < 0)
        {
            errorMessage = "Select an icon";
            return false;
        }

        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            errorMessage = "Enter a name";
            return false;
        }

        if (trimmedName.Length > maxProfileNameLength)
        {
            errorMessage = $"Name is too long. Max {maxProfileNameLength} characters";
            return false;
        }

        if (IsDuplicateProfile(trimmedName, iconIndex))
        {
            errorMessage = "That profile already exists";
            return false;
        }

        int firstEmptyIndex = GetFirstEmptySlotIndex();

        if (firstEmptyIndex < 0)
        {
            errorMessage = NoFreeSlotsError;
            return false;
        }

        PlayerProfileData profile = saveData.slots[firstEmptyIndex];
        profile.isUsed = true;
        profile.playerName = trimmedName;
        profile.iconIndex = iconIndex;

        SaveProfiles();
        ProfilesChanged?.Invoke();

        errorMessage = string.Empty;
        return true;
    }

    public void DeleteProfileAt(int slotIndex)
    {
        EnsureSlotsInitialized();

        if (!IsValidSlot(slotIndex))
            return;

        if (!HasProfileAt(slotIndex))
            return;

        for (int i = slotIndex; i < saveData.slots.Length - 1; i++)
            saveData.slots[i] = saveData.slots[i + 1];

        saveData.slots[saveData.slots.Length - 1] = CreateEmptyProfile();

        SaveProfiles();
        ProfilesChanged?.Invoke();
    }

    public bool IsDuplicateProfile(string rawName, int iconIndex)
    {
        EnsureSlotsInitialized();

        string normalizedName = NormalizeName(rawName);

        for (int i = 0; i < saveData.slots.Length; i++)
        {
            PlayerProfileData profile = saveData.slots[i];

            if (profile == null || !profile.isUsed)
                continue;

            bool sameName = string.Equals(
                NormalizeName(profile.playerName),
                normalizedName,
                StringComparison.OrdinalIgnoreCase);

            bool sameIcon = profile.iconIndex == iconIndex;

            if (sameName && sameIcon)
                return true;
        }

        return false;
    }

    public void LoadProfiles()
    {
        if (!File.Exists(SavePath))
        {
            saveData = new PlayerProfilesSaveData();
            EnsureSlotsInitialized();
            return;
        }

        try
        {
            string json = File.ReadAllText(SavePath);

            if (string.IsNullOrWhiteSpace(json))
            {
                saveData = new PlayerProfilesSaveData();
            }
            else
            {
                saveData = JsonUtility.FromJson<PlayerProfilesSaveData>(json);
            }

            if (saveData == null)
                saveData = new PlayerProfilesSaveData();

            EnsureSlotsInitialized();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Failed to load profiles: " + ex.Message);
            saveData = new PlayerProfilesSaveData();
            EnsureSlotsInitialized();
        }
    }

    public void SaveProfiles()
    {
        try
        {
            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(SavePath, json);
            WebGLSaveSync.Flush();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Failed to save profiles: " + ex.Message);
        }
    }

    private void EnsureSlotsInitialized()
    {
        if (saveData.slots == null || saveData.slots.Length != slotCount)
        {
            PlayerProfileData[] oldSlots = saveData.slots;
            saveData.slots = new PlayerProfileData[slotCount];

            for (int i = 0; i < slotCount; i++)
            {
                if (oldSlots != null && i < oldSlots.Length && oldSlots[i] != null)
                    saveData.slots[i] = oldSlots[i];
                else
                    saveData.slots[i] = CreateEmptyProfile();
            }
        }

        for (int i = 0; i < saveData.slots.Length; i++)
        {
            if (saveData.slots[i] == null)
                saveData.slots[i] = CreateEmptyProfile();
        }
    }

    private PlayerProfileData CreateEmptyProfile()
    {
        PlayerProfileData profile = new PlayerProfileData();
        profile.Clear();
        return profile;
    }

    private int GetFirstEmptySlotIndex()
    {
        for (int i = 0; i < saveData.slots.Length; i++)
        {
            if (saveData.slots[i] == null || !saveData.slots[i].isUsed)
                return i;
        }

        return -1;
    }

    private bool IsValidSlot(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < slotCount;
    }

    private string NormalizeName(string rawName)
    {
        return rawName != null ? rawName.Trim() : string.Empty;
    }

    public void RecordMatchWinnerLoser(int winnerSlotIndex, int loserSlotIndex, float matchDurationSeconds)
    {
        EnsureSlotsInitialized();

        if (!IsValidSlot(winnerSlotIndex) || !IsValidSlot(loserSlotIndex))
            return;

        if (winnerSlotIndex == loserSlotIndex)
            return;

        PlayerProfileData winner = saveData.slots[winnerSlotIndex];
        PlayerProfileData loser = saveData.slots[loserSlotIndex];

        if (winner == null || !winner.isUsed || loser == null || !loser.isUsed)
            return;

        float safeDuration = Mathf.Max(0f, matchDurationSeconds);

        winner.totalGamesPlayed++;
        winner.wins++;
        winner.totalMatchDurationSeconds += safeDuration;

        loser.totalGamesPlayed++;
        loser.losses++;
        loser.totalMatchDurationSeconds += safeDuration;

        SaveProfiles();
        ProfilesChanged?.Invoke();
    }

    public void RecordMatchDraw(int player1SlotIndex, int player2SlotIndex, float matchDurationSeconds)
    {
        EnsureSlotsInitialized();

        if (!IsValidSlot(player1SlotIndex) || !IsValidSlot(player2SlotIndex))
            return;

        if (player1SlotIndex == player2SlotIndex)
            return;

        PlayerProfileData player1 = saveData.slots[player1SlotIndex];
        PlayerProfileData player2 = saveData.slots[player2SlotIndex];

        if (player1 == null || !player1.isUsed || player2 == null || !player2.isUsed)
            return;

        float safeDuration = Mathf.Max(0f, matchDurationSeconds);

        player1.totalGamesPlayed++;
        player1.draws++;
        player1.totalMatchDurationSeconds += safeDuration;

        player2.totalGamesPlayed++;
        player2.draws++;
        player2.totalMatchDurationSeconds += safeDuration;

        SaveProfiles();
        ProfilesChanged?.Invoke();
    }
}