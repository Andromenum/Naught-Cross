using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 1.5f;

    private AudioSource audioSource;
    private float targetVolume;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        targetVolume = audioSource.volume;
        audioSource.volume = 0f;
    }

    private void Start()
    {
        if (audioSource.clip == null)
            return;

        audioSource.Play();
        StartCoroutine(FadeInRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        if (fadeDuration <= 0f)
        {
            audioSource.volume = targetVolume;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            audioSource.volume = Mathf.Lerp(0f, targetVolume, t);
            yield return null;
        }

        audioSource.volume = targetVolume;
    }
}