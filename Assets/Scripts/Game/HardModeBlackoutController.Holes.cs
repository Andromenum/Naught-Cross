using System.Collections.Generic;
using UnityEngine;

public partial class HardModeBlackoutController
{
    private void RefreshHoleDataForCurrentLayout()
    {
        if (!blackoutRoutineActive)
            return;

        if (selectionMade || blackoutCancelled)
            return;

        if (runtimeMaterial == null || overlayRect == null)
            return;

        int refreshedHoleCount = BuildHoleData(activeLandscapeCells, activePortraitCells, activeBoardState);

        activeHoleCount = refreshedHoleCount;

        ApplyHoleDataToMaterial(activeHoleCount);
        ApplyBaseWobbleToMaterial();
    }

    private bool TryGetHoleClicked(Vector2 screenPosition, out Vector2Int clickedCell, out int clickedHoleIndex)
    {
        clickedCell = Vector2Int.zero;
        clickedHoleIndex = -1;

        if (overlayRect == null || activeHoleCount <= 0)
            return false;

        Rect overlayLocalRect = overlayRect.rect;

        if (overlayLocalRect.width <= 0f || overlayLocalRect.height <= 0f)
            return false;

        Camera canvasCamera = GetCanvasCamera();

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                overlayRect,
                screenPosition,
                canvasCamera,
                out Vector2 localPoint))
        {
            return false;
        }

        float uvX = Mathf.InverseLerp(overlayLocalRect.xMin, overlayLocalRect.xMax, localPoint.x);
        float uvY = Mathf.InverseLerp(overlayLocalRect.yMin, overlayLocalRect.yMax, localPoint.y);

        Vector2 clickUv = new Vector2(uvX, uvY);
        float aspect = overlayLocalRect.height > 0f ? overlayLocalRect.width / overlayLocalRect.height : 1f;

        for (int i = 0; i < activeHoleCount; i++)
        {
            Vector4 hole = holeData[i];

            Vector2 delta = clickUv - new Vector2(hole.x, hole.y);
            delta.x *= aspect;

            float distance = delta.magnitude;
            float clickableRadius = hole.z * clickRadiusMultiplier;

            if (distance <= clickableRadius)
            {
                clickedCell = holeGridPositions[i];
                clickedHoleIndex = i;
                return true;
            }
        }

        return false;
    }

    private int BuildHoleData(
        Dictionary<Vector2Int, BoardCellUI> landscapeCells,
        Dictionary<Vector2Int, BoardCellUI> portraitCells,
        Dictionary<Vector2Int, int> boardState)
    {
        ClearHoleData();

        int holeCount = 0;

        AddHolesFromLookup(landscapeCells, boardState, ref holeCount);
        AddHolesFromLookup(portraitCells, boardState, ref holeCount);

        return holeCount;
    }

    private void AddHolesFromLookup(
        Dictionary<Vector2Int, BoardCellUI> lookup,
        Dictionary<Vector2Int, int> boardState,
        ref int holeCount)
    {
        if (lookup == null)
            return;

        foreach (KeyValuePair<Vector2Int, BoardCellUI> pair in lookup)
        {
            if (holeCount >= MaxHoleCount)
                return;

            if (boardState != null && boardState.ContainsKey(pair.Key))
                continue;

            BoardCellUI cell = pair.Value;

            if (cell == null || !cell.gameObject.activeInHierarchy)
                continue;

            RectTransform cellRect = cell.transform as RectTransform;

            if (cellRect == null)
                continue;

            if (TryBuildHoleFromCell(cellRect, out Vector4 hole))
            {
                holeData[holeCount] = hole;
                holeGridPositions[holeCount] = pair.Key;
                holeCount++;
            }
        }
    }

    private bool TryBuildHoleFromCell(RectTransform cellRect, out Vector4 hole)
    {
        hole = Vector4.zero;

        if (cellRect == null || overlayRect == null)
            return false;

        Rect overlayLocalRect = overlayRect.rect;

        if (overlayLocalRect.width <= 0f || overlayLocalRect.height <= 0f)
            return false;

        Camera canvasCamera = GetCanvasCamera();

        cellRect.GetWorldCorners(worldCorners);

        bool hasPoint = false;
        Vector2 min = Vector2.zero;
        Vector2 max = Vector2.zero;

        for (int i = 0; i < worldCorners.Length; i++)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(canvasCamera, worldCorners[i]);

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    overlayRect,
                    screenPoint,
                    canvasCamera,
                    out Vector2 localPoint))
            {
                continue;
            }

            if (!hasPoint)
            {
                min = localPoint;
                max = localPoint;
                hasPoint = true;
            }
            else
            {
                min = Vector2.Min(min, localPoint);
                max = Vector2.Max(max, localPoint);
            }
        }

        if (!hasPoint)
            return false;

        Vector2 centerLocal = (min + max) * 0.5f;
        Vector2 sizeLocal = max - min;

        float radiusLocal = Mathf.Max(sizeLocal.x, sizeLocal.y) * 0.5f * holeRadiusMultiplier;

        if (radiusLocal <= 0f)
            return false;

        float radiusNormalized = radiusLocal / overlayLocalRect.height;
        float softnessNormalized = radiusNormalized * holeSoftnessMultiplier;

        float uvX = Mathf.InverseLerp(overlayLocalRect.xMin, overlayLocalRect.xMax, centerLocal.x);
        float uvY = Mathf.InverseLerp(overlayLocalRect.yMin, overlayLocalRect.yMax, centerLocal.y);

        hole = new Vector4(uvX, uvY, radiusNormalized, softnessNormalized);
        return true;
    }

    private void ClearHoleData()
    {
        for (int i = 0; i < holeData.Length; i++)
        {
            holeData[i] = Vector4.zero;
            holeGridPositions[i] = Vector2Int.zero;
        }

        activeHoleCount = 0;
    }
}