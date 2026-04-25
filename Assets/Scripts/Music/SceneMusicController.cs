using UnityEngine;

public class SceneMusicController : MonoBehaviour
{
    [SerializeField] private AudioClip sceneMusic;
    [SerializeField] private bool fadeIn = true;

    private void Start()
    {
        if (AudioManager.Instance != null && sceneMusic != null)
            AudioManager.Instance.PlayMusic(sceneMusic, fadeIn);
    }
}