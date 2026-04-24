using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ProfileIconButtonUI : MonoBehaviour
{
    private Button button;

    private Image iconImage;
    private Image selectionFrameImage;

    private int iconIndex;
    private Action<int> clickCallback;
    private bool refsCached;

    private void Awake()
    {
        EnsureReferences();
    }

    public void Setup(
        int index,
        Sprite iconSprite,
        Action<int> onClicked,
        Sprite selectionFrameSprite,
        Material selectionFrameMaterial)
    {
        EnsureReferences();

        iconIndex = index;
        clickCallback = onClicked;

        if (iconImage != null)
            iconImage.sprite = iconSprite;

        if (selectionFrameImage != null)
        {
            selectionFrameImage.sprite = selectionFrameSprite;
            selectionFrameImage.material = selectionFrameMaterial;
            selectionFrameImage.enabled = false;
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClicked);
        }
    }

    public void SetSelected(bool selected)
    {
        EnsureReferences();

        if (selectionFrameImage != null)
            selectionFrameImage.enabled = selected;
    }

    private void EnsureReferences()
    {
        if (refsCached)
            return;

        button = GetComponent<Button>();

        Transform iconTransform = transform.Find("IconImage");
        if (iconTransform != null)
            iconImage = iconTransform.GetComponent<Image>();

        Transform overlayTransform = transform.Find("AnimatedOverlayImage");
        if (overlayTransform != null)
            selectionFrameImage = overlayTransform.GetComponent<Image>();

        refsCached = true;
    }

    private void HandleClicked()
    {
        clickCallback?.Invoke(iconIndex);
    }
}