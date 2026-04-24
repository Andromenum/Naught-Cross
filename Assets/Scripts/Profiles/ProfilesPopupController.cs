using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class PlayerControlsView
{
    public GameObject root;
    public Transform markChoicesContainer;
    public Button confirmButton;
    public Button deleteProfileButton;

    [Header("Delete Confirm")]
    public GameObject deleteConfirmRoot;
    public Button deleteYesButton;
    public Button deleteNoButton;
}

[System.Serializable]
public class ProfilesPopupView
{
    public GameObject root;
    public Transform slotsContainer;
    public PlayerControlsView player1Controls;
    public PlayerControlsView player2Controls;
}

public class ProfilesPopupController : PopupControllerBase
{
    [Header("Landscape View")]
    [SerializeField] private ProfilesPopupView landscapeView;

    [Header("Portrait View")]
    [SerializeField] private ProfilesPopupView portraitView;

    [Header("Slot Visuals")]
    [SerializeField] private string emptySlotLabel = "CREATE\nPROFILE";
    [SerializeField] private ProfileIconLibrary iconLibrary;

    [Header("Player 1 Profile Selection")]
    [SerializeField] private Sprite player1SelectionSprite;
    [SerializeField] private Material player1SelectionMaterial;

    [Header("Player 2 Profile Selection")]
    [SerializeField] private Sprite player2SelectionSprite;
    [SerializeField] private Material player2SelectionMaterial;

    [Header("Player 1 X Choices")]
    [SerializeField] private Sprite[] player1XChoiceSprites;
    [SerializeField] private Sprite xChoiceSelectionSprite;
    [SerializeField] private Material xChoiceSelectionMaterial;

    [Header("Player 2 O Choices")]
    [SerializeField] private Sprite[] player2OChoiceSprites;
    [SerializeField] private Sprite oChoiceSelectionSprite;
    [SerializeField] private Material oChoiceSelectionMaterial;

    [Header("Other Popups")]
    [SerializeField] private CreateProfilePopupController createProfilePopupController;
    [SerializeField] private GameSelectPopupController gameSelectPopupController;

    private readonly List<MarkChoiceButtonUI> landscapePlayer1MarkButtons = new List<MarkChoiceButtonUI>();
    private readonly List<MarkChoiceButtonUI> portraitPlayer1MarkButtons = new List<MarkChoiceButtonUI>();
    private readonly List<MarkChoiceButtonUI> landscapePlayer2MarkButtons = new List<MarkChoiceButtonUI>();
    private readonly List<MarkChoiceButtonUI> portraitPlayer2MarkButtons = new List<MarkChoiceButtonUI>();

    private int selectedPlayer1SlotIndex = -1;
    private int selectedPlayer1XChoiceIndex = -1;
    private bool player1Ready;
    private bool player1DeleteConfirmOpen;

    private int selectedPlayer2SlotIndex = -1;
    private int selectedPlayer2OChoiceIndex = -1;
    private bool player2Ready;
    private bool player2DeleteConfirmOpen;

    protected override void Awake()
    {
        if (landscapeView != null)
            landscapePopupRoot = landscapeView.root;

        if (portraitView != null)
            portraitPopupRoot = portraitView.root;

        base.Awake();
    }

    public override void OpenPopup()
    {
        ResetSelectionState();
        base.OpenPopup();
    }

    public override void ClosePopup()
    {
        ResetSelectionState();
        CloseNextPopup();
        base.ClosePopup();
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (PlayerProfilesManager.Instance != null)
            PlayerProfilesManager.Instance.ProfilesChanged += HandleProfilesChanged;
    }

    protected override void OnDisable()
    {
        if (PlayerProfilesManager.Instance != null)
            PlayerProfilesManager.Instance.ProfilesChanged -= HandleProfilesChanged;

        base.OnDisable();
    }

    protected override void BeforeOpenOrRefresh()
    {
        RefreshAllViews();
    }

    protected override void AfterLayoutSwap()
    {
        RefreshAllViews();
    }

