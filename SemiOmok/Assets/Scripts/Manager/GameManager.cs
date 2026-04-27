using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class GameManager : MonoBehaviour
{
    public enum Player { None, Black, White }

    [Header("Board Settings")]
    public int boardSize = 15;
    
    private Player[,] board;

    [Header("Game State")]
    public Player currentPlayer = Player.Black; 
    public bool isGameOver = false;
    
    public Player localPlayer = Player.Black; 

    [Header("Optional Managers")]
    public AIManager aiManager; 
    
    [Header("UI References")]
    [Tooltip("이 패널이 켜져 있는 동안에는 (사람은) 돌을 둘 수 없습니다.")]
    public GameObject coinTossPanel;

    [Header("Camera Rotation Settings")]
    public Transform targetCamera; 
    public float pressedXAngle = 0f; 
    public float releasedXAngle = 90f; 
    public float rotationSpeed = 5f; 

    private bool isSpaceHeld = false;

    public event Action<int, int, Player> OnStonePlaced;
    public event Action<Player> OnTurnChanged;
    public event Action<Player> OnGameOver;

    private readonly int[][] directions = new int[][]
    {
        new int[] { 1, 0 },   
        new int[] { 0, 1 },   
        new int[] { 1, 1 },   
        new int[] { 1, -1 }   
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

    public bool PlaceStone(int x, int y, Player requestPlayer)
    {
        // ★ 수정됨: 코인 토스 패널이 활성화되어 있다면 "사람(localPlayer)의 마우스 클릭"만 무시합니다.
        // AI는 패널이 닫히는 도중이라도 정상적으로 돌을 둘 수 있습니다.
        if (coinTossPanel != null && coinTossPanel.activeSelf && requestPlayer == localPlayer) 
            return false;

        if (isGameOver) return false;
        if (currentPlayer != requestPlayer) return false;
        
        bool isLocalPlayer = (requestPlayer == localPlayer);
        bool isAiPlayer = (aiManager != null && aiManager.enabled && aiManager.aiPlayerColor == requestPlayer);
        
        if (!isLocalPlayer && !isAiPlayer)
        {
            return false;
        }
        
        if (x < 0 || x >= boardSize || y < 0 || y >= boardSize) return false;
        if (board[x, y] != Player.None) return false;

        if (currentPlayer == Player.Black && IsForbidden(x, y, currentPlayer))
        {
            Debug.LogWarning("렌주룰: 흑은 쌍삼 또는 쌍사에 돌을 둘 수 없습니다!");
            return false;
        }

        board[x, y] = currentPlayer;

        OnStonePlaced?.Invoke(x, y, currentPlayer);

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

    public bool IsForbidden(int x, int y, Player player)
    {
        if (player == Player.White) return false;
        if (x < 0 || x >= boardSize || y < 0 || y >= boardSize) return true;
        if (board[x, y] != Player.None) return false;

        board[x, y] = player; 
        bool forbidden = false;

        if (CheckWin(x, y, player)) 
        {
            board[x, y] = Player.None;
            return false;
        }

        if (GetFourCount(x, y, player) >= 2) forbidden = true;
        else if (GetOpenThreeCount(x, y, player) >= 2) forbidden = true;

        board[x, y] = Player.None; 
        return forbidden;
    }

    private bool CheckWin(int x, int y, Player player)
    {
        foreach (var dir in directions)
        {
            int count = 1 + CountStones(x, y, dir[0], dir[1], player) + CountStones(x, y, -dir[0], -dir[1], player);
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

    private int GetFourCount(int x, int y, Player player)
    {
        int fourCount = 0;
        foreach (var dir in directions)
        {
            string line = GetLinePattern(x, y, dir[0], dir[1], player);
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
            if (line.Contains("011100") || line.Contains("001110") || 
                line.Contains("010110") || line.Contains("011010"))
            {
                openThreeCount++;
            }
        }
        return openThreeCount;
    }

    private string GetLinePattern(int x, int y, int dx, int dy, Player player)
    {
        string pattern = "";
        for (int i = -4; i <= 4; i++)
        {
            int cx = x + (i * dx);
            int cy = y + (i * dy);

            if (cx < 0 || cx >= boardSize || cy < 0 || cy >= boardSize)
                pattern += "2"; 
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
