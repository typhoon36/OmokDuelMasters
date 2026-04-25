using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public enum Player { None, Black, White }

    [Header("Board Settings")]
    public int boardSize = 15;
    
    private Player[,] board;

    [Header("Game State")]
    public Player currentPlayer = Player.Black; 
    public bool isGameOver = false;

    [Header("Camera Rotation Settings")]
    public Transform targetCamera; 
    public float pressedXAngle = 0f; 
    public float releasedXAngle = 90f; 
    public float rotationSpeed = 5f; 

    private bool isSpaceHeld = false;

    public event Action<Player> OnTurnChanged;
    public event Action<Player> OnGameOver;

    private readonly int[][] directions = new int[][]
    {
        new int[] { 1, 0 },   // 가로
        new int[] { 0, 1 },   // 세로
        new int[] { 1, 1 },   // 대각선 ↘
        new int[] { 1, -1 }   // 대각선 ↗
    };

    private void Start()
    {
        InitializeGame();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartScene();
        }

        if (Input.GetKeyDown(KeyCode.Space)) isSpaceHeld = true;
        else if (Input.GetKeyUp(KeyCode.Space)) isSpaceHeld = false;

        if (targetCamera != null)
        {
            float targetAngle = isSpaceHeld ? pressedXAngle : releasedXAngle;
            Vector3 currentEuler = targetCamera.rotation.eulerAngles;
            float newX = Mathf.LerpAngle(currentEuler.x, targetAngle, Time.deltaTime * rotationSpeed);
            targetCamera.rotation = Quaternion.Euler(newX, currentEuler.y, currentEuler.z);
        }
    }

    private void RestartScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    public void InitializeGame()
    {
        board = new Player[boardSize, boardSize];
        currentPlayer = Player.Black;
        isGameOver = false;

        for (int i = 0; i < boardSize; i++)
            for (int j = 0; j < boardSize; j++)
                board[i, j] = Player.None;
        
        OnTurnChanged?.Invoke(currentPlayer);
    }

    public bool PlaceStone(int x, int y)
    {
        if (isGameOver) return false;
        if (x < 0 || x >= boardSize || y < 0 || y >= boardSize) return false;
        if (board[x, y] != Player.None) return false;

        // 렌주룰: 흑돌일 경우 착수 금지점(쌍삼, 쌍사) 검사 (6목 금지는 해제됨)
        if (currentPlayer == Player.Black && IsForbidden(x, y, currentPlayer))
        {
            Debug.LogWarning("렌주룰: 흑은 쌍삼 또는 쌍사에 돌을 둘 수 없습니다!");
            return false;
        }

        board[x, y] = currentPlayer;

        // 승리 판정: 흑은 정확히 5목만, 백은 5목 이상이면 승리
        if (CheckWin(x, y, currentPlayer))
        {
            isGameOver = true;
            OnGameOver?.Invoke(currentPlayer);
            Debug.Log($"{currentPlayer} Wins!");
            return true;
        }

        currentPlayer = (currentPlayer == Player.Black) ? Player.White : Player.Black;
        OnTurnChanged?.Invoke(currentPlayer);

        return true;
    }

    /// <summary>
    /// 외부에서 보드매니저가 금지 아이콘(X)을 표시할 때 호출하는 함수입니다.
    /// </summary>
    public bool IsForbidden(int x, int y, Player player)
    {
        // 백은 금지점이 없음
        if (player == Player.White) return false;
        if (x < 0 || x >= boardSize || y < 0 || y >= boardSize) return true;
        if (board[x, y] != Player.None) return false;

        board[x, y] = player; // 가상 착수
        bool forbidden = false;

        // 1. 오목이 성립하면 금지룰(쌍삼, 쌍사)보다 우선하여 승리(금지 아님)
        if (CheckWin(x, y, player)) 
        {
            board[x, y] = Player.None;
            return false;
        }

        // 2. 장목 (6목 이상) 검사 제거 (요청에 의해 착수는 가능하게 허용)
        // if (IsOverline(x, y, player)) forbidden = true;

        // 3. 쌍사 (4-4)
        if (GetFourCount(x, y, player) >= 2) forbidden = true;
        // 4. 쌍삼 (3-3)
        else if (GetOpenThreeCount(x, y, player) >= 2) forbidden = true;

        board[x, y] = Player.None; // 롤백
        return forbidden;
    }

    private bool CheckWin(int x, int y, Player player)
    {
        foreach (var dir in directions)
        {
            int count = 1 + CountStones(x, y, dir[0], dir[1], player) + CountStones(x, y, -dir[0], -dir[1], player);
            
            // 흑(Black)은 정확히 5목만 승리, 백(White)은 5목 이상이면 승리
            // (따라서 흑은 6목을 둘 수는 있지만 승리로 인정되지 않고 게임이 계속됨)
            if (player == Player.Black && count == 5) return true;
            if (player == Player.White && count >= 5) return true;
        }
        return false;
    }

    private int CountStones(int startX, int startY, int dirX, int dirY, Player player)
    {
        int count = 0;
        int cx = startX + dirX;
        int cy = startY + dirY;

        while (cx >= 0 && cx < boardSize && cy >= 0 && cy < boardSize && board[cx, cy] == player)
        {
            count++;
            cx += dirX;
            cy += dirY;
        }
        return count;
    }

    // --- 렌주룰(오목 판별) 세부 로직 ---
    
    private int GetFourCount(int x, int y, Player player)
    {
        int fourCount = 0;
        foreach (var dir in directions)
        {
            string line = GetLinePattern(x, y, dir[0], dir[1], player);
            // 4가 되는 패턴: 연속 4이거나 한 칸 띄워진(Split) 4결합 (막히지 않고 5가 될 가능성이 있는 것)
            if (line.Contains("11110") || line.Contains("01111") || 
                line.Contains("11101") || line.Contains("11011") || line.Contains("10111"))
            {
                fourCount++;
            }
        }
        return fourCount;
    }

    private int GetOpenThreeCount(int x, int y, Player player)
    {
        int openThreeCount = 0;
        foreach (var dir in directions)
        {
            string line = GetLinePattern(x, y, dir[0], dir[1], player);
            // 열린 3 패턴 (양쪽이 빈 공간으로 열려있어 4가 될 수 있어야 함)
            if (line.Contains("011100") || line.Contains("001110") || 
                line.Contains("010110") || line.Contains("011010"))
            {
                openThreeCount++;
            }
        }
        return openThreeCount;
    }

    // 검사를 위해 특정 방향의 9칸 돌 상태를 "0(비음), 1(내돌), 2(벽/상대돌)" 문자열로 변환합니다.
    private string GetLinePattern(int x, int y, int dx, int dy, Player player)
    {
        string pattern = "";
        Player opponent = (player == Player.Black) ? Player.White : Player.Black;

        // 기준점에서 앞뒤로 4칸씩, 총 9칸 검사
        for (int i = -4; i <= 4; i++)
        {
            int cx = x + (i * dx);
            int cy = y + (i * dy);

            if (cx < 0 || cx >= boardSize || cy < 0 || cy >= boardSize)
            {
                pattern += "2"; // 맵 밖은 막힌 것과 같음
            }
            else
            {
                if (board[cx, cy] == player) pattern += "1";
                else if (board[cx, cy] == Player.None) pattern += "0";
                else pattern += "2";
            }
        }
        return pattern;
    }

    public Player GetCellState(int x, int y)
    {
        if (x < 0 || x >= boardSize || y < 0 || y >= boardSize) return Player.None;
        return board[x, y];
    }
}
