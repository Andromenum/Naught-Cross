using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Graphic))]
public class TurnIndicatorPulse : MonoBehaviour
{
    [Header("Scale Pulse")]
    [SerializeField] private bool animateScale = true;
    [SerializeField] private Vector3 baseScale = Vector3.one;
    [SerializeField] private Vector3 pulseScale = new Vector3(1.08f, 1.08f, 1f);
    [SerializeField] private float scaleSpeed = 2f;

    [Header("Alpha Pulse")]
    [SerializeField] private bool animateAlpha = true;
    [SerializeField] private float minAlpha = 0.45f;
    [SerializeField] private float maxAlpha = 0.9f;
    [SerializeField] private float alphaSpeed = 2.2f;

    [Header("Rotation")]
    [SerializeField] private bool animateRotation = false;
    [SerializeField] private float rotationSpeed = 25f;

    private RectTransform rectTransform;
    private Graphic targetGraphic;
    private Color originalColor;
    private bool isActiveVisual;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        targetGraphic = GetComponent<Graphic>();

        if (targetGraphic != null)
            originalColor = targetGraphic.color;

        SetVisualActive(false, true);
    }

    private void Update()
    {
        if (!isActiveVisual)
            return;

        float timeValue = Time.time;

        if (animateScale && rectTransform != null)
        {
            float t = 0.5f + 0.5f * Mathf.Sin(timeValue * scaleSpeed * Mathf.PI * 2f);
            rectTransform.localScale = Vector3.Lerp(baseScale, pulseScale, t);
        }

        if (animateAlpha && targetGraphic != null)
        {
            float t = 0.5f + 0.5f * Mathf.Sin(timeValue * alphaSpeed * Mathf.PI * 2f);
            Color c = originalColor;
            c.a = Mathf.Lerp(minAlpha, maxAlpha, t);
            targetGraphic.color = c;
        }

        if (animateRotation && rectTransform != null)
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, timeValue * rotationSpeed);
    }

    public void SetVisualActive(bool active, bool instant = false)
    {
        isActiveVisual = active;

        if (!active)
        {
            if (rectTransform != null)
            {
                rectTransform.localScale = baseScale;
                rectTransform.localRotation = Quaternion.identity;
            }

            if (targetGraphic != null)
            {
                Color c = originalColor;
                c.a = minAlpha;
                targetGraphic.color = c;
                targetGraphic.enabled = false;
            }

            return;
        }

        if (targetGraphic != null)
            targetGraphic.enabled = true;

        if (instant)
        {
            if (rectTransform != null)
            {
                rectTransform.localScale = baseScale;
                rectTransform.localRotation = Quaternion.identity;
            }

            if (targetGraphic != null)
            {
                Color c = originalColor;
                c.a = maxAlpha;
                targetGraphic.color = c;
            }
        }
    }
}