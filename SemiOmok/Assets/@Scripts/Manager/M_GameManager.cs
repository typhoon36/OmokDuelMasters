using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class M_GameManager : MonoBehaviour
{
    public enum Player { None, Black, White }
    public enum GameMode { Local, MultiPlay }

    [Header("Board Settings")]
    public int boardSize = 15;


    private Player[,] board;

    [Header("Game State")]
    public GameMode currentMode = GameMode.Local;
    public Player currentPlayer = Player.Black;

    public bool isGameOver = false;


    public Player localPlayer = Player.None;

    public Player myColor { get => localPlayer; set => localPlayer = value; }

    [Header("Optional Managers")]
    public M_AIManager aiManager;


    [Header("UI References")]
    [Tooltip("이 패널이 켜져 있는 동안에는 (사람은) 돌을 둘 수 없습니다.")]
    public GameObject coinTossPanel;

    [Tooltip("매칭 대기 패널. 이 패널이 켜져 있는 동안에도 돌을 둘 수 없습니다.")]
    public GameObject matchingPanel;

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

    private void Awake()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"[GameManager] Awake - 현재 씬 이름: {sceneName}");

        // 대소문자 구분 없이 씬 이름 체크
        string lowerSceneName = sceneName.ToLower();
        if (lowerSceneName.Contains("multiplayer") || PhotonNetwork.InRoom)
        {
            currentMode = GameMode.MultiPlay;
        }
        else if (lowerSceneName.Contains("single"))
        {
            currentMode = GameMode.Local;
        }

        // [NET][FIX] 씬 이름 또는 방 참여 여부를 통해 멀티플레이 모드를 자동으로 감지합니다.
        Debug.Log($"<color=cyan>[GameManager] Awake - 설정된 모드: {currentMode} (InRoom: {PhotonNetwork.InRoom})</color>");
    }

    private void Start()
    {
        // [NET][FIX] 빌드 환경에서 Awake 시점의 네트워크 상태가 부정확할 수 있으므로 다시 한번 확인
        if (PhotonNetwork.InRoom)
        {
            currentMode = GameMode.MultiPlay;
            Debug.Log($"[GameManager] Start - 네트워크 상태 재확인: {currentMode}");
        }

        if (targetCamera == null && Camera.main != null)
        {
            targetCamera = Camera.main.transform;
            Debug.Log("[GameManager] targetCamera가 비어 있어 Camera.main으로 설정했습니다.");
        }
        
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
        // [NET][FIX] [NET][DEBUG] 멀티 턴 상태 확인용 로그들 추가
        Debug.Log($"[PlaceStone 요청] x={x}, y={y}, request={requestPlayer}, current={currentPlayer}, local={localPlayer}, mode={currentMode}");

        // [NET][FIX] 매칭 패널이 켜져 있는 동안에는 착수를 제한합니다.
        if (matchingPanel != null && matchingPanel.activeSelf && requestPlayer == localPlayer)
        {
            Debug.LogWarning("[PlaceStone 실패] 매칭 패널이 켜져 있습니다.");
            return false;
        }

        if (requestPlayer == Player.None)
        {
            Debug.LogWarning("[PlaceStone 실패] requestPlayer가 None입니다.");
            return false;
        }

        // [NET][FIX] 코인토스 패널이 켜져 있는 동안에는 착수를 제한합니다.
        if (coinTossPanel != null && coinTossPanel.activeSelf && requestPlayer == localPlayer)
        {
            Debug.LogWarning("[PlaceStone 실패] 코인토스 패널이 켜져 있습니다.");
            return false;
        }

        if (isGameOver)
        {
            Debug.LogWarning("[PlaceStone 실패] 게임이 종료되었습니다.");
            return false;
        }

        if (currentPlayer != requestPlayer)
        {
            Debug.LogWarning($"[PlaceStone 실패] 턴 불일치 currentPlayer={currentPlayer}, requestPlayer={requestPlayer}");
            return false;
        }

        bool isLocalPlayer = requestPlayer == localPlayer;
        bool isAiPlayer = aiManager != null && aiManager.enabled && aiManager.aiPlayerColor == requestPlayer;
        bool isRemotePlayer = currentMode == GameMode.MultiPlay && requestPlayer != localPlayer;

        if (!isLocalPlayer && !isAiPlayer && !isRemotePlayer)
        {
            Debug.LogWarning($"[PlaceStone 실패] 권한 없음 request={requestPlayer}, local={localPlayer}");
            return false;
        }

        if (x < 0 || x >= boardSize || y < 0 || y >= boardSize)
        {
            Debug.LogWarning("[PlaceStone 실패] 보드 범위 밖입니다.");
            return false;
        }

        if (board[x, y] != Player.None)
        {
            Debug.LogWarning("[PlaceStone 실패] 이미 돌이 있습니다.");
            return false;
        }

        if (currentPlayer == Player.Black && IsForbidden(x, y, currentPlayer))
        {
            Debug.LogWarning("[PlaceStone 실패] 렌주룰 금수입니다.");
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

        currentPlayer = currentPlayer == Player.Black ? Player.White : Player.Black;
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
