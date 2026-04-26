using UnityEngine;

[System.Serializable]
public class StatsPopupView
{
    public GameObject root;
    public GameObject headerRoot;
    public Transform contentRoot;
    public StatsEntryUI entryPrefab;
}

public class StatsPopupController : PopupControllerBase
{
    [Header("Landscape View")]
    [SerializeField] private StatsPopupView landscapeView;

    [Header("Portrait View")]
    [SerializeField] private StatsPopupView portraitView;

    [Header("Icons")]
    [SerializeField] private ProfileIconLibrary iconLibrary;

    protected override void Awake()
    {
        if (landscapeView != null)
            landscapePopupRoot = landscapeView.root;

        if (portraitView != null)
            portraitPopupRoot = portraitView.root;

        base.Awake();
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
        RebuildAllViews();
    }

    protected override void AfterLayoutSwap()
    {
        RebuildAllViews();
    }

    private void HandleProfilesChanged()
    {
        RebuildAllViews();
    }

    private void RebuildAllViews()
    {
        bool hasAnyProfiles = HasAnyProfiles();

        RebuildView(landscapeView, hasAnyProfiles);
        RebuildView(portraitView, hasAnyProfiles);
    }

    private void RebuildView(StatsPopupView view, bool hasAnyProfiles)
    {
        if (view == null)
            return;

        if (view.headerRoot != null)
            view.headerRoot.SetActive(hasAnyProfiles);

        if (view.contentRoot == null || view.entryPrefab == null)
            return;

        ClearChildren(view.contentRoot);

        if (PlayerProfilesManager.Instance == null)
            return;

        for (int i = 0; i < PlayerProfilesManager.Instance.SlotCount; i++)
        {
            if (!PlayerProfilesManager.Instance.HasProfileAt(i))
                continue;

            PlayerProfileData profile = PlayerProfilesManager.Instance.GetProfileAt(i);
            if (profile == null)
                continue;

            StatsEntryUI entry = Instantiate(view.entryPrefab, view.contentRoot);

            Sprite icon = null;
            if (iconLibrary != null && profile.iconIndex >= 0)
                icon = iconLibrary.GetIcon(profile.iconIndex);

            entry.Setup(profile, icon);
        }
    }

    private bool HasAnyProfiles()
    {
        if (PlayerProfilesManager.Instance == null)
            return false;

        for (int i = 0; i < PlayerProfilesManager.Instance.SlotCount; i++)
        {
            if (PlayerProfilesManager.Instance.HasProfileAt(i))
                return true;
        }

        return false;
    }

    private void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }
}