    public void ForceRefresh()
    {
        ValidateSelectionsAfterProfileChange();
        RefreshAllViews();
    }

    private void HandleProfilesChanged()
    {
        ValidateSelectionsAfterProfileChange();
        RefreshAllViews();
    }

    private void RefreshAllViews()
    {
        RefreshView(landscapeView);
        RefreshView(portraitView);

        RefreshPlayer1ControlsView(landscapeView, landscapePlayer1MarkButtons);
        RefreshPlayer1ControlsView(portraitView, portraitPlayer1MarkButtons);

        RefreshPlayer2ControlsView(landscapeView, landscapePlayer2MarkButtons);
        RefreshPlayer2ControlsView(portraitView, portraitPlayer2MarkButtons);

        RefreshNextPopupState();
    }

    private void RefreshView(ProfilesPopupView view)
    {
        if (view == null || view.slotsContainer == null)
            return;

        int childCount = view.slotsContainer.childCount;

        for (int i = 0; i < childCount; i++)
        {
            Transform child = view.slotsContainer.GetChild(i);
            ProfileSlotUI slotUI = child.GetComponent<ProfileSlotUI>();

            if (slotUI == null)
                continue;

            slotUI.Setup(i, HandleSlotClicked);

            bool hasProfile = PlayerProfilesManager.Instance != null && PlayerProfilesManager.Instance.HasProfileAt(i);

            if (!hasProfile)
            {
                slotUI.ShowEmpty(emptySlotLabel);
                slotUI.SetInteractable(true);
                continue;
            }

            PlayerProfileData profile = PlayerProfilesManager.Instance.GetProfileAt(i);
            Sprite icon = iconLibrary != null ? iconLibrary.GetIcon(profile.iconIndex) : null;

            slotUI.ShowFilled(icon, profile.playerName);
            slotUI.SetPlayer1Selected(i == selectedPlayer1SlotIndex, player1SelectionSprite, player1SelectionMaterial);
            slotUI.SetPlayer2Selected(i == selectedPlayer2SlotIndex, player2SelectionSprite, player2SelectionMaterial);

            bool lockedForPlayer1 = player1Ready && i == selectedPlayer1SlotIndex;
            bool lockedForPlayer2 = player2Ready && i == selectedPlayer2SlotIndex;

            slotUI.SetInteractable(!lockedForPlayer1 && !lockedForPlayer2);
        }
    }

    private void RefreshPlayer1ControlsView(ProfilesPopupView view, List<MarkChoiceButtonUI> targetButtons)
    {
        if (view == null || view.player1Controls == null)
            return;

        PlayerControlsView controls = view.player1Controls;

        bool showControls = selectedPlayer1SlotIndex >= 0 || player1Ready;

        if (controls.root != null)
            controls.root.SetActive(showControls);

        if (!showControls)
            return;

        WireAndRefreshMarkButtons(
            controls,
            targetButtons,
            player1XChoiceSprites,
            HandlePlayer1XChoiceClicked,
            xChoiceSelectionSprite,
            xChoiceSelectionMaterial);

        bool markChoicesInteractable = !player1Ready;
        bool deleteInteractable = !player1Ready && selectedPlayer1SlotIndex >= 0;
        bool confirmInteractable = player1Ready || (selectedPlayer1SlotIndex >= 0 && selectedPlayer1XChoiceIndex >= 0);

        if (controls.deleteProfileButton != null)
        {
            controls.deleteProfileButton.onClick.RemoveAllListeners();
            controls.deleteProfileButton.onClick.AddListener(HandlePlayer1DeleteClicked);
            controls.deleteProfileButton.interactable = deleteInteractable && !player1DeleteConfirmOpen;
        }

        if (controls.deleteConfirmRoot != null)
            controls.deleteConfirmRoot.SetActive(deleteInteractable && player1DeleteConfirmOpen);

        if (controls.deleteYesButton != null)
        {
            controls.deleteYesButton.onClick.RemoveAllListeners();
            controls.deleteYesButton.onClick.AddListener(HandlePlayer1DeleteConfirmed);
            controls.deleteYesButton.interactable = deleteInteractable && player1DeleteConfirmOpen;
        }

        if (controls.deleteNoButton != null)
        {
            controls.deleteNoButton.onClick.RemoveAllListeners();
            controls.deleteNoButton.onClick.AddListener(HandlePlayer1DeleteCancelled);
            controls.deleteNoButton.interactable = deleteInteractable && player1DeleteConfirmOpen;
        }

        if (controls.confirmButton != null)
        {
            controls.confirmButton.onClick.RemoveAllListeners();
            controls.confirmButton.onClick.AddListener(HandlePlayer1ConfirmClicked);
            controls.confirmButton.interactable = confirmInteractable;

            TMP_Text confirmLabel = controls.confirmButton.GetComponentInChildren<TMP_Text>(true);
            if (confirmLabel != null)
                confirmLabel.text = player1Ready ? "UNREADY" : "CONFIRM";
        }

        for (int i = 0; i < targetButtons.Count; i++)
        {
            if (targetButtons[i] == null)
                continue;

            targetButtons[i].SetInteractable(markChoicesInteractable);
            targetButtons[i].SetSelected(i == selectedPlayer1XChoiceIndex);
        }
    }

