using UnityEngine;

public class ExitPopupController : PopupControllerBase
{
    private bool exitRequested;

    protected override void BeforeOpenOrRefresh()
    {
        exitRequested = false;
    }

    public void ConfirmExit()
    {
        if (exitRequested)
            return;

        exitRequested = true;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}