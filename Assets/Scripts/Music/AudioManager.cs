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

    private bool musicPausedByPauseMenu;
    private bool wasMusicPlayingBeforePause;

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

    public void PlayMusic(AudioClip clip, bool fadeIn = true, bool restartIfSameClip = true)
    {
        if (clip == null || audioSource == null)
            return;

        ClearGameplayPauseMusicState();
        StopCurrentFade();

        currentMusicMultiplier = 1f;

        bool sameClip = audioSource.clip == clip;

        if (sameClip && audioSource.isPlaying && !restartIfSameClip)
        {
            audioSource.mute = !MusicEnabled;

            if (MusicEnabled)
                FadeMusicToCurrentTarget(fadeIn ? fadeDuration : 0f);
            else
                ApplyMusicSettingsInstant();

            return;
        }

        fadeRoutine = StartCoroutine(SwitchMusicRoutine(clip, fadeIn, restartFromBeginning: true));
    }

    public void RestartMusic(AudioClip clip, bool fadeIn = true)
    {
        PlayMusic(clip, fadeIn, restartIfSameClip: true);
    }

    public void StopMusic(bool fadeOut = true)
    {
        ClearGameplayPauseMusicState();
        StopCurrentFade();

        if (audioSource == null)
            return;

        if (!fadeOut)
        {
            audioSource.Stop();
            audioSource.clip = null;
            audioSource.volume = GetCurrentTargetVolume();
            return;
        }

        fadeRoutine = StartCoroutine(FadeOutAndStopRoutine(fadeDuration));
    }

    public void FadeOutMusic(float duration)
    {
        ClearGameplayPauseMusicState();
        StopCurrentFade();

        if (audioSource == null)
            return;

        fadeRoutine = StartCoroutine(FadeOutAndStopRoutine(duration));
    }

    public void LoadSceneWithMusicFade(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName) || isLoadingScene)
            return;

        ClearGameplayPauseMusicState();
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

        if (audioSource == null)
            return;

        audioSource.mute = !enabled;

        /*
         * If gameplay pause has paused the music, settings should only change
         * the preference. It should not restart music inside the pause menu.
         */
        if (musicPausedByPauseMenu)
        {
            ApplyMusicSettingsInstant();
            return;
        }

        if (enabled && audioSource.clip != null && !audioSource.isPlaying)
        {
            audioSource.UnPause();

            if (!audioSource.isPlaying)
                audioSource.Play();
        }

        ApplyMusicSettingsInstant();
    }

    public void SetMusicVolume(float volume)
    {
        MusicVolume = Mathf.Clamp01(volume);

        PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
        PlayerPrefs.Save();

        ApplyMusicSettingsInstant();
    }

    private IEnumerator SwitchMusicRoutine(AudioClip newClip, bool fadeIn, bool restartFromBeginning)
    {
        if (audioSource == null)
            yield break;

        bool changingClip = audioSource.clip != newClip;

        if (changingClip && audioSource.isPlaying && MusicEnabled)
            yield return FadeToRoutine(0f, fadeDuration);

        audioSource.Stop();
        audioSource.clip = newClip;
        audioSource.mute = !MusicEnabled;

        if (restartFromBeginning)
            audioSource.time = 0f;

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
        if (audioSource == null)
            yield break;

        if (audioSource.isPlaying && MusicEnabled)
            yield return FadeToRoutine(0f, duration);

        audioSource.Stop();
        audioSource.clip = null;
        audioSource.volume = GetCurrentTargetVolume();
        fadeRoutine = null;
    }

    private IEnumerator FadeOutAndLoadRoutine(string sceneName)
    {
        isLoadingScene = true;

        if (audioSource != null && audioSource.isPlaying && MusicEnabled)
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

        targetVolume = Mathf.Clamp01(targetVolume);

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
        return Mathf.Clamp01(MusicVolume * currentMusicMultiplier);
    }

    private void StopCurrentFade()
    {
        if (fadeRoutine == null)
            return;

        StopCoroutine(fadeRoutine);
        fadeRoutine = null;
    }

    public void PauseMusicForGameplayPause()
    {
        if (audioSource == null)
            return;

        musicPausedByPauseMenu = true;
        wasMusicPlayingBeforePause = audioSource.isPlaying;

        if (audioSource.isPlaying)
            audioSource.Pause();
    }

    public void ResumeMusicFromGameplayPause()
    {
        if (audioSource == null)
            return;

        if (!musicPausedByPauseMenu)
            return;

        musicPausedByPauseMenu = false;

        if (wasMusicPlayingBeforePause && MusicEnabled && audioSource.clip != null)
            audioSource.UnPause();

        wasMusicPlayingBeforePause = false;

        ApplyMusicSettingsInstant();
    }

    private void ClearGameplayPauseMusicState()
    {
        musicPausedByPauseMenu = false;
        wasMusicPlayingBeforePause = false;
    }
}