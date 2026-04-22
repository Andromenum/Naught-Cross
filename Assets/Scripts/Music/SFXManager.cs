using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    private const string SfxEnabledKey = "SfxEnabled";
    private const string SfxVolumeKey = "SfxVolume";

    [Header("Default Clips")]
    [SerializeField] private AudioClip globalClickClip;
    [SerializeField] private AudioClip buttonClickClip;

    private AudioSource audioSource;

    public bool SfxEnabled { get; private set; }
    public float SfxVolume { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;

        SfxEnabled = PlayerPrefs.GetInt(SfxEnabledKey, 1) == 1;
        SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);

        audioSource.volume = SfxVolume;
        audioSource.mute = !SfxEnabled;
    }

    public void SetSfxEnabled(bool enabled)
    {
        SfxEnabled = enabled;

        PlayerPrefs.SetInt(SfxEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();

        audioSource.mute = !enabled;
    }

    public void SetSfxVolume(float volume)
    {
        SfxVolume = Mathf.Clamp01(volume);
        audioSource.volume = SfxVolume;

        PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
        PlayerPrefs.Save();
    }

    public void PlayGlobalClick()
    {
        PlayClip(globalClickClip);
    }

    public void PlayButtonClick()
    {
        PlayClip(buttonClickClip);
    }

    public void PlayClip(AudioClip clip)
    {
        if (!SfxEnabled || clip == null)
            return;

        audioSource.PlayOneShot(clip, 1f);
    }
}