using System.Collections.Generic;
using UnityEngine;

public partial class TicTacToeGameplayController
{
    private void CacheBoardCells(
        Transform root,
        Dictionary<Vector2Int, BoardCellUI> targetLookup,
        string label)
    {
        targetLookup.Clear();

        if (root == null)
            return;

        int childCount = root.childCount;

        for (int i = 0; i < childCount; i++)
        {
            BoardCellUI cell = root.GetChild(i).GetComponent<BoardCellUI>();
            if (cell == null)
                continue;

            Vector2Int pos = cell.GridPosition;

            if (targetLookup.ContainsKey(pos))
            {
                Debug.LogWarning($"{label} board has duplicate cell grid position: {pos}");
                continue;
            }

            targetLookup.Add(pos, cell);
            cell.Setup(HandleCellClicked);
        }

        if (targetLookup.Count == 0)
            Debug.LogWarning($"{label} board has no valid BoardCellUI components.");
    }

    private void RefreshAllBoardViews()
    {
        RefreshBoardView(landscapeCellLookup);
        RefreshBoardView(portraitCellLookup);
    }

    private void RefreshBoardView(Dictionary<Vector2Int, BoardCellUI> lookup)
    {
        foreach (KeyValuePair<Vector2Int, BoardCellUI> pair in lookup)
        {
            BoardCellUI cell = pair.Value;
            if (cell == null)
                continue;

            Vector2Int pos = pair.Key;

            if (boardState.TryGetValue(pos, out int playerValue))
            {
                Sprite markSprite = playerValue == 1 ? player1MarkSprite : player2MarkSprite;
                cell.SetMark(markSprite);
            }
            else
            {
                cell.ClearMark();

                bool canInteract =
                    matchStarted &&
                    !gameEnded &&
                    !isGameplayPaused &&
                    !isChaosTransitionRunning;

                cell.SetInteractable(canInteract);
            }
        }
    }

    private void ClearAllBoardViews()
    {
        ClearBoardView(landscapeCellLookup);
        ClearBoardView(portraitCellLookup);
    }

    private void ClearBoardView(Dictionary<Vector2Int, BoardCellUI> lookup)
    {
        foreach (KeyValuePair<Vector2Int, BoardCellUI> pair in lookup)
        {
            if (pair.Value != null)
                pair.Value.ClearMark();
        }
    }

    private void ApplyMarkToAllBoardViews(Vector2Int pos, Sprite markSprite)
    {
        if (landscapeCellLookup.TryGetValue(pos, out BoardCellUI landscapeCell) && landscapeCell != null)
            landscapeCell.SetMark(markSprite);

        if (portraitCellLookup.TryGetValue(pos, out BoardCellUI portraitCell) && portraitCell != null)
            portraitCell.SetMark(markSprite);
    }

    private void ClearAllStrikeLines()
    {
        if (landscapeStrikeLine != null)
            landscapeStrikeLine.ClearStrike();

        if (portraitStrikeLine != null)
            portraitStrikeLine.ClearStrike();
    }

    private bool CellExistsInAnyBoard(Vector2Int pos)
    {
        return landscapeCellLookup.ContainsKey(pos) || portraitCellLookup.ContainsKey(pos);
    }

    private int GetBoardCellCount()
    {
        return landscapeCellLookup.Count > 0 ? landscapeCellLookup.Count : portraitCellLookup.Count;
    }
}