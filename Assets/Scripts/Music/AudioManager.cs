using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private const string MusicEnabledKey = "MusicEnabled";
    private const string MusicVolumeKey = "MusicVolume";

    [SerializeField] private float fadeDuration = 1.5f;

    private AudioSource audioSource;
    private Coroutine fadeRoutine;
    private bool isLoadingScene;

    private float currentMusicMultiplier = 1f;

    public bool MusicEnabled { get; private set; }
    public float MusicVolume { get; private set; }
    public float FadeDuration => fadeDuration;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f;

        MusicEnabled = PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;
        MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);

        ApplyMusicSettingsInstant();
    }

    public void PlayMusic(AudioClip clip, bool fadeIn = true)
    {
        if (clip == null)
            return;

        currentMusicMultiplier = 1f;

        if (audioSource.clip == clip && audioSource.isPlaying)
        {
            audioSource.mute = !MusicEnabled;

            if (MusicEnabled)
                FadeMusicToCurrentTarget(fadeIn ? fadeDuration : 0f);

            return;
        }

        StopCurrentFade();
        fadeRoutine = StartCoroutine(SwitchMusicRoutine(clip, fadeIn));
    }

    public void StopMusic(bool fadeOut = true)
    {
        StopCurrentFade();

        if (!fadeOut)
        {
            audioSource.Stop();
            audioSource.clip = null;
            return;
        }

        fadeRoutine = StartCoroutine(FadeOutAndStopRoutine(fadeDuration));
    }

    public void FadeOutMusic(float duration)
    {
        StopCurrentFade();
        fadeRoutine = StartCoroutine(FadeOutAndStopRoutine(duration));
    }

    public void LoadSceneWithMusicFade(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName) || isLoadingScene)
            return;

        StopCurrentFade();
        fadeRoutine = StartCoroutine(FadeOutAndLoadRoutine(sceneName));
    }

    public void DuckMusic(float targetMultiplier, float duration)
    {
        currentMusicMultiplier = Mathf.Clamp01(targetMultiplier);

        if (!MusicEnabled || audioSource == null)
            return;

        FadeMusicToCurrentTarget(duration);
    }

    public void RestoreMusicVolume(float duration)
    {
        currentMusicMultiplier = 1f;

        if (!MusicEnabled || audioSource == null)
            return;

        FadeMusicToCurrentTarget(duration);
    }

    public void SetMusicEnabled(bool enabled)
    {
        MusicEnabled = enabled;

        PlayerPrefs.SetInt(MusicEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();

        audioSource.mute = !enabled;

        if (enabled && audioSource.clip != null && !audioSource.isPlaying)
            audioSource.Play();

        ApplyMusicSettingsInstant();
    }

    public void SetMusicVolume(float volume)
    {
        MusicVolume = Mathf.Clamp01(volume);

        PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
        PlayerPrefs.Save();

        ApplyMusicSettingsInstant();
    }

    private IEnumerator SwitchMusicRoutine(AudioClip newClip, bool fadeIn)
    {
        if (audioSource.isPlaying && MusicEnabled)
            yield return FadeToRoutine(0f, fadeDuration);

        audioSource.Stop();
        audioSource.clip = newClip;
        audioSource.mute = !MusicEnabled;

        float targetVolume = GetCurrentTargetVolume();

        audioSource.volume = fadeIn && MusicEnabled ? 0f : targetVolume;
        audioSource.Play();

        if (fadeIn && MusicEnabled)
            yield return FadeToRoutine(targetVolume, fadeDuration);
        else
            audioSource.volume = targetVolume;

        fadeRoutine = null;
    }

    private IEnumerator FadeOutAndStopRoutine(float duration)
    {
        if (audioSource.isPlaying && MusicEnabled)
            yield return FadeToRoutine(0f, duration);

        audioSource.Stop();
        audioSource.clip = null;
        fadeRoutine = null;
    }

    private IEnumerator FadeOutAndLoadRoutine(string sceneName)
    {
        isLoadingScene = true;

        if (audioSource.isPlaying && MusicEnabled)
            yield return FadeToRoutine(0f, fadeDuration);

        SceneManager.LoadScene(sceneName);

        isLoadingScene = false;
        fadeRoutine = null;
    }

    private void FadeMusicToCurrentTarget(float duration)
    {
        StopCurrentFade();
        fadeRoutine = StartCoroutine(FadeToCurrentTargetRoutine(duration));
    }

    private IEnumerator FadeToCurrentTargetRoutine(float duration)
    {
        yield return FadeToRoutine(GetCurrentTargetVolume(), duration);
        fadeRoutine = null;
    }

    private IEnumerator FadeToRoutine(float targetVolume, float duration)
    {
        if (audioSource == null)
            yield break;

        if (duration <= 0f)
        {
            audioSource.volume = targetVolume;
            yield break;
        }

        float startVolume = audioSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }

        audioSource.volume = targetVolume;
    }

    private void ApplyMusicSettingsInstant()
    {
        if (audioSource == null)
            return;

        audioSource.mute = !MusicEnabled;
        audioSource.volume = GetCurrentTargetVolume();
    }

    private float GetCurrentTargetVolume()
    {
        return MusicVolume * currentMusicMultiplier;
    }

    private void StopCurrentFade()
    {
        if (fadeRoutine == null)
            return;

        StopCoroutine(fadeRoutine);
        fadeRoutine = null;
    }
}