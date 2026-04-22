using UnityEngine;

public abstract class PopupControllerBase : MonoBehaviour
{
    [SerializeField] protected GameObject landscapePopupRoot;
    [SerializeField] protected GameObject portraitPopupRoot;

    protected bool isOpen;
    private bool isSubscribed;

    protected virtual void Awake()
    {
        if (landscapePopupRoot != null)
            landscapePopupRoot.SetActive(false);

        if (portraitPopupRoot != null)
            portraitPopupRoot.SetActive(false);
    }

    protected virtual void Start()
    {
        TrySubscribeToLayoutController();
    }

    protected virtual void OnEnable()
    {
        TrySubscribeToLayoutController();
    }

    protected virtual void OnDisable()
    {
        if (UILayoutController.Instance != null && isSubscribed)
        {
            UILayoutController.Instance.LayoutChanged -= HandleLayoutChanged;
            isSubscribed = false;
        }
    }

    public virtual void OpenPopup()
    {
        isOpen = true;
        RefreshPopupVisibility();
    }

    public virtual void ClosePopup()
    {
        isOpen = false;

        if (landscapePopupRoot != null)
            landscapePopupRoot.SetActive(false);

        if (portraitPopupRoot != null)
            portraitPopupRoot.SetActive(false);
    }

    protected virtual void BeforeOpenOrRefresh()
    {
    }

    protected virtual void AfterLayoutSwap()
    {
    }

    private void TrySubscribeToLayoutController()
    {
        if (isSubscribed)
            return;

        if (UILayoutController.Instance == null)
            return;

        UILayoutController.Instance.LayoutChanged += HandleLayoutChanged;
        isSubscribed = true;
    }

    private void HandleLayoutChanged(bool isPortrait)
    {
        if (!isOpen)
            return;

        RefreshPopupVisibility();
    }

    private void RefreshPopupVisibility()
    {
        BeforeOpenOrRefresh();

        bool isPortrait = UILayoutController.Instance != null && UILayoutController.Instance.IsPortrait;

        if (landscapePopupRoot != null)
            landscapePopupRoot.SetActive(isOpen && !isPortrait);

        if (portraitPopupRoot != null)
            portraitPopupRoot.SetActive(isOpen && isPortrait);

        AfterLayoutSwap();
    }
}