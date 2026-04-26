using System.Collections;
using UnityEngine;

public partial class HardModeBlackoutController
{
    private void ApplyHoleDataToMaterial(int holeCount)
    {
        if (runtimeMaterial == null || overlayRect == null)
            return;

        Rect rect = overlayRect.rect;
        float aspect = rect.height > 0f ? rect.width / rect.height : 1f;

        runtimeMaterial.SetFloat("_Aspect", aspect);
        runtimeMaterial.SetFloat("_HoleCount", holeCount);

        runtimeMaterial.SetVector("_Hole0", holeData[0]);
        runtimeMaterial.SetVector("_Hole1", holeData[1]);
        runtimeMaterial.SetVector("_Hole2", holeData[2]);
        runtimeMaterial.SetVector("_Hole3", holeData[3]);
        runtimeMaterial.SetVector("_Hole4", holeData[4]);
        runtimeMaterial.SetVector("_Hole5", holeData[5]);
        runtimeMaterial.SetVector("_Hole6", holeData[6]);
        runtimeMaterial.SetVector("_Hole7", holeData[7]);
        runtimeMaterial.SetVector("_Hole8", holeData[8]);
    }

    private void ApplyOnlySelectedHoleToMaterial()
    {
        if (selectedHoleIndex < 0 || selectedHoleIndex >= activeHoleCount)
            return;

        Vector4 selectedHole = holeData[selectedHoleIndex];

        ClearHoleData();

        holeData[0] = selectedHole;
        activeHoleCount = 1;

        ApplyHoleDataToMaterial(1);
    }

    private void ApplyBaseWobbleToMaterial()
    {
        if (runtimeMaterial == null)
            return;

        runtimeMaterial.SetFloat("_EdgeWobbleStrength", edgeWobbleStrength);
        runtimeMaterial.SetFloat("_EdgeWobbleFrequency", edgeWobbleFrequency);
        runtimeMaterial.SetFloat("_EdgeWobbleSpeed", edgeWobbleSpeed);
        runtimeMaterial.SetFloat("_EdgeRandomness", edgeRandomness);
        runtimeMaterial.SetFloat("_EdgeNoiseScale", edgeNoiseScale);
    }

    private IEnumerator PlayClickedHoleRevealExpansion(float duration)
    {
        if (runtimeMaterial == null)
            yield break;

        Vector4 startHole = holeData[0];

        float startWobbleStrength = edgeWobbleStrength;
        float targetWobbleStrength = clickedRevealWobbleTarget;

        float startRadius = startHole.z;

        float targetRadiusByMultiplier = startRadius * Mathf.Max(1f, clickedRevealRadiusMultiplier);
        float targetRadiusFullScreen = 2.5f;
        float targetRadius = Mathf.Max(targetRadiusByMultiplier, targetRadiusFullScreen);

        float startSoftness = startHole.w;

        float targetSoftness = Mathf.Max(
            startSoftness * Mathf.Max(1f, clickedRevealSoftnessMultiplier),
            0.45f
        );

        if (duration <= 0f)
        {
            runtimeMaterial.SetFloat("_EdgeWobbleStrength", targetWobbleStrength);

            holeData[0] = new Vector4(
                startHole.x,
                startHole.y,
                targetRadius,
                targetSoftness);

            ApplyHoleDataToMaterial(1);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += GetDeltaTime();

            float normalized = Mathf.Clamp01(elapsed / duration);

            float ramped = clickedRevealRampCurve != null
                ? Mathf.Clamp01(clickedRevealRampCurve.Evaluate(normalized))
                : normalized * normalized * normalized;

            float wobbleStrength = Mathf.Lerp(startWobbleStrength, targetWobbleStrength, ramped);
            float radius = Mathf.Lerp(startRadius, targetRadius, ramped);
            float softness = Mathf.Lerp(startSoftness, targetSoftness, ramped);

            runtimeMaterial.SetFloat("_EdgeWobbleStrength", wobbleStrength);

            holeData[0] = new Vector4(
                startHole.x,
                startHole.y,
                radius,
                softness);

            ApplyHoleDataToMaterial(1);

            yield return null;
        }

        runtimeMaterial.SetFloat("_EdgeWobbleStrength", targetWobbleStrength);

        holeData[0] = new Vector4(
            startHole.x,
            startHole.y,
            targetRadius,
            targetSoftness);

        ApplyHoleDataToMaterial(1);
    }
}