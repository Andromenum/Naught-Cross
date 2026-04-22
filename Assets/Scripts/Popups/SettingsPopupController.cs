using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class SettingsPopupView
{
    public GameObject root;
    public Toggle musicToggle;
    public Slider musicSlider;
    public Toggle sfxToggle;
    public Slider sfxSlider;
}

public class SettingsPopupController : PopupControllerBase
{
    [Header("Landscape View")]
    [SerializeField] private SettingsPopupView landscapeView;

    [Header("Portrait View")]
    [SerializeField] private SettingsPopupView portraitView;

    protected override void Awake()
    {
        landscapePopupRoot = landscapeView.root;
        portraitPopupRoot = portraitView.root;
        base.Awake();
    }

    protected override void BeforeOpenOrRefresh()
    {
        RefreshAllViews();
    }

    protected override void AfterLayoutSwap()
    {
        RefreshAllViews();
    }

    public void OnMusicToggleChanged(bool value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicEnabled(value);

        RefreshAllViews();
    }

    public void OnMusicSliderChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(value);

        RefreshAllViews();
    }

    public void OnSfxToggleChanged(bool value)
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.SetSfxEnabled(value);

        RefreshAllViews();
    }

    public void OnSfxSliderChanged(float value)
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.SetSfxVolume(value);

        RefreshAllViews();
    }

    private void RefreshAllViews()
    {
        RefreshView(landscapeView);
        RefreshView(portraitView);
    }

    private void RefreshView(SettingsPopupView view)
    {
        if (view == null)
            return;

        if (AudioManager.Instance != null)
        {
            if (view.musicToggle != null)
                view.musicToggle.SetIsOnWithoutNotify(AudioManager.Instance.MusicEnabled);

            if (view.musicSlider != null)
                view.musicSlider.SetValueWithoutNotify(AudioManager.Instance.MusicVolume);
        }

        if (SFXManager.Instance != null)
        {
            if (view.sfxToggle != null)
                view.sfxToggle.SetIsOnWithoutNotify(SFXManager.Instance.SfxEnabled);

            if (view.sfxSlider != null)
                view.sfxSlider.SetValueWithoutNotify(SFXManager.Instance.SfxVolume);
        }
    }
}