    private void RefreshPlayer2ControlsView(ProfilesPopupView view, List<MarkChoiceButtonUI> targetButtons)
    {
        if (view == null || view.player2Controls == null)
            return;

        PlayerControlsView controls = view.player2Controls;

        bool showControls = player1Ready && (selectedPlayer2SlotIndex >= 0 || player2Ready);

        if (controls.root != null)
            controls.root.SetActive(showControls);

        if (!showControls)
            return;

        WireAndRefreshMarkButtons(
            controls,
            targetButtons,
            player2OChoiceSprites,
            HandlePlayer2OChoiceClicked,
            oChoiceSelectionSprite,
            oChoiceSelectionMaterial);

        bool markChoicesInteractable = !player2Ready;
        bool deleteInteractable = !player2Ready && selectedPlayer2SlotIndex >= 0;
        bool confirmInteractable = player2Ready || (selectedPlayer2SlotIndex >= 0 && selectedPlayer2OChoiceIndex >= 0);

        if (controls.deleteProfileButton != null)
        {
            controls.deleteProfileButton.onClick.RemoveAllListeners();
            controls.deleteProfileButton.onClick.AddListener(HandlePlayer2DeleteClicked);
            controls.deleteProfileButton.interactable = deleteInteractable && !player2DeleteConfirmOpen;
        }

        if (controls.deleteConfirmRoot != null)
            controls.deleteConfirmRoot.SetActive(deleteInteractable && player2DeleteConfirmOpen);

        if (controls.deleteYesButton != null)
        {
            controls.deleteYesButton.onClick.RemoveAllListeners();
            controls.deleteYesButton.onClick.AddListener(HandlePlayer2DeleteConfirmed);
            controls.deleteYesButton.interactable = deleteInteractable && player2DeleteConfirmOpen;
        }

        if (controls.deleteNoButton != null)
        {
            controls.deleteNoButton.onClick.RemoveAllListeners();
            controls.deleteNoButton.onClick.AddListener(HandlePlayer2DeleteCancelled);
            controls.deleteNoButton.interactable = deleteInteractable && player2DeleteConfirmOpen;
        }

        if (controls.confirmButton != null)
        {
            controls.confirmButton.onClick.RemoveAllListeners();
            controls.confirmButton.onClick.AddListener(HandlePlayer2ConfirmClicked);
            controls.confirmButton.interactable = confirmInteractable;

            TMP_Text confirmLabel = controls.confirmButton.GetComponentInChildren<TMP_Text>(true);
            if (confirmLabel != null)
                confirmLabel.text = player2Ready ? "UNREADY" : "CONFIRM";
        }

        for (int i = 0; i < targetButtons.Count; i++)
        {
            if (targetButtons[i] == null)
                continue;

            targetButtons[i].SetInteractable(markChoicesInteractable);
            targetButtons[i].SetSelected(i == selectedPlayer2OChoiceIndex);
        }
    }

