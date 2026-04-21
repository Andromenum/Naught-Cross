using UnityEngine;
using UnityEngine.UI;

public class SettingsPopupController : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private GameObject popupRoot;

    [Header("Music UI")]
    [SerializeField] private Toggle musicToggle;
    [SerializeField] private Slider musicSlider;

    [Header("SFX UI")]
    [SerializeField] private Toggle sfxToggle;
    [SerializeField] private Slider sfxSlider;

    private void Start()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);

        RefreshUI();
    }

    public void OpenPopup()
    {
        RefreshUI();

        if (popupRoot != null)
            popupRoot.SetActive(true);
    }

    public void ClosePopup()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    public void OnMusicToggleChanged(bool value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicEnabled(value);
    }

    public void OnMusicSliderChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(value);
    }

    public void OnSfxToggleChanged(bool value)
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.SetSfxEnabled(value);
    }

    public void OnSfxSliderChanged(float value)
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.SetSfxVolume(value);
    }

    private void RefreshUI()
    {
        if (AudioManager.Instance != null)
        {
            if (musicToggle != null)
                musicToggle.SetIsOnWithoutNotify(AudioManager.Instance.MusicEnabled);

            if (musicSlider != null)
                musicSlider.SetValueWithoutNotify(AudioManager.Instance.MusicVolume);
        }

        if (SFXManager.Instance != null)
        {
            if (sfxToggle != null)
                sfxToggle.SetIsOnWithoutNotify(SFXManager.Instance.SfxEnabled);

            if (sfxSlider != null)
                sfxSlider.SetValueWithoutNotify(SFXManager.Instance.SfxVolume);
        }
    }
}