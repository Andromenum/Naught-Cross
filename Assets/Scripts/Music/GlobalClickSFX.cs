using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SFXManager))]
public class GlobalClickSFX : MonoBehaviour
{
    [SerializeField] private float minTimeBetweenClicks = 0.05f;

    private SFXManager sfxManager;
    private float lastClickTime = -999f;

    private void Awake()
    {
        sfxManager = GetComponent<SFXManager>();
    }

    private void Update()
    {
        bool mouseClicked =
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame;

        bool touchBegan =
            Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.wasPressedThisFrame;

        if (!mouseClicked && !touchBegan)
            return;

        if (Time.unscaledTime - lastClickTime < minTimeBetweenClicks)
            return;

        lastClickTime = Time.unscaledTime;
        sfxManager.PlayGlobalClick();
    }
}