    private void WireAndRefreshMarkButtons(
        PlayerControlsView controls,
        List<MarkChoiceButtonUI> targetButtons,
        Sprite[] choiceSprites,
        System.Action<int> clickHandler,
        Sprite selectionSprite,
        Material selectionMaterial)
    {
        targetButtons.Clear();

        if (controls.markChoicesContainer == null)
            return;

        MarkChoiceButtonUI[] buttons = controls.markChoicesContainer.GetComponentsInChildren<MarkChoiceButtonUI>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null)
                continue;

            Sprite choiceSprite = i < choiceSprites.Length ? choiceSprites[i] : null;

            buttons[i].Setup(
                i,
                choiceSprite,
                clickHandler,
                selectionSprite,
                selectionMaterial);

            targetButtons.Add(buttons[i]);
        }
    }

    private void HandleSlotClicked(int slotIndex)
    {
        if (PlayerProfilesManager.Instance == null)
            return;

        bool hasProfile = PlayerProfilesManager.Instance.HasProfileAt(slotIndex);

        if (!hasProfile)
        {
            if (createProfilePopupController != null)
                createProfilePopupController.OpenPopup();

            return;
        }

        if (!player1Ready)
        {
            selectedPlayer1SlotIndex = slotIndex;
            player1DeleteConfirmOpen = false;

            if (selectedPlayer2SlotIndex == slotIndex)
                selectedPlayer2SlotIndex = -1;

            RefreshAllViews();
            return;
        }

        if (slotIndex == selectedPlayer1SlotIndex)
            return;

        if (!player2Ready)
        {
            selectedPlayer2SlotIndex = slotIndex;
            player2DeleteConfirmOpen = false;
            RefreshAllViews();
            return;
        }
    }

    private void HandlePlayer1XChoiceClicked(int choiceIndex)
    {
        if (player1Ready)
            return;

        selectedPlayer1XChoiceIndex = choiceIndex;
        player1DeleteConfirmOpen = false;
        RefreshAllViews();
    }

    private void HandlePlayer2OChoiceClicked(int choiceIndex)
    {
        if (player2Ready)
            return;

        selectedPlayer2OChoiceIndex = choiceIndex;
        player2DeleteConfirmOpen = false;
        RefreshAllViews();
    }

    private void HandlePlayer1ConfirmClicked()
    {
        if (!player1Ready)
        {
            if (selectedPlayer1SlotIndex < 0 || selectedPlayer1XChoiceIndex < 0)
                return;

            player1Ready = true;
            player1DeleteConfirmOpen = false;

            selectedPlayer2SlotIndex = -1;
            selectedPlayer2OChoiceIndex = -1;
            player2Ready = false;
            player2DeleteConfirmOpen = false;
        }
        else
        {
            player1Ready = false;
            player1DeleteConfirmOpen = false;

            selectedPlayer2SlotIndex = -1;
            selectedPlayer2OChoiceIndex = -1;
            player2Ready = false;
            player2DeleteConfirmOpen = false;

            CloseNextPopup();
        }

        RefreshAllViews();
    }

    private void HandlePlayer2ConfirmClicked()
    {
        if (!player1Ready)
            return;

        if (!player2Ready)
        {
            if (selectedPlayer2SlotIndex < 0 || selectedPlayer2OChoiceIndex < 0)
                return;

            player2Ready = true;
            player2DeleteConfirmOpen = false;
        }
        else
        {
            player2Ready = false;
            player2DeleteConfirmOpen = false;
            CloseNextPopup();
        }

        RefreshAllViews();
    }

    private void HandlePlayer1DeleteClicked()
    {
        if (player1Ready || selectedPlayer1SlotIndex < 0)
            return;

        player1DeleteConfirmOpen = true;
        RefreshAllViews();
    }

    private void HandlePlayer1DeleteCancelled()
    {
        player1DeleteConfirmOpen = false;
        RefreshAllViews();
    }

    private void HandlePlayer1DeleteConfirmed()
    {
        if (player1Ready || PlayerProfilesManager.Instance == null || selectedPlayer1SlotIndex < 0)
            return;

        int deletedIndex = selectedPlayer1SlotIndex;

        selectedPlayer1SlotIndex = -1;
        selectedPlayer1XChoiceIndex = -1;
        player1DeleteConfirmOpen = false;

        selectedPlayer2SlotIndex = AdjustSelectedIndexAfterDelete(selectedPlayer2SlotIndex, deletedIndex);

        PlayerProfilesManager.Instance.DeleteProfileAt(deletedIndex);
        RefreshAllViews();
    }

    private void HandlePlayer2DeleteClicked()
    {
        if (player2Ready || selectedPlayer2SlotIndex < 0)
            return;

        player2DeleteConfirmOpen = true;
        RefreshAllViews();
    }

    private void HandlePlayer2DeleteCancelled()
    {
        player2DeleteConfirmOpen = false;
        RefreshAllViews();
    }

    private void HandlePlayer2DeleteConfirmed()
    {
        if (player2Ready || PlayerProfilesManager.Instance == null || selectedPlayer2SlotIndex < 0)
            return;

        int deletedIndex = selectedPlayer2SlotIndex;

        selectedPlayer2SlotIndex = -1;
        selectedPlayer2OChoiceIndex = -1;
        player2DeleteConfirmOpen = false;

        PlayerProfilesManager.Instance.DeleteProfileAt(deletedIndex);
        RefreshAllViews();
    }

    private void ValidateSelectionsAfterProfileChange()
    {
        selectedPlayer1SlotIndex = ValidateSelectedSlotIndex(selectedPlayer1SlotIndex);
        selectedPlayer2SlotIndex = ValidateSelectedSlotIndex(selectedPlayer2SlotIndex);

        if (selectedPlayer1SlotIndex < 0)
        {
            player1Ready = false;
            selectedPlayer1XChoiceIndex = -1;
            player1DeleteConfirmOpen = false;

            selectedPlayer2SlotIndex = -1;
            selectedPlayer2OChoiceIndex = -1;
            player2Ready = false;
            player2DeleteConfirmOpen = false;
        }

        if (selectedPlayer2SlotIndex < 0)
        {
            selectedPlayer2OChoiceIndex = player2Ready ? selectedPlayer2OChoiceIndex : -1;
            player2DeleteConfirmOpen = false;
        }

        if (selectedPlayer2SlotIndex == selectedPlayer1SlotIndex)
            selectedPlayer2SlotIndex = -1;
    }

    private int ValidateSelectedSlotIndex(int slotIndex)
    {
        if (slotIndex < 0)
            return -1;

        if (PlayerProfilesManager.Instance == null)
            return -1;

        return PlayerProfilesManager.Instance.HasProfileAt(slotIndex) ? slotIndex : -1;
    }

    private int AdjustSelectedIndexAfterDelete(int currentIndex, int deletedIndex)
    {
        if (currentIndex < 0)
            return -1;

        if (currentIndex == deletedIndex)
            return -1;

        if (currentIndex > deletedIndex)
            return currentIndex - 1;

        return currentIndex;
    }

    private void RefreshNextPopupState()
    {
        if (player1Ready && player2Ready)
        {
            if (gameSelectPopupController != null)
                gameSelectPopupController.OpenPopup();
        }
        else
        {
            CloseNextPopup();
        }
    }

    private void CloseNextPopup()
    {
        if (gameSelectPopupController != null)
            gameSelectPopupController.ClosePopup();
    }

    private void ResetSelectionState()
    {
        selectedPlayer1SlotIndex = -1;
        selectedPlayer1XChoiceIndex = -1;
        player1Ready = false;
        player1DeleteConfirmOpen = false;

        selectedPlayer2SlotIndex = -1;
        selectedPlayer2OChoiceIndex = -1;
        player2Ready = false;
        player2DeleteConfirmOpen = false;

        RefreshAllViews();
    }
}