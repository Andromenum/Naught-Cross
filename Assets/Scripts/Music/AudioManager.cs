using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private const string MusicEnabledKey = "MusicEnabled";
    private const string MusicVolumeKey = "MusicVolume";

    [SerializeField] private float fadeDuration = 1.5f;

    private AudioSource audioSource;
    private Coroutine fadeRoutine;

    public bool MusicEnabled { get; private set; }
    public float MusicVolume { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        audioSource = GetComponent<AudioSource>();
        MusicEnabled = PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;
        MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);

        audioSource.volume = 0f;
    }

    private void Start()
    {
        if (audioSource.clip == null)
            return;

        if (!MusicEnabled)
            return;

        audioSource.Play();
        StartFadeTo(MusicVolume);
    }

    public void SetMusicEnabled(bool enabled)
    {
        MusicEnabled = enabled;

        PlayerPrefs.SetInt(MusicEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();

        if (!enabled)
        {
            StopCurrentFade();
            audioSource.Stop();
            audioSource.volume = 0f;
            return;
        }

        if (audioSource.clip == null)
            return;

        if (!audioSource.isPlaying)
            audioSource.Play();

        StartFadeTo(MusicVolume);
    }

    public void SetMusicVolume(float volume)
    {
        MusicVolume = Mathf.Clamp01(volume);

        PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
        PlayerPrefs.Save();

        if (!MusicEnabled)
            return;

        if (audioSource.clip == null)
            return;

        if (!audioSource.isPlaying)
            audioSource.Play();

        StopCurrentFade();
        audioSource.volume = MusicVolume;
    }

    private void StartFadeTo(float targetVolume)
    {
        StopCurrentFade();
        fadeRoutine = StartCoroutine(FadeToRoutine(Mathf.Clamp01(targetVolume)));
    }

    private IEnumerator FadeToRoutine(float targetVolume)
    {
        if (fadeDuration <= 0f)
        {
            audioSource.volume = targetVolume;
            fadeRoutine = null;
            yield break;
        }

        float startVolume = audioSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }

        audioSource.volume = targetVolume;
        fadeRoutine = null;
    }

    private void StopCurrentFade()
    {
        if (fadeRoutine == null)
            return;

        StopCoroutine(fadeRoutine);
        fadeRoutine = null;
    }
}