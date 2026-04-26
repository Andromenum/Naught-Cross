using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatsEntryUI : MonoBehaviour
{
    private Image iconImage;
    private TMP_Text nameText;
    private TMP_Text winsText;
    private TMP_Text totalMatchesText;
    private TMP_Text drawsText;
    private TMP_Text avgMatchTimeText;

    private bool refsCached;

    private void Awake()
    {
        EnsureReferences();
    }

    public void Setup(PlayerProfileData profile, Sprite iconSprite)
    {
        EnsureReferences();

        if (profile == null)
            return;

        if (iconImage != null)
            iconImage.sprite = iconSprite;

        if (nameText != null)
            nameText.text = profile.playerName;

        if (winsText != null)
            winsText.text = profile.wins.ToString();

        if (totalMatchesText != null)
            totalMatchesText.text = GetTotalMatches(profile).ToString();

        if (drawsText != null)
            drawsText.text = profile.draws.ToString();

        if (avgMatchTimeText != null)
            avgMatchTimeText.text = FormatTime(profile.GetAverageMatchDuration());
    }

    private int GetTotalMatches(PlayerProfileData profile)
    {
        return profile.wins + profile.losses + profile.draws;
    }

    private string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int minutes = totalSeconds / 60;
        int remainingSeconds = totalSeconds % 60;

        return minutes.ToString("00") + ":" + remainingSeconds.ToString("00");
    }

    private void EnsureReferences()
    {
        if (refsCached)
            return;

        if (transform.childCount > 0)
            iconImage = transform.GetChild(0).GetComponent<Image>();

        if (transform.childCount > 1)
            nameText = transform.GetChild(1).GetComponent<TMP_Text>();

        if (transform.childCount > 2)
            winsText = transform.GetChild(2).GetComponent<TMP_Text>();

        if (transform.childCount > 3)
            totalMatchesText = transform.GetChild(3).GetComponent<TMP_Text>();

        if (transform.childCount > 4)
            drawsText = transform.GetChild(4).GetComponent<TMP_Text>();

        if (transform.childCount > 5)
            avgMatchTimeText = transform.GetChild(5).GetComponent<TMP_Text>();

        refsCached = true;
    }
}