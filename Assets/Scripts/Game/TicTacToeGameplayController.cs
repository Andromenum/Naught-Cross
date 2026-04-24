using System.Collections.Generic;
using UnityEngine;

public class TicTacToeGameplayController : MonoBehaviour
{
    [Header("Board Roots")]
    [SerializeField] private Transform landscapeBoardCellsRoot;
    [SerializeField] private Transform portraitBoardCellsRoot;
    [SerializeField] private int requiredInRow = 3;

    [Header("Scene References")]
    [SerializeField] private GameplayHUDController gameplayHUDController;
    [SerializeField] private VictoryPopupController victoryPopupController;

    private readonly Dictionary<Vector2Int, int> boardState = new Dictionary<Vector2Int, int>();

    private readonly Dictionary<Vector2Int, BoardCellUI> landscapeCellLookup = new Dictionary<Vector2Int, BoardCellUI>();
    private readonly Dictionary<Vector2Int, BoardCellUI> portraitCellLookup = new Dictionary<Vector2Int, BoardCellUI>();

    private MatchPlayerData player1Data;
    private MatchPlayerData player2Data;

    private Sprite player1MarkSprite;
    private Sprite player2MarkSprite;

    private bool isPlayer1Turn = true;
    private bool gameEnded;
    private float matchStartTime;
    private int moveCount;

