using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NamedSFXClip
{
    public string id;
    public AudioClip clip;

    [Range(0f, 1f)]
    public float volume = 1f;
}

[RequireComponent(typeof(AudioSource))]
public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    private const string SfxEnabledKey = "SfxEnabled";
    private const string SfxVolumeKey = "SfxVolume";

    [Header("Named Clips")]
    [SerializeField] private NamedSFXClip[] namedClips;

    private AudioSource oneShotSource;
    private AudioSource loopSource;

    private readonly Dictionary<string, NamedSFXClip> clipLookup = new Dictionary<string, NamedSFXClip>();

    private float activeLoopVolumeScale = 1f;

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
        DontDestroyOnLoad(gameObject);

        AudioSource[] sources = GetComponents<AudioSource>();

        oneShotSource = sources.Length > 0 ? sources[0] : gameObject.AddComponent<AudioSource>();
        loopSource = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();

        ConfigureSource(oneShotSource, false);
        ConfigureSource(loopSource, true);

        SfxEnabled = PlayerPrefs.GetInt(SfxEnabledKey, 1) == 1;
        SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);

        BuildLookup();
        ApplySfxSettings();
    }

    private void OnValidate()
    {
        if (namedClips == null)
            return;

        for (int i = 0; i < namedClips.Length; i++)
        {
            if (namedClips[i] == null)
                continue;

            namedClips[i].volume = Mathf.Clamp01(namedClips[i].volume);
        }
    }

    private void BuildLookup()
    {
        clipLookup.Clear();

        if (namedClips == null)
            return;

        for (int i = 0; i < namedClips.Length; i++)
        {
            NamedSFXClip entry = namedClips[i];

            if (entry == null)
                continue;

            if (string.IsNullOrWhiteSpace(entry.id))
                continue;

            if (entry.clip == null)
                continue;

            if (clipLookup.ContainsKey(entry.id))
            {
                Debug.LogWarning("SFXManager: Duplicate SFX id found: " + entry.id);
                continue;
            }

            clipLookup.Add(entry.id, entry);
        }
    }

    public void SetSfxEnabled(bool enabled)
    {
        SfxEnabled = enabled;

        PlayerPrefs.SetInt(SfxEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();

        ApplySfxSettings();
    }

    public void SetSfxVolume(float volume)
    {
        SfxVolume = Mathf.Clamp01(volume);

        PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
        PlayerPrefs.Save();

        ApplySfxSettings();
    }

    public void PlayGlobalClick()
    {
        PlayById("global_click");
    }

    public void PlayButtonClick()
    {
        PlayById("button_click");
    }

    public void PlayPopupOpen()
    {
        PlayById("popup_open");
    }

    public void PlayById(string id, float volumeScale = 1f)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        if (!clipLookup.TryGetValue(id, out NamedSFXClip entry))
        {
            Debug.LogWarning("SFXManager: No SFX clip found with id: " + id);
            return;
        }

        PlayClip(entry.clip, entry.volume * volumeScale);
    }

    public void PlayLoopById(string id, float volumeScale = 1f)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        if (!clipLookup.TryGetValue(id, out NamedSFXClip entry))
        {
            Debug.LogWarning("SFXManager: No loop SFX clip found with id: " + id);
            return;
        }

        PlayLoop(entry.clip, entry.volume * volumeScale);
    }

    public void PlayClip(AudioClip clip, float volumeScale = 1f)
    {
        if (!SfxEnabled || clip == null || oneShotSource == null)
            return;

        oneShotSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    public void PlayLoop(AudioClip clip, float volumeScale = 1f)
    {
        if (!SfxEnabled || clip == null || loopSource == null)
            return;

        activeLoopVolumeScale = Mathf.Clamp01(volumeScale);

        if (loopSource.clip == clip && loopSource.isPlaying)
        {
            loopSource.volume = SfxVolume * activeLoopVolumeScale;
            return;
        }

        loopSource.clip = clip;
        loopSource.volume = SfxVolume * activeLoopVolumeScale;
        loopSource.Play();
    }

    public void StopLoop()
    {
        if (loopSource == null)
            return;

        loopSource.Stop();
        loopSource.clip = null;
        activeLoopVolumeScale = 1f;
    }

    public float GetClipLengthById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return 0f;

        if (!clipLookup.TryGetValue(id, out NamedSFXClip entry))
            return 0f;

        if (entry == null || entry.clip == null)
            return 0f;

        return entry.clip.length;
    }

    private void ConfigureSource(AudioSource source, bool loop)
    {
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
    }

    private void ApplySfxSettings()
    {
        if (oneShotSource != null)
        {
            oneShotSource.mute = !SfxEnabled;
            oneShotSource.volume = SfxVolume;
        }

        if (loopSource != null)
        {
            loopSource.mute = !SfxEnabled;
            loopSource.volume = SfxVolume * activeLoopVolumeScale;
        }
    }
}