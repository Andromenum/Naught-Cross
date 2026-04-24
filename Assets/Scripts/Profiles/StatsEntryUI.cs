using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatsEntryUI : MonoBehaviour
{
    private Image iconImage;
    private TMP_Text nameText;
    private TMP_Text winsText;
    private TMP_Text lossesText;
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

        if (lossesText != null)
            lossesText.text = profile.losses.ToString();

        if (drawsText != null)
            drawsText.text = profile.draws.ToString();

        if (avgMatchTimeText != null)
            avgMatchTimeText.text = profile.GetAverageMatchDuration().ToString("0.##");
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
            lossesText = transform.GetChild(3).GetComponent<TMP_Text>();

        if (transform.childCount > 4)
            drawsText = transform.GetChild(4).GetComponent<TMP_Text>();

        if (transform.childCount > 5)
            avgMatchTimeText = transform.GetChild(5).GetComponent<TMP_Text>();

        refsCached = true;
    }
}