    private static readonly Vector2Int[] CheckDirections =
    {
        new Vector2Int(1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(1, 1),
        new Vector2Int(1, -1)
    };

    private void Start()
    {
        if (!ValidateSettings())
            return;

        if (!LoadMatchData())
            return;

        CacheBoardCells(landscapeBoardCellsRoot, landscapeCellLookup, "Landscape");
        CacheBoardCells(portraitBoardCellsRoot, portraitCellLookup, "Portrait");

        BeginMatch();
    }

    private void OnEnable()
    {
        if (UILayoutController.Instance != null)
            UILayoutController.Instance.LayoutChanged += HandleLayoutChanged;
    }

    private void OnDisable()
    {
        if (UILayoutController.Instance != null)
            UILayoutController.Instance.LayoutChanged -= HandleLayoutChanged;
    }

    private bool ValidateSettings()
    {
        if (requiredInRow <= 0)
        {
            Debug.LogWarning("TicTacToeGameplayController: requiredInRow must be greater than 0.");
            return false;
        }

        if (landscapeBoardCellsRoot == null && portraitBoardCellsRoot == null)
        {
            Debug.LogWarning("TicTacToeGameplayController: Both board roots are missing.");
            return false;
        }

        return true;
    }

    private bool LoadMatchData()
    {
        if (GameSessionManager.Instance == null || !GameSessionManager.Instance.HasValidMatchSetup)
        {
            Debug.LogWarning("TicTacToeGameplayController: Game session data is missing.");
            return false;
        }

        player1Data = GameSessionManager.Instance.Player1;
        player2Data = GameSessionManager.Instance.Player2;

        if (player1Data == null || player2Data == null)
        {
            Debug.LogWarning("TicTacToeGameplayController: Player data is invalid.");
            return false;
        }

        if (player1Data.selectedMarkSprite == null)
        {
            Debug.LogWarning("TicTacToeGameplayController: Player 1 selected mark sprite is missing.");
            return false;
        }

        if (player2Data.selectedMarkSprite == null)
        {
            Debug.LogWarning("TicTacToeGameplayController: Player 2 selected mark sprite is missing.");
            return false;
        }

        player1MarkSprite = player1Data.selectedMarkSprite;
        player2MarkSprite = player2Data.selectedMarkSprite;

        return true;
    }

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

    private void BeginMatch()
    {
        boardState.Clear();

        ClearAllBoardViews();

        isPlayer1Turn = true;
        gameEnded = false;
        moveCount = 0;
        matchStartTime = Time.time;

        if (victoryPopupController != null)
            victoryPopupController.ClosePopup();

        if (gameplayHUDController != null)
        {
            gameplayHUDController.LoadFromSession();
            gameplayHUDController.SetCurrentTurnToPlayer1();
        }

        RefreshAllBoardViews();
    }

    private void HandleCellClicked(Vector2Int cellPosition)
    {
        if (gameEnded)
            return;

        if (!CellExistsInAnyBoard(cellPosition))
            return;

        if (boardState.ContainsKey(cellPosition))
            return;

        int currentPlayerValue = isPlayer1Turn ? 1 : 2;
        Sprite currentMarkSprite = isPlayer1Turn ? player1MarkSprite : player2MarkSprite;

        boardState[cellPosition] = currentPlayerValue;
        moveCount++;

        ApplyMarkToAllBoardViews(cellPosition, currentMarkSprite);

        if (HasWinFromLastMove(cellPosition, currentPlayerValue))
        {
            FinishWithWinner(isPlayer1Turn ? player1Data : player2Data, isPlayer1Turn ? player2Data : player1Data);
            return;
        }

        if (moveCount >= GetBoardCellCount())
        {
            FinishWithDraw();
            return;
        }

        isPlayer1Turn = !isPlayer1Turn;

        if (gameplayHUDController != null)
        {
            if (isPlayer1Turn)
                gameplayHUDController.SetCurrentTurnToPlayer1();
            else
                gameplayHUDController.SetCurrentTurnToPlayer2();
        }

        RefreshAllBoardViews();
    }

    private bool HasWinFromLastMove(Vector2Int lastMove, int playerValue)
    {
        if (moveCount < (requiredInRow * 2) - 1)
            return false;

        for (int i = 0; i < CheckDirections.Length; i++)
        {
            Vector2Int dir = CheckDirections[i];

            int totalConnected = 1;
            totalConnected += CountDirection(lastMove, dir, playerValue);
            totalConnected += CountDirection(lastMove, -dir, playerValue);

            if (totalConnected >= requiredInRow)
                return true;
        }

        return false;
    }

    private int CountDirection(Vector2Int start, Vector2Int direction, int playerValue)
    {
        int count = 0;
        Vector2Int current = start + direction;

        while (boardState.TryGetValue(current, out int cellValue) && cellValue == playerValue)
        {
            count++;
            current += direction;
        }

        return count;
    }

    private void FinishWithWinner(MatchPlayerData winner, MatchPlayerData loser)
    {
        gameEnded = true;
        RefreshAllBoardViews();

        if (gameplayHUDController != null)
            gameplayHUDController.ClearTurnIndicators();

        float matchDuration = Mathf.Max(0f, Time.time - matchStartTime);

        if (PlayerProfilesManager.Instance != null && winner != null && loser != null)
            PlayerProfilesManager.Instance.RecordMatchWinnerLoser(
                winner.profileSlotIndex,
                loser.profileSlotIndex,
                matchDuration);

        if (victoryPopupController != null)
            victoryPopupController.ShowWinner(winner);
    }

    private void FinishWithDraw()
    {
        gameEnded = true;
        RefreshAllBoardViews();

        if (gameplayHUDController != null)
            gameplayHUDController.ClearTurnIndicators();

        float matchDuration = Mathf.Max(0f, Time.time - matchStartTime);

        if (PlayerProfilesManager.Instance != null && player1Data != null && player2Data != null)
            PlayerProfilesManager.Instance.RecordMatchDraw(
                player1Data.profileSlotIndex,
                player2Data.profileSlotIndex,
                matchDuration);

        if (victoryPopupController != null)
            victoryPopupController.ShowDraw();
    }

    private void HandleLayoutChanged(bool isPortrait)
    {
        RefreshAllBoardViews();
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

                bool interactable = !gameEnded;
                cell.SetInteractable(interactable);
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

    private bool CellExistsInAnyBoard(Vector2Int pos)
    {
        return landscapeCellLookup.ContainsKey(pos) || portraitCellLookup.ContainsKey(pos);
    }

    private int GetBoardCellCount()
    {
        return landscapeCellLookup.Count > 0 ? landscapeCellLookup.Count : portraitCellLookup.Count;
    }
}