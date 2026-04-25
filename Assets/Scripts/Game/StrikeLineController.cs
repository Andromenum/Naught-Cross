using System.Collections;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class StrikeLineController : MonoBehaviour
{
    [Header("Sizing")]
    [SerializeField] private float lineThickness = 44f;
    [SerializeField] private float extraStartDistance = 30f;
    [SerializeField] private float extraEndDistance = 30f;

    [Header("Animation")]
    [SerializeField] private float revealDuration = 0.45f;
    [SerializeField] private AnimationCurve revealCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Uneven Reveal")]
    [SerializeField] private float progressNoiseStrength = 0.04f;
    [SerializeField] private float progressNoiseSpeed = 9f;
    [SerializeField] private float tipWobbleAmplitude = 4f;
    [SerializeField] private float tipWobbleFrequency = 14f;

    [Header("End")]
    [SerializeField] private float holdAtEndTime = 0.1f;

    private RectTransform root;
    private RectTransform coordinateSpace;
    private RectTransform revealMaskRect;
    private RectTransform lineRect;
    private RectTransform tipVisual;

    private bool refsCached;

    public float TotalDuration => revealDuration + holdAtEndTime;

    private void Awake()
    {
        EnsureReferences();
        ClearStrike();
    }

    public void ClearStrike()
    {
        StopAllCoroutines();
        EnsureReferences();

        if (revealMaskRect != null)
            revealMaskRect.sizeDelta = new Vector2(0f, lineThickness);

        if (lineRect != null)
            lineRect.sizeDelta = new Vector2(0f, lineThickness);

        if (tipVisual != null)
            tipVisual.gameObject.SetActive(false);

        if (root != null)
            root.gameObject.SetActive(false);
    }

    public IEnumerator PlayStrikeBetween(RectTransform firstCell, RectTransform lastCell)
    {
        ClearStrike();
        EnsureReferences();

        if (firstCell == null || lastCell == null || root == null || coordinateSpace == null || revealMaskRect == null || lineRect == null)
            yield break;

        Vector2 firstCenter = GetRectCenterInSpace(firstCell, coordinateSpace);
        Vector2 lastCenter = GetRectCenterInSpace(lastCell, coordinateSpace);

        Vector2 direction = lastCenter - firstCenter;
        float rawLength = direction.magnitude;

        if (rawLength <= 0.01f)
            yield break;

        direction.Normalize();

        Vector2 startPoint = firstCenter - direction * extraStartDistance;
        Vector2 endPoint = lastCenter + direction * extraEndDistance;

        Vector2 fullDelta = endPoint - startPoint;
        float fullLength = fullDelta.magnitude;
        float angle = Mathf.Atan2(fullDelta.y, fullDelta.x) * Mathf.Rad2Deg;

        root.gameObject.SetActive(true);
        root.anchoredPosition = startPoint;
        root.localRotation = Quaternion.Euler(0f, 0f, angle);
        root.sizeDelta = new Vector2(fullLength, lineThickness);

        revealMaskRect.anchoredPosition = Vector2.zero;
        revealMaskRect.localRotation = Quaternion.identity;
        revealMaskRect.sizeDelta = new Vector2(0f, lineThickness);

        lineRect.anchoredPosition = Vector2.zero;
        lineRect.localRotation = Quaternion.identity;
        lineRect.sizeDelta = new Vector2(fullLength, lineThickness);

        if (tipVisual != null)
        {
            tipVisual.gameObject.SetActive(true);
            tipVisual.anchoredPosition = Vector2.zero;
        }

        float elapsed = 0f;
        float shownProgress = 0f;
        float noiseSeed = Random.Range(0f, 999f);

        while (elapsed < revealDuration)
        {
            elapsed += Time.deltaTime;

            float normalized = Mathf.Clamp01(elapsed / revealDuration);
            float curved = revealCurve.Evaluate(normalized);

            float noise = (Mathf.PerlinNoise(noiseSeed, elapsed * progressNoiseSpeed) - 0.5f) * 2f;
            float noisyProgress = Mathf.Clamp01(curved + noise * progressNoiseStrength);

            shownProgress = Mathf.Max(shownProgress, noisyProgress);

            float currentWidth = fullLength * shownProgress;
            revealMaskRect.sizeDelta = new Vector2(currentWidth, lineThickness);

            if (tipVisual != null)
            {
                float wobbleY = Mathf.Sin(elapsed * tipWobbleFrequency) * tipWobbleAmplitude;
                tipVisual.anchoredPosition = new Vector2(currentWidth, wobbleY);
            }

            yield return null;
        }

        revealMaskRect.sizeDelta = new Vector2(fullLength, lineThickness);

        if (tipVisual != null)
        {
            tipVisual.anchoredPosition = new Vector2(fullLength, 0f);
            yield return new WaitForSeconds(holdAtEndTime);
            tipVisual.gameObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(holdAtEndTime);
        }
    }

    private void EnsureReferences()
    {
        if (refsCached)
            return;

        root = GetComponent<RectTransform>();
        coordinateSpace = transform.parent as RectTransform;

        if (transform.childCount > 0)
        {
            revealMaskRect = transform.GetChild(0) as RectTransform;

            if (revealMaskRect != null && revealMaskRect.childCount > 0)
                lineRect = revealMaskRect.GetChild(0) as RectTransform;
        }

        if (transform.childCount > 1)
            tipVisual = transform.GetChild(1) as RectTransform;

        refsCached = true;
    }

    private Vector2 GetRectCenterInSpace(RectTransform target, RectTransform space)
    {
        Canvas canvas = space.GetComponentInParent<Canvas>();
        Camera cam = null;

        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = canvas.worldCamera;

        Vector3 worldCenter = target.TransformPoint(target.rect.center);
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldCenter);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(space, screenPoint, cam, out Vector2 localPoint);
        return localPoint;
    }
}