using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class CreateProfilePopupView
{
    public GameObject root;
    public Transform iconsContainer;
    public TMP_InputField nameInput;
    public ErrorPopupAnimator errorPopup;
    public Button confirmButton;
}

public class CreateProfilePopupController : PopupControllerBase
{
    [Header("Landscape View")]
    [SerializeField] private CreateProfilePopupView landscapeView;

    [Header("Portrait View")]
    [SerializeField] private CreateProfilePopupView portraitView;

    [Header("Icons")]
    [SerializeField] private ProfileIconLibrary iconLibrary;
    [SerializeField] private ProfileIconButtonUI iconButtonPrefab;

    [Header("Selection Frame")]
    [SerializeField] private Sprite selectionFrameSprite;
    [SerializeField] private Material selectionFrameMaterial;

    private ProfilesPopupController profilesPopupController;

    private readonly List<ProfileIconButtonUI> landscapeButtons = new List<ProfileIconButtonUI>();
    private readonly List<ProfileIconButtonUI> portraitButtons = new List<ProfileIconButtonUI>();

    private int selectedIconIndex = -1;
    private string draftName = string.Empty;
    private string currentErrorMessage = string.Empty;

    protected override void Awake()
    {
        profilesPopupController = GetComponent<ProfilesPopupController>();

        if (landscapeView != null)
            landscapePopupRoot = landscapeView.root;

        if (portraitView != null)
            portraitPopupRoot = portraitView.root;

        base.Awake();
    }

    public override void OpenPopup()
    {
        ResetDraft();
        RebuildAllIconLists();
        RefreshAllViews();
        base.OpenPopup();
    }

    protected override void BeforeOpenOrRefresh()
    {
        if (landscapeButtons.Count == 0 && portraitButtons.Count == 0)
            RebuildAllIconLists();

        RefreshAllViews();
    }

    protected override void AfterLayoutSwap()
    {
        RefreshAllViews();
    }

    public override void ClosePopup()
    {
        ClearError();
        base.ClosePopup();
    }

    public void OnNameChanged(string newValue)
    {
        draftName = newValue != null ? newValue : string.Empty;
        ClearError();
        RefreshAllViews();
    }

    public void ConfirmCreateProfile()
    {
        if (PlayerProfilesManager.Instance == null)
            return;

        bool created = PlayerProfilesManager.Instance.TryCreateProfile(
            draftName,
            selectedIconIndex,
            out string errorMessage);

        if (!created)
        {
            if (!string.Equals(errorMessage, PlayerProfilesManager.NoFreeSlotsError, StringComparison.Ordinal))
                SetError(errorMessage);

            return;
        }

        profilesPopupController?.ForceRefresh();
        ClosePopup();
    }

    private void RebuildAllIconLists()
    {
        RebuildIconListForView(landscapeView, landscapeButtons);
        RebuildIconListForView(portraitView, portraitButtons);
    }

    private void RebuildIconListForView(CreateProfilePopupView view, List<ProfileIconButtonUI> targetList)
    {
        targetList.Clear();

        if (view == null || view.iconsContainer == null || iconLibrary == null || iconButtonPrefab == null)
            return;

        ClearChildren(view.iconsContainer);

        for (int i = 0; i < iconLibrary.Count; i++)
        {
            ProfileIconButtonUI iconButton = Instantiate(iconButtonPrefab, view.iconsContainer);
            iconButton.Setup(
                i,
                iconLibrary.GetIcon(i),
                HandleIconClicked,
                selectionFrameSprite,
                selectionFrameMaterial);

            targetList.Add(iconButton);
        }
    }

    private void HandleIconClicked(int iconIndex)
    {
        selectedIconIndex = iconIndex;
        ClearError();
        RefreshAllViews();
    }

    private void RefreshAllViews()
    {
        RefreshView(landscapeView);
        RefreshView(portraitView);

        RefreshButtonListSelection(landscapeButtons);
        RefreshButtonListSelection(portraitButtons);
    }

    private void RefreshView(CreateProfilePopupView view)
    {
        if (view == null)
            return;

        if (view.nameInput != null)
            view.nameInput.SetTextWithoutNotify(draftName);

        if (view.confirmButton != null)
            view.confirmButton.interactable = true;
    }

    private void RefreshButtonListSelection(List<ProfileIconButtonUI> buttons)
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] == null)
                continue;

            buttons[i].SetSelected(i == selectedIconIndex);
        }
    }

    private void SetError(string message)
    {
        currentErrorMessage = message ?? string.Empty;

        CreateProfilePopupView activeView = GetCurrentlyOpenView();
        if (activeView != null && activeView.errorPopup != null)
            activeView.errorPopup.ShowError(currentErrorMessage);
    }

    private void ClearError()
    {
        currentErrorMessage = string.Empty;

        if (landscapeView != null && landscapeView.errorPopup != null)
            landscapeView.errorPopup.ForceHidden();

        if (portraitView != null && portraitView.errorPopup != null)
            portraitView.errorPopup.ForceHidden();
    }

    private CreateProfilePopupView GetCurrentlyOpenView()
    {
        if (portraitView != null && portraitView.root != null && portraitView.root.activeInHierarchy)
            return portraitView;

        if (landscapeView != null && landscapeView.root != null && landscapeView.root.activeInHierarchy)
            return landscapeView;

        bool isPortrait = UILayoutController.Instance != null && UILayoutController.Instance.IsPortrait;
        return isPortrait ? portraitView : landscapeView;
    }

    private void ResetDraft()
    {
        draftName = string.Empty;
        selectedIconIndex = -1;
        currentErrorMessage = string.Empty;
        ClearError();
    }

    private void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }
}