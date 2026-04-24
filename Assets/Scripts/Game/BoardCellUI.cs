using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BoardCellUI : MonoBehaviour
{
    [SerializeField] private Vector2Int gridPosition;

    private Button button;
    private Image markImage;

    private Action<Vector2Int> clickCallback;
    private bool refsCached;

    public Vector2Int GridPosition => gridPosition;

    private void Awake()
    {
        EnsureReferences();
        ForceHiddenMark();
    }

    public void Setup(Action<Vector2Int> onClicked)
    {
        EnsureReferences();

        clickCallback = onClicked;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(HandleClicked);
    }

    public void ClearMark()
    {
        EnsureReferences();

        if (markImage != null)
        {
            markImage.sprite = null;
            SetImageAlpha(0f);
        }

        SetInteractable(true);
    }

    public void SetMark(Sprite sprite)
    {
        EnsureReferences();

        if (markImage != null)
        {
            markImage.sprite = sprite;
            SetImageAlpha(sprite != null ? 1f : 0f);
        }

        SetInteractable(false);
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

        if (transform.childCount > 0)
            markImage = transform.GetChild(0).GetComponent<Image>();

        refsCached = true;
    }

    private void ForceHiddenMark()
    {
        if (markImage == null)
            return;

        SetImageAlpha(0f);
    }

    private void SetImageAlpha(float alpha)
    {
        if (markImage == null)
            return;

        Color c = markImage.color;
        c.a = alpha;
        markImage.color = c;
    }

    private void HandleClicked()
    {
        clickCallback?.Invoke(gridPosition);
    }
}