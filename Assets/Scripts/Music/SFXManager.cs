using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SFXManager : MonoBehaviour
{
    [Header("Default Clips")]
    [SerializeField] private AudioClip globalClickClip;
    [SerializeField] private AudioClip buttonClickClip;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
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
        if (clip == null)
            return;

        audioSource.PlayOneShot(clip);
    }

    public void SetVolume(float volume)
    {
        audioSource.volume = Mathf.Clamp01(volume);
    }
}