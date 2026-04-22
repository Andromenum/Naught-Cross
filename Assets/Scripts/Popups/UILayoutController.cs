using System;
using UnityEngine;

public class UILayoutController : MonoBehaviour
{
    public static UILayoutController Instance { get; private set; }

    [SerializeField] private GameObject landscapeRoot;
    [SerializeField] private GameObject portraitRoot;

    public event Action<bool> LayoutChanged;

    public bool IsPortrait { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ApplyLayout(forceNotify: false);
    }

    private void Update()
    {
        bool shouldBePortrait = Screen.height > Screen.width;

        if (shouldBePortrait != IsPortrait)
            ApplyLayout(forceNotify: true);
    }

    private void ApplyLayout(bool forceNotify)
    {
        bool newIsPortrait = Screen.height > Screen.width;
        IsPortrait = newIsPortrait;

        if (landscapeRoot != null)
            landscapeRoot.SetActive(!IsPortrait);

        if (portraitRoot != null)
            portraitRoot.SetActive(IsPortrait);

        if (forceNotify)
            LayoutChanged?.Invoke(IsPortrait);
    }
}