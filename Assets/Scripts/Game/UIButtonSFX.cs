using UnityEngine;
using UnityEngine.UI;

public class UIButtonSFX : MonoBehaviour
{
    /// <summary>
    /// Script used to add a SFX click sound to buttons/toggles in the gameplay scene.
    /// SFXManager is not present in the scene at edit time because it persists from the main menu.
    /// 
    /// If this object has a Toggle, the script uses the Toggle.
    /// Otherwise, it uses a Button.
    /// </summary>

    [Header("SFX")]
    [SerializeField] private string sfxId = "button_click";
    [SerializeField, Range(0f, 1f)] private float volumeScale = 1f;

    private Button button;
    private Toggle toggle;

    private void Awake()
    {
        toggle = GetComponent<Toggle>();

        if (toggle != null)
        {
            toggle.onValueChanged.AddListener(HandleToggleValueChanged);
            return;
        }

        button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(PlayClickSFX);
        else
            Debug.LogWarning($"{nameof(UIButtonSFX)} on {gameObject.name} needs either a Button or a Toggle.");
    }

    private void OnDestroy()
    {
        if (toggle != null)
            toggle.onValueChanged.RemoveListener(HandleToggleValueChanged);

        if (button != null)
            button.onClick.RemoveListener(PlayClickSFX);
    }

    private void HandleToggleValueChanged(bool value)
    {
        PlayClickSFX();
    }

    public void PlayClickSFX()
    {
        if (SFXManager.Instance == null)
            return;

        if (!string.IsNullOrWhiteSpace(sfxId))
            SFXManager.Instance.PlayById(sfxId, volumeScale);
        else
            SFXManager.Instance.PlayButtonClick();
    }
}