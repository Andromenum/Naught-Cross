using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class HardModeBlackoutController
{
    private void EnsureReferences()
    {
        if (overlayRect == null)
            overlayRect = GetComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (targetCanvas == null)
            targetCanvas = GetComponentInParent<Canvas>(true);

        if (blackoutImage == null)
            blackoutImage = GetComponentInChildren<Image>(true);
    }

    private void CreateRuntimeMaterial()
    {
        if (runtimeMaterial != null)
            return;

        if (blackoutImage == null)
            return;

        Material sourceMaterial = blackoutImage.material;

        if (sourceMaterial == null)
        {
            Debug.LogWarning("HardModeBlackoutController: Blackout Image has no material assigned.");
            return;
        }

        runtimeMaterial = new Material(sourceMaterial);
        blackoutImage.material = runtimeMaterial;
    }

    private void CacheActiveSources(
        Dictionary<Vector2Int, BoardCellUI> landscapeCells,
        Dictionary<Vector2Int, BoardCellUI> portraitCells,
        Dictionary<Vector2Int, int> boardState)
    {
        activeLandscapeCells = landscapeCells;
        activePortraitCells = portraitCells;
        activeBoardState = boardState;
    }

    private void ClearActiveSources()
    {
        activeLandscapeCells = null;
        activePortraitCells = null;
        activeBoardState = null;
    }
}