using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public class ProfileSlotUI : MonoBehaviour
{
    [Header("Empty Slot Look")]
    [SerializeField] private bool keepCurrentSpriteWhenEmpty = true;
    [SerializeField] private Sprite emptySpriteOverride;
    [SerializeField] private Color emptyColor = Color.white;
    [SerializeField] private Color filledColor = Color.white;

    private Button button;
    private Image backgroundImage;
    private Image iconImage;
    private TMP_Text labelText;
    private Image player1SelectionImage;
    private Image player2SelectionImage;

    private int slotIndex;
    private Action<int> clickCallback;
    private Sprite initialIconSprite;
    private bool refsCached;

    private void Awake()
    {
        EnsureReferences();
    }

    public void Setup(int index, Action<int> onClicked)
    {
        EnsureReferences();

        slotIndex = index;
        clickCallback = onClicked;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(HandleClicked);
    }

    public void ShowEmpty(string emptyLabel)
    {
        EnsureReferences();

        if (iconImage != null)
        {
            if (!keepCurrentSpriteWhenEmpty && emptySpriteOverride != null)
                iconImage.sprite = emptySpriteOverride;
            else
                iconImage.sprite = initialIconSprite;

            iconImage.color = emptyColor;
        }

        if (labelText != null)
            labelText.text = emptyLabel;

        SetPlayer1Selected(false, null, null);
        SetPlayer2Selected(false, null, null);
    }

    public void ShowFilled(Sprite profileSprite, string playerName)
    {
        EnsureReferences();

        if (iconImage != null)
        {
            iconImage.sprite = profileSprite;
            iconImage.color = filledColor;
        }

        if (labelText != null)
            labelText.text = playerName;
    }

    public void SetPlayer1Selected(bool selected, Sprite selectionSprite, Material selectionMaterial)
    {
        EnsureReferences();

        if (player1SelectionImage == null)
            return;

        if (selectionSprite != null)
            player1SelectionImage.sprite = selectionSprite;

        if (selectionMaterial != null)
            player1SelectionImage.material = selectionMaterial;

        player1SelectionImage.enabled = selected;
    }

    public void SetPlayer2Selected(bool selected, Sprite selectionSprite, Material selectionMaterial)
    {
        EnsureReferences();

        if (player2SelectionImage == null)
            return;

        if (selectionSprite != null)
            player2SelectionImage.sprite = selectionSprite;

        if (selectionMaterial != null)
            player2SelectionImage.material = selectionMaterial;

        player2SelectionImage.enabled = selected;
    }

    public void SetInteractable(bool interactable)
    {
        EnsureReferences();

        if (button != null)
            button.interactable = interactable;
    }

    private void EnsureReferences()
    {
        if (refsCached)
            return;

        button = GetComponent<Button>();
        backgroundImage = GetComponent<Image>();

        Transform iconTransform = transform.Find("IconImage");
        if (iconTransform != null)
            iconImage = iconTransform.GetComponent<Image>();

        Transform textTransform = transform.Find("Text");
        if (textTransform != null)
            labelText = textTransform.GetComponent<TMP_Text>();
        else
            labelText = GetComponentInChildren<TMP_Text>(true);

        Transform p1Transform = transform.Find("Player1SelectionImage");
        if (p1Transform != null)
            player1SelectionImage = p1Transform.GetComponent<Image>();

        Transform p2Transform = transform.Find("Player2SelectionImage");
        if (p2Transform != null)
            player2SelectionImage = p2Transform.GetComponent<Image>();

        if (iconImage != null)
            initialIconSprite = iconImage.sprite;

        refsCached = true;
    }

    private void HandleClicked()
    {
        clickCallback?.Invoke(slotIndex);
    }
}