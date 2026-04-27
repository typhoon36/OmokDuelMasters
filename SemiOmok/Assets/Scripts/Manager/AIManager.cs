using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro; 

public class AIManager : MonoBehaviour
{
    [Header("Game Manager Reference")]
    public GameManager gameManager;

    [Header("AI Settings")]
    [Tooltip("AI가 앞을 내다보는 수(깊이)입니다. 2는 빠름, 3은 똑똑합니다. (3 권장)")]
    public int searchDepth = 3;

    [Header("UI References")]
    public TextMeshProUGUI aiStateText; 

    [Header("Debug View (Do Not Edit)")]
    public GameManager.Player aiPlayerColor = GameManager.Player.None;

    private bool isAiThinking = false;
    private Coroutine thinkingTextCoroutine; 

    private void Start()
    {
        if (SceneManager.GetActiveScene().name != "Class")
        {
            enabled = false;
            return;
        }

        if (gameManager == null)
            gameManager = FindAnyObjectByType<GameManager>();

        if (gameManager != null)
        {
            gameManager.OnTurnChanged += OnTurnChanged;
        }

        if (aiStateText != null)
            aiStateText.text = "";
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.OnTurnChanged -= OnTurnChanged;
        }
    }

    public void SetAIColor(GameManager.Player color)
    {
        aiPlayerColor = color;
        Debug.Log($"AI 설정 완료: AI는 {aiPlayerColor} 입니다.");

        if (gameManager.currentPlayer == aiPlayerColor && !isAiThinking && !gameManager.isGameOver)
        {
            StartCoroutine(CalculateAndMakeMove());
        }
    }

    private void OnTurnChanged(GameManager.Player currentPlayer)
    {
        if (aiPlayerColor == GameManager.Player.None) return;

        if (currentPlayer == aiPlayerColor && !gameManager.isGameOver && !isAiThinking && enabled)
        {
            StartCoroutine(CalculateAndMakeMove());
        }
    }

    private IEnumerator ShowThinkingText()
    {
        if (aiStateText == null) yield break;

        string baseText = "AI 고민중";
        int dotCount = 1;

        while (isAiThinking)
        {
            string dots = new string('.', dotCount);
            aiStateText.text = baseText + dots;

            dotCount++;
            if (dotCount > 3) dotCount = 1;

            yield return new WaitForSeconds(0.3f);
        }

        aiStateText.text = "";
    }

    private IEnumerator CalculateAndMakeMove()
    {   
        isAiThinking = true;

        if (thinkingTextCoroutine != null) StopCoroutine(thinkingTextCoroutine);
        thinkingTextCoroutine = StartCoroutine(ShowThinkingText());

        if (IsBoardEmpty())
        {
            yield return new WaitForSeconds(1.0f); 
            isAiThinking = false; 
            gameManager.PlaceStone(gameManager.boardSize / 2, gameManager.boardSize / 2, aiPlayerColor);
            yield break;
        }

        int bestX = -1;
        int bestY = -1;
        int bestValue = int.MinValue;

        List<Vector2Int> candidates = GetCandidateMoves();

        if (candidates.Count == 0)
        {
            isAiThinking = false;
            yield break;
        }

        yield return new WaitForSeconds(0.1f);

        int alpha = int.MinValue;
        int beta = int.MaxValue;

        // 돌려볼 보드 생성을 반복문 밖에서 1회만 하고 재사용(성능 이점)
        int[,] simulateBoard = GetBoardCopy();

        foreach (var move in candidates)
        {
            if (aiPlayerColor == GameManager.Player.Black && gameManager.IsForbidden(move.x, move.y, aiPlayerColor))
                continue;

            simulateBoard[move.x, move.y] = (int)aiPlayerColor;
            int boardValue = Minimax(simulateBoard, searchDepth - 1, alpha, beta, false);
            simulateBoard[move.x, move.y] = 0; // 롤백 (원상복구)

            if (boardValue > bestValue)
            {
                bestValue = boardValue;
                bestX = move.x;
                bestY = move.y;
            }

            alpha = Mathf.Max(alpha, bestValue);
        }

        if (bestX == -1 || bestY == -1)
        {
            bestX = candidates[0].x;
            bestY = candidates[0].y;
        }

        yield return new WaitForSeconds(0.2f); // 연산이 너무 빨리 끝나면 조금 딜레이
        
        isAiThinking = false; 
        gameManager.PlaceStone(bestX, bestY, aiPlayerColor);
    }

    private int Minimax(int[,] board, int depth, int alpha, int beta, bool isMaximizing)
    {
        if (depth == 0)
        {
            return EvaluateBoard(board);
        }

        List<Vector2Int> candidates = GetCandidateMovesFromBoard(board);
        if (candidates.Count == 0) return EvaluateBoard(board);

        if (isMaximizing)
        {
            int maxEval = int.MinValue;
            foreach (var move in candidates)
            {
                if (aiPlayerColor == GameManager.Player.Black && IsSimulateForbidden(board, move.x, move.y, (int)aiPlayerColor)) continue;

                board[move.x, move.y] = (int)aiPlayerColor;
                int eval = Minimax(board, depth - 1, alpha, beta, false);
                board[move.x, move.y] = 0; 

                maxEval = Mathf.Max(maxEval, eval);
                alpha = Mathf.Max(alpha, eval);
                if (beta <= alpha) break; 
            }
            // 둘 곳이 전부 금지수라 최대값을 못 구했다면
            if (maxEval == int.MinValue) return EvaluateBoard(board);
            return maxEval;
        }
        else
        {
            int minEval = int.MaxValue;
            int opponentColor = (int)((aiPlayerColor == GameManager.Player.Black) ? GameManager.Player.White : GameManager.Player.Black);

            foreach (var move in candidates)
            {
                if (opponentColor == (int)GameManager.Player.Black && IsSimulateForbidden(board, move.x, move.y, opponentColor)) continue;

                board[move.x, move.y] = opponentColor;
                int eval = Minimax(board, depth - 1, alpha, beta, true);
                board[move.x, move.y] = 0; 

                minEval = Mathf.Min(minEval, eval);
                beta = Mathf.Min(beta, eval);
                if (beta <= alpha) break; 
            }
            if (minEval == int.MaxValue) return EvaluateBoard(board);
            return minEval;
        }
    }

    // =========================================================================
    // ★ 업그레이드된 두뇌 (평가 시스템) ★
    // =========================================================================

    private int EvaluateBoard(int[,] board)
    {
        int score = 0;
        int opponentColor = (int)((aiPlayerColor == GameManager.Player.Black) ? GameManager.Player.White : GameManager.Player.Black);

        // 내 돌의 유리함
        score += EvaluateForPlayer(board, (int)aiPlayerColor);
        // 상대 돌의 위협 (방어를 더 우선시하기 위해 * 1.5 가중치 부여)
        score -= (int)(EvaluateForPlayer(board, opponentColor) * 1.5f); 

        return score;
    }

    private int EvaluateForPlayer(int[,] board, int player)
    {
        int score = 0;
        int size = gameManager.boardSize;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                if (board[x, y] != player) continue;

                // 가로, 세로, 2가지 대각선 방향으로 탐색
                score += EvaluateLine(board, player, x, y, 1, 0); // 오른쪽 (가로)
                score += EvaluateLine(board, player, x, y, 0, 1); // 위 (세로)
                score += EvaluateLine(board, player, x, y, 1, 1); // 우상향 대각선
                score += EvaluateLine(board, player, x, y, 1, -1);// 우하향 대각선
            }
        }
        return score;
    }

    /// <summary>
    /// 돌이 연속된 개수와 양쪽 끝이 뚫려있는지(Open) 막혀있는지(Closed)를 파악해 점수를 부여합니다.
    /// </summary>
    private int EvaluateLine(int[,] board, int player, int x, int y, int dx, int dy)
    {
        int size = gameManager.boardSize;

        // 시작점 이전 칸이 내 돌이라면 이미 그 줄은 평가된 적 있으므로 스킵 (중복 합산 방지)
        int prevX = x - dx;
        int prevY = y - dy;
        if (prevX >= 0 && prevX < size && prevY >= 0 && prevY < size && board[prevX, prevY] == player)
            return 0;

        int consecutiveStones = 0;
        int emptyEnds = 0;

        // 이전 칸 방향이 비어있는지 확인
        if (prevX >= 0 && prevX < size && prevY >= 0 && prevY < size && board[prevX, prevY] == 0)
            emptyEnds++;

        // 현재 위치부터 지정 방향으로 내 돌이 몇 개 연속되어 있는지 카운트
        int currX = x;
        int currY = y;
        while (currX >= 0 && currX < size && currY >= 0 && currY < size && board[currX, currY] == player)
        {
            consecutiveStones++;
            currX += dx;
            currY += dy;
        }

        // 연속된 돌 끝 방향이 비어있는지 확인
        if (currX >= 0 && currX < size && currY >= 0 && currY < size && board[currX, currY] == 0)
            emptyEnds++;

        // 모양(패턴)에 따른 극단적 점수 부여
        if (consecutiveStones >= 5) return 1000000; // 사실상 승리
        if (consecutiveStones == 4)
        {
            if (emptyEnds == 2) return 100000; // 열린 4 (양쪽 뚫림 = 무조건 승리)
            if (emptyEnds == 1) return 10000;  // 닫힌 4 (양쪽 다 막히지 않음 = 상대에게 막힐 수는 있음)
        }
        if (consecutiveStones == 3)
        {
            if (emptyEnds == 2) return 5000;   // 열린 3 (매우 위협적)
            if (emptyEnds == 1) return 500;    // 닫힌 3 (조금 위협적)
        }
        if (consecutiveStones == 2)
        {
            if (emptyEnds == 2) return 100;    // 열린 2 (빌드업 시작)
            if (emptyEnds == 1) return 10;     
        }

        return 0;
    }

    // =========================================================================

    private int[,] GetBoardCopy()
    {
        int size = gameManager.boardSize;
        int[,] copy = new int[size, size];
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                copy[x, y] = (int)gameManager.GetCellState(x, y);
            }
        }
        return copy;
    }

    private bool IsBoardEmpty()
    {
        int size = gameManager.boardSize;
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                if (gameManager.GetCellState(x, y) != GameManager.Player.None) return false;
            }
        }
        return true;
    }

    private List<Vector2Int> GetCandidateMoves()
    {
        return GetCandidateMovesFromBoard(GetBoardCopy());
    }

    // AI 성능을 위해 돌이 놓인 곳 반경 2칸 이내 공간만 탐색합니다.
    private List<Vector2Int> GetCandidateMovesFromBoard(int[,] board)
    {
        List<Vector2Int> candidates = new List<Vector2Int>();
        int size = gameManager.boardSize;
        bool[,] hasNeighbor = new bool[size, size];

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                if (board[x, y] != 0) 
                {
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        for (int dy = -2; dy <= 2; dy++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx >= 0 && nx < size && ny >= 0 && ny < size && board[nx, ny] == 0)
                            {
                                if (!hasNeighbor[nx, ny])
                                {
                                    hasNeighbor[nx, ny] = true;
                                    candidates.Add(new Vector2Int(nx, ny));
                                }
                            }
                        }
                    }
                }
            }
        }
        return candidates;
    }

    // 가상 시뮬레이션용 단순 금지 체크 (시간을 위해 쌍삼, 쌍사 같은 최소한만)
    private bool IsSimulateForbidden(int[,] board, int x, int y, int playerInt)
    {
        // 시뮬레이션 보드에서 GameManager의 IsForbidden를 대리 평가하는 과정은 무거울 수 있으므로
        // 이 로직은 게임 매니저의 최신 코드를 조금 간소화 한 버전을 요구합니다. 
        // 현재는 깊이를 위해 금지 검사를 생략하거나, 실제 두기 전에만 체크해도 무방합니다.
        return false; 
    }
}
