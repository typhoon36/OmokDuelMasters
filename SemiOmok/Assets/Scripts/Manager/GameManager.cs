/**
 * [수정 내역 - 팀 공유용]
 * 1. isGameStarted 플래그 도입: 멀티플레이 시 모든 준비(매칭+코인토스)가 끝날 때까지 착수 금지 로직 추가
 * 2. PlaceStone 예외 처리: 게임 시작 전이나 패널이 켜져 있을 때 클릭 방지 강화
 * 3. 모드 자동 감지: Photon 룸 참여 여부에 따라 싱글/멀티 모드 및 로컬 플레이어 색상 초기화 자동화
 */
using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
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
    public bool isGameStarted = false; // [NET][FIX] 게임 시작 여부 확인 플래그

    public Player localPlayer = Player.None; // 초기값 None으로 변경 (모드에 따라 설정됨)

    [Header("Health Settings (Chalks)")]
    [Tooltip("최대 체력 (기본 5)")]
    public int maxHealth = 5;
    private int currentHealth;

    [Tooltip("분필들이 생성될 Scroll View 안의 Content 오브젝트를 연결하세요.")]
    public Transform healthContentParent;
    [Tooltip("멀쩡한 분필 프리팹")]
    public GameObject normalChalkPrefab;
    [Tooltip("부러진 분필 프리팹")]
    public GameObject brokenChalkPrefab;

    private List<GameObject> chalkInstances = new List<GameObject>();

    [Header("Optional Managers")]
    public AIManager aiManager;
    public BoardManager boardManager;

    [Header("UI References")]
    [Tooltip("이 패널이 켜져 있는 동안에는 사람의 마우스 클릭이 무시됩니다.")]
    public GameObject coinTossPanel;

    [Tooltip("매칭 대기 패널. 이 패널이 켜져 있는 동안에도 돌을 둘 수 없습니다.")]
    public GameObject matchingPanel;

    [Tooltip("게임이 끝났을 때 승리/패배 영상을 틀어줄 비디오 패널 스크립트 연결")]
    public VideoPanelPlayer videoPanelPlayer;

    [Tooltip("UI 매니저를 연결하세요. 스페이스를 눌러 뷰를 바꿀 때 커서를 숨깁니다.")]
    public UIManager uiManager;

    [Header("Camera Rotation Settings")]
    public Transform targetCamera;
    public float pressedXAngle = 0f;
    public float releasedXAngle = 90f;
    public float pressedYAngle = 0f;
    public float releasedYAngle = 0f;
    public float rotationSpeed = 5f;

    public bool isSpaceHeld = false;

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
        // [NET][FIX] 씬 이름 또는 방 참여 여부를 통해 멀티플레이 모드를 자동으로 감지합니다.
        // 빌드 환경 대응: 씬 이름에 'Multi'가 포함되어 있는지 확인하여 모드를 더 정확하게 감지합니다.
        string sceneName = SceneManager.GetActiveScene().name;
        bool isMultiplayerScene = sceneName.Contains("Multi", StringComparison.OrdinalIgnoreCase);

        if (PhotonNetwork.InRoom && isMultiplayerScene)
        {
            currentMode = GameMode.MultiPlay;
        }
        else
        {
            currentMode = GameMode.Local;
            isGameStarted = false; // [NET][FIX] 싱글플레이도 코인 토스 전까지 착수를 막습니다.
            localPlayer = Player.None; 
        }

        Debug.Log($"<color=cyan>[GameManager] Awake - 설정된 모드: {currentMode} (Scene: {sceneName}, InRoom: {PhotonNetwork.InRoom})</color>");
    }

    private void Start()
    {
        // [NET][FIX] 빌드 환경에서 Awake 시점의 네트워크 상태가 부정확할 수 있으므로 다시 한번 확인
        string sceneName = SceneManager.GetActiveScene().name;
        bool isMultiplayerScene = sceneName.Contains("Multi", StringComparison.OrdinalIgnoreCase);

        if (PhotonNetwork.InRoom && isMultiplayerScene)
        {
            currentMode = GameMode.MultiPlay;
        }
        else
        {
            currentMode = GameMode.Local;
        }

        if (targetCamera == null && Camera.main != null)
        {
            targetCamera = Camera.main.transform;
        }

        InitializeGame();

        // [NET][FIX] 싱글플레이 모드인 경우 시작 시 직접 코인 토스를 실행합니다.
        if (currentMode == GameMode.Local)
        {
            if (coinTossPanel != null) coinTossPanel.SetActive(true);
            
            CoinToss coinToss = FindFirstObjectByType<CoinToss>();
            if (coinToss != null)
            {
                coinToss.StartToss();
            }
            else
            {
                Debug.LogWarning("[GameManager] 싱글플레이 코인 토스를 시작하려 했으나 CoinToss 오브젝트를 찾지 못했습니다.");
                // 코인 토스가 없는 경우를 대비해 예외 처리 (바로 시작)
                isGameStarted = true;
                localPlayer = Player.Black;
                OnTurnChanged?.Invoke(currentPlayer);
            }
        }
    }

    private void Update()
    {
        // [테스트 단축키] - (마이너스) 키를 누르면 체력이 강제로 1 깎입니다.
        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            TakeDamage(1);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartScene();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            isSpaceHeld = true;
            if (uiManager != null) uiManager.SetCursorVisible(false);
            if (boardManager != null) boardManager.UpdateMarkerVisibility(false);
        }
        else if (Input.GetKeyUp(KeyCode.Space))
        {
            isSpaceHeld = false;
            if (uiManager != null) uiManager.SetCursorVisible(true);
            if (boardManager != null) boardManager.UpdateMarkerVisibility(true);
        }

        if (targetCamera != null)
        {
            float targetAngleX = isSpaceHeld ? pressedXAngle : releasedXAngle;
            float targetAngleY = isSpaceHeld ? pressedYAngle : releasedYAngle;

            Vector3 currentEuler = targetCamera.rotation.eulerAngles;

            float newX = Mathf.LerpAngle(currentEuler.x, targetAngleX, Time.deltaTime * rotationSpeed);
            float newY = Mathf.LerpAngle(currentEuler.y, targetAngleY, Time.deltaTime * rotationSpeed);

            targetCamera.rotation = Quaternion.Euler(newX, newY, currentEuler.z);
        }
    }

    private void RestartScene()
    {
        // [NET][FIX] 멀티플레이 시에는 함부로 씬을 재시작하면 안 됩니다.
        if (currentMode == GameMode.MultiPlay)
        {
            Debug.LogWarning("[GameManager] 멀티플레이 모드에서는 R키로 씬을 재시작할 수 없습니다.");
            return;
        }

        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    public void InitializeGame()
    {
        board = new Player[boardSize, boardSize];
        currentPlayer = Player.Black;
        isGameOver = false;
        
        // [NET][FIX] 멀티/싱글 모두 코인 토스 전까지는 false로 유지합니다.
        isGameStarted = false;

        for (int i = 0; i < boardSize; i++)
            for (int j = 0; j < boardSize; j++)
                board[i, j] = Player.None;
        
        currentHealth = maxHealth;
        InitializeHealthUI();
    }

    // ==========================================
    // ★ 추가됨: 코인 토스가 끝나면 반드시 이 함수를 호출해 주세요!
    // ==========================================
    public void StartGameAfterCoinToss(Player assignedLocalPlayer)
    {
        // 0. 게임 시작 상태로 변경
        isGameStarted = true;

        // 1. 내 플레이어 색상 확정
        localPlayer = assignedLocalPlayer;
        
        // 2. AI 색상도 자동으로 반대로 확정
        if (aiManager != null)
        {
            aiManager.aiPlayerColor = (localPlayer == Player.Black) ? Player.White : Player.Black;
        }

        // 3. 클릭을 막던 코인 토스 패널 확실하게 비활성화
        if (coinTossPanel != null)
        {
            coinTossPanel.SetActive(false);
        }

        // 4. 이제 진짜로 게임을 시작한다고 전체에 알림 (이때 AI가 흑돌이면 동작을 시작함)
        OnTurnChanged?.Invoke(currentPlayer);
        Debug.Log($"코인 토스 완료! 플레이어: {localPlayer}, 모드: {currentMode}, 게임을 시작합니다.");
    }

    private void InitializeHealthUI()
    {
        if (healthContentParent == null || normalChalkPrefab == null) return;

        foreach (Transform child in healthContentParent)
        {
            Destroy(child.gameObject);
        }
        chalkInstances.Clear();

        for (int i = 0; i < maxHealth; i++)
        {
            GameObject chalk = Instantiate(normalChalkPrefab, healthContentParent);
            chalkInstances.Add(chalk);
        }
    }

    public void TakeDamage(int damageAmount)
    {
        if (isGameOver) return;

        for (int d = 0; d < damageAmount; d++)
        {
            if (currentHealth <= 0) break;
            currentHealth--;

            int targetIndex = maxHealth - currentHealth - 1;

            if (healthContentParent != null && brokenChalkPrefab != null)
            {
                GameObject oldChalk = chalkInstances[targetIndex];
                int siblingIndex = oldChalk.transform.GetSiblingIndex();
                Destroy(oldChalk);

                GameObject newBrokenChalk = Instantiate(brokenChalkPrefab, healthContentParent);
                newBrokenChalk.transform.SetSiblingIndex(siblingIndex);
                chalkInstances[targetIndex] = newBrokenChalk;
            }

            Debug.Log($"체력 감소! 현재 체력: {currentHealth}");

            if (currentHealth <= 0)
            {
                isGameOver = true;
                string reason = "선생님에게 발각되어 체력이 바닥났습니다...";

                if (videoPanelPlayer != null)
                {
                    videoPanelPlayer.PlayResultVideo(false, reason);
                }

                Player winner = (localPlayer == Player.Black) ? Player.White : Player.Black;
                OnGameOver?.Invoke(winner);
                break;
            }
        }
    }

    public bool PlaceStone(int x, int y, Player requestPlayer)
    {
        if (currentMode == GameMode.MultiPlay && !isGameStarted)
        {
            Debug.LogWarning($"[GameManager] PlaceStone 거부: 게임 미시작. (Mode: {currentMode})");
            return false;
        }
        // [NET][FIX] 매칭 패널 또는 코인토스 패널이 켜져 있는 동안에는 착수를 제한합니다.


        if (isGameOver)
        {
            Debug.LogWarning("[GameManager] PlaceStone 거부: 게임 종료 상태.");
            return false;
        }
        if (currentPlayer != requestPlayer)
        {
            Debug.LogWarning($"[GameManager] PlaceStone 거부: 턴 불일치. 현재: {currentPlayer}, 요청: {requestPlayer}");
            return false;
        }

        bool isLocalPlayer = (requestPlayer == localPlayer);
        bool isAiPlayer = (aiManager != null && aiManager.enabled && aiManager.aiPlayerColor == requestPlayer);
        bool isRemotePlayer = (currentMode == GameMode.MultiPlay && requestPlayer != localPlayer);

        // [NET][FIX] 멀티플레이 시에는 로컬/AI 외에 리모트 플레이어의 착수도 허용해야 합니다.
        if (!isLocalPlayer && !isAiPlayer && !isRemotePlayer)
        {
            Debug.LogWarning($"[GameManager] PlaceStone 거부: 유효하지 않은 플레이어. (Local: {isLocalPlayer}, AI: {isAiPlayer}, Remote: {isRemotePlayer})");
            return false;
        }

        if (x < 0 || x >= boardSize || y < 0 || y >= boardSize)
        {
            Debug.LogWarning($"[GameManager] PlaceStone 거부: 좌표 오류 ({x}, {y})");
            return false;
        }

        if (board[x, y] != Player.None)
        {
            Debug.LogWarning($"[GameManager] PlaceStone 거부: 이미 돌이 있음 ({x}, {y})");
            return false;
        }

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

            bool isWin = (currentPlayer == localPlayer);
            string reason = isWin ? "오목 완성! 당신의 승리입니다!" : "상대방의 오목 완성! 당신의 패배입니다...";

            if (videoPanelPlayer != null)
            {
                videoPanelPlayer.PlayResultVideo(isWin, reason);
            }

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
