using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public class MarkChoiceButtonUI : MonoBehaviour
{
    private Button button;
    private Image previewImage;
    private Image selectionImage;

    private int choiceIndex;
    private Action<int> clickCallback;
    private bool refsCached;

    private void Awake()
    {
        EnsureReferences();
    }

    public void Setup(
        int index,
        Sprite previewSprite,
        Action<int> onClicked,
        Sprite selectionSprite,
        Material selectionMaterial)
    {
        EnsureReferences();

        choiceIndex = index;
        clickCallback = onClicked;

        if (previewImage != null)
            previewImage.sprite = previewSprite;

        if (selectionImage != null)
        {
            if (selectionSprite != null)
                selectionImage.sprite = selectionSprite;

            if (selectionMaterial != null)
                selectionImage.material = selectionMaterial;

            selectionImage.enabled = false;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(HandleClicked);
    }

    public void SetSelected(bool selected)
    {
        EnsureReferences();

        if (selectionImage != null)
            selectionImage.enabled = selected;
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
        previewImage = GetComponent<Image>();

        Transform selectionTransform = transform.Find("SelectionImage");
        if (selectionTransform != null)
            selectionImage = selectionTransform.GetComponent<Image>();

        refsCached = true;
    }

    private void HandleClicked()
    {
        clickCallback?.Invoke(choiceIndex);
    }
}