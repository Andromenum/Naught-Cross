using UnityEngine;

public abstract class PopupControllerBase : MonoBehaviour
{
    [SerializeField] protected GameObject landscapePopupRoot;
    [SerializeField] protected GameObject portraitPopupRoot;

    protected bool isOpen;
    private bool isSubscribed;

    protected virtual void Awake()
    {
        ForceHideRoot(landscapePopupRoot);
        ForceHideRoot(portraitPopupRoot);
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
        bool wasClosed = !isOpen;
        isOpen = true;

        RefreshPopupVisibility(animate: wasClosed);

        if (wasClosed && SFXManager.Instance != null)
            SFXManager.Instance.PlayPopupOpen();
    }

    public virtual void ClosePopup()
    {
        if (!isOpen)
            return;

        isOpen = false;

        bool isPortrait = UILayoutController.Instance != null && UILayoutController.Instance.IsPortrait;

        GameObject activeRoot = isPortrait ? portraitPopupRoot : landscapePopupRoot;
        GameObject inactiveRoot = isPortrait ? landscapePopupRoot : portraitPopupRoot;

        ForceHideRoot(inactiveRoot);
        CloseRoot(activeRoot);
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

        RefreshPopupVisibility(animate: false);
    }

    private void RefreshPopupVisibility(bool animate)
    {
        BeforeOpenOrRefresh();

        bool isPortrait = UILayoutController.Instance != null && UILayoutController.Instance.IsPortrait;

        GameObject activeRoot = isPortrait ? portraitPopupRoot : landscapePopupRoot;
        GameObject inactiveRoot = isPortrait ? landscapePopupRoot : portraitPopupRoot;

        ForceHideRoot(inactiveRoot);

        if (animate)
            OpenRoot(activeRoot);
        else
            ForceShowRoot(activeRoot);

        AfterLayoutSwap();
    }

    private void OpenRoot(GameObject root)
    {
        if (root == null)
            return;

        PopupAnimator animator = root.GetComponent<PopupAnimator>();

        if (animator != null)
            animator.PlayOpen();
        else
            root.SetActive(true);
    }

    private void CloseRoot(GameObject root)
    {
        if (root == null)
            return;

        PopupAnimator animator = root.GetComponent<PopupAnimator>();

        if (animator != null)
            animator.PlayClose();
        else
            root.SetActive(false);
    }

    private void ForceShowRoot(GameObject root)
    {
        if (root == null)
            return;

        PopupAnimator animator = root.GetComponent<PopupAnimator>();

        if (animator != null)
            animator.ForceShown();
        else
            root.SetActive(true);
    }

    private void ForceHideRoot(GameObject root)
    {
        if (root == null)
            return;

        PopupAnimator animator = root.GetComponent<PopupAnimator>();

        if (animator != null)
            animator.ForceHidden();
        else
            root.SetActive(false);
    }
}