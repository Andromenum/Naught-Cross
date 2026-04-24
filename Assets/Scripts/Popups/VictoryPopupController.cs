using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class VictoryPopupView
{
    public GameObject root;
    public Image winnerIconImage;
    public TMP_Text winnerNameText;
    public TMP_Text resultText;
}

public class VictoryPopupController : PopupControllerBase
{
    [Header("Landscape View")]
    [SerializeField] private VictoryPopupView landscapeView;

    [Header("Portrait View")]
    [SerializeField] private VictoryPopupView portraitView;

    [Header("Libraries")]
    [SerializeField] private ProfileIconLibrary iconLibrary;

    protected override void Awake()
    {
        if (landscapeView != null)
            landscapePopupRoot = landscapeView.root;

        if (portraitView != null)
            portraitPopupRoot = portraitView.root;

        base.Awake();
    }

    public void ShowWinner(MatchPlayerData winner)
    {
        ApplyWinnerToView(landscapeView, winner);
        ApplyWinnerToView(portraitView, winner);
        OpenPopup();
    }

    public void ShowDraw()
    {
        ApplyDrawToView(landscapeView);
        ApplyDrawToView(portraitView);
        OpenPopup();
    }

    private void ApplyWinnerToView(VictoryPopupView view, MatchPlayerData winner)
    {
        if (view == null)
            return;

        if (view.resultText != null)
            view.resultText.text = "WINS!";

        if (view.winnerNameText != null)
            view.winnerNameText.text = winner != null ? winner.playerName : string.Empty;

        if (view.winnerIconImage != null)
        {
            Sprite icon = null;

            if (winner != null && iconLibrary != null)
                icon = iconLibrary.GetIcon(winner.iconIndex);

            view.winnerIconImage.sprite = icon;
            view.winnerIconImage.enabled = icon != null;
        }
    }

    private void ApplyDrawToView(VictoryPopupView view)
    {
        if (view == null)
            return;

        if (view.resultText != null)
            view.resultText.text = "DRAW";

        if (view.winnerNameText != null)
            view.winnerNameText.text = "NO WINNER";

        if (view.winnerIconImage != null)
            view.winnerIconImage.enabled = false;
    }
}