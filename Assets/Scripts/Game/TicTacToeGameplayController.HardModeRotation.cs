using System.Collections;
using UnityEngine;

public partial class TicTacToeGameplayController
{
    private bool ShouldUseHardModeBoardRotation()
    {
        return hardModeActive &&
               useHardModeBoardRotation &&
               (landscapeBoardRotationRoot != null || portraitBoardRotationRoot != null);
    }

    private IEnumerator PlayHardModeBoardRotationIfNeeded()
    {
        if (!ShouldUseHardModeBoardRotation())
            yield break;

        int direction = GetBoardRotationDirection();
        float rotationAmount = 90f * direction;

        float landscapeStartZ = GetLocalZRotation(landscapeBoardRotationRoot);
        float portraitStartZ = GetLocalZRotation(portraitBoardRotationRoot);

        float landscapeTargetZ = landscapeStartZ + rotationAmount;
        float portraitTargetZ = portraitStartZ + rotationAmount;

        if (boardRotationDuration <= 0f)
        {
            SetLocalZRotation(landscapeBoardRotationRoot, landscapeTargetZ);
            SetLocalZRotation(portraitBoardRotationRoot, portraitTargetZ);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < boardRotationDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / boardRotationDuration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            SetLocalZRotation(
                landscapeBoardRotationRoot,
                Mathf.Lerp(landscapeStartZ, landscapeTargetZ, easedT));

            SetLocalZRotation(
                portraitBoardRotationRoot,
                Mathf.Lerp(portraitStartZ, portraitTargetZ, easedT));

            yield return null;
        }

        SetLocalZRotation(landscapeBoardRotationRoot, landscapeTargetZ);
        SetLocalZRotation(portraitBoardRotationRoot, portraitTargetZ);
    }

    private int GetBoardRotationDirection()
    {
        if (randomizeBoardRotationDirection)
            return Random.value < 0.5f ? -1 : 1;

        return rotateClockwiseWhenNotRandom ? -1 : 1;
    }

    private void ResetBoardRotationRoots()
    {
        SetLocalZRotation(landscapeBoardRotationRoot, 0f);
        SetLocalZRotation(portraitBoardRotationRoot, 0f);
    }

    private float GetLocalZRotation(RectTransform target)
    {
        if (target == null)
            return 0f;

        return target.localEulerAngles.z;
    }

    private void SetLocalZRotation(RectTransform target, float zRotation)
    {
        if (target == null)
            return;

        Vector3 euler = target.localEulerAngles;
        euler.z = zRotation;
        target.localEulerAngles = euler;
    }
}