using System.Collections.Generic;
using Manager.Network;
using UnityEngine;

public class M_BoardManager : MonoBehaviour
{

    [Header("Game Manager Reference")]
    public M_GameManager gameManager;

    [Header("Stone Prefabs")]
    public GameObject blackStonePrefab;
    public GameObject whiteStonePrefab;
    public GameObject forbiddenMarkPrefab;


    [Header("Board Visual Settings")]
    public float gridSizeX = 0.05f;

    public float gridSizeZ = 0.05f;

    public Vector3 boardOrigin = new Vector3(-0.35f, 0, -0.35f);
    public Vector3 boardRotation = Vector3.zero;

    public float spawnHeight = 0.5f;

    [Header("Stone Rotation Settings")]
    public Vector3 stoneRotationOffset = new Vector3(90f, 0f, 0f);


    [Header("Preview Settings")]
    [Range(0f, 1f)]
    public float previewAlpha = 0.5f;
    private GameObject previewBlack;
    private GameObject previewWhite;

    private List<GameObject> activeForbiddenMarks = new List<GameObject>();


    private void Start()
    {
        if (gameManager == null)
            gameManager = FindAnyObjectByType<M_GameManager>();

        previewBlack = CreatePreviewObject(blackStonePrefab);
        previewWhite = CreatePreviewObject(whiteStonePrefab);

        if (gameManager != null)
        {
            gameManager.OnTurnChanged += UpdateForbiddenMarks;
            gameManager.OnStonePlaced += SpawnStoneVisual;
        }
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.OnTurnChanged -= UpdateForbiddenMarks;
            gameManager.OnStonePlaced -= SpawnStoneVisual;
        }
    }

    private void Update()
    {
        if (gameManager != null && (gameManager.isGameOver || (gameManager.matchingPanel != null && gameManager.matchingPanel.activeSelf)))
        {
            HidePreviews();
            return;
        }

        UpdatePreview();

        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }
    }

    private Vector3 GetWorldPosition(int x, int y, float localHeightOffset = 0f)
    {
        Vector3 localPos = new Vector3(x * gridSizeX, localHeightOffset, y * gridSizeZ);
        return boardOrigin + Quaternion.Euler(boardRotation) * localPos;
    }

    private Vector2Int GetGridIndex(Vector3 worldPosition)
    {
        Vector3 diff = worldPosition - boardOrigin;
        Vector3 localDiff = Quaternion.Inverse(Quaternion.Euler(boardRotation)) * diff;


        int x = Mathf.RoundToInt(localDiff.x / gridSizeX);
        int z = Mathf.RoundToInt(localDiff.z / gridSizeZ);


        return new Vector2Int(x, z);
    }

    private Plane GetBoardPlane()
    {
        Vector3 planeNormal = Quaternion.Euler(boardRotation) * Vector3.up;
        return new Plane(planeNormal, boardOrigin);
    }

    private void UpdateForbiddenMarks(M_GameManager.Player currentPlayer)
    {
        foreach (GameObject mark in activeForbiddenMarks)
        {
            Destroy(mark);
        }
        activeForbiddenMarks.Clear();

        // [NET][FIX] [MULTI FIX] 내가 흑돌일 때만 금수 위치를 계산하여 표시합니다.
        if (currentPlayer == M_GameManager.Player.Black && gameManager.localPlayer == M_GameManager.Player.Black)
        {
            for (int x = 0; x < gameManager.boardSize; x++)
            {
                for (int y = 0; y < gameManager.boardSize; y++)
                {
                    if (gameManager.GetCellState(x, y) == M_GameManager.Player.None &&
                        gameManager.IsForbidden(x, y, currentPlayer))
                    {
                        Vector3 markPos = GetWorldPosition(x, y, 0.01f);

                        if (forbiddenMarkPrefab != null)
                        {
                            GameObject mark = Instantiate(forbiddenMarkPrefab, markPos, Quaternion.Euler(boardRotation), this.transform);
                            activeForbiddenMarks.Add(mark);
                        }
                    }
                }
            }
        }
    }

    private GameObject CreatePreviewObject(GameObject prefab)
    {
        if (prefab == null) return null;

        GameObject previewObj = Instantiate(prefab);
        previewObj.name = prefab.name + "_Preview";

        if (previewObj.TryGetComponent(out Rigidbody rb)) Destroy(rb);
        if (previewObj.TryGetComponent(out Collider col)) Destroy(col);

        Renderer[] renderers = previewObj.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            foreach (Material mat in r.materials)
            {
                if (mat.HasProperty("_Color"))
                {
                    Color color = mat.color;
                    color.a = previewAlpha;
                    mat.color = color;
                }
            }
        }

        previewObj.SetActive(false);
        return previewObj;
    }

    private void UpdatePreview()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane boardPlane = GetBoardPlane();

        if (boardPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            Vector2Int gridIndex = GetGridIndex(hitPoint);

            int x = gridIndex.x;
            int y = gridIndex.y;

            if (x >= 0 && x < gameManager.boardSize &&
                y >= 0 && y < gameManager.boardSize &&
                gameManager.GetCellState(x, y) == M_GameManager.Player.None)
            {
                M_GameManager.Player currentPlayer = gameManager.currentPlayer;

                // [NET][FIX] [MULTI FIX] 흑돌일 때만 금수 체크를 수행하여 미리보기를 제한합니다.
                bool isForbiddenForLocal = (gameManager.localPlayer == M_GameManager.Player.Black && gameManager.IsForbidden(x, y, currentPlayer));

                if (currentPlayer != gameManager.localPlayer || isForbiddenForLocal || (gameManager.matchingPanel != null && gameManager.matchingPanel.activeSelf))
                {
                    HidePreviews();
                    return;
                }

                bool isBlackTurn = (currentPlayer == M_GameManager.Player.Black);

                if (previewBlack) previewBlack.SetActive(isBlackTurn);
                if (previewWhite) previewWhite.SetActive(!isBlackTurn);

                GameObject activePreview = isBlackTurn ? previewBlack : previewWhite;

                if (activePreview != null)
                {
                    activePreview.transform.position = GetWorldPosition(x, y, 0f);
                    activePreview.transform.rotation = Quaternion.Euler(boardRotation) * Quaternion.Euler(stoneRotationOffset);
                }
                return;

            }
        }

        HidePreviews();
    }

    private void HidePreviews()
    {
        if (previewBlack) previewBlack.SetActive(false);
        if (previewWhite) previewWhite.SetActive(false);
    }

    private void HandleMouseClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane boardPlane = GetBoardPlane();

        if (boardPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            Vector2Int gridIndex = GetGridIndex(hitPoint);

            int x = gridIndex.x;
            int y = gridIndex.y;

            if (gameManager.currentMode == M_GameManager.GameMode.MultiPlay)
            {
                // [NET][FIX] 멀티플레이 시 마스터에게 착수 요청
                if (NetworkTurnManager.Instance != null)
                {
                    NetworkTurnManager.Instance.RequestStonePlacement(x, y);
                }
            }
            else
            {
                // 로컬 모드 시 즉시 착수
                gameManager.PlaceStone(x, y, gameManager.currentPlayer);
            }
        }
    }

    public void PlaceStoneFromNetwork(int x, int y, M_GameManager.Player remoteColor)
    {
        // [NET][FIX] 네트워크로부터 확정된 착수 정보를 받아 실행
        bool success = gameManager.PlaceStone(x, y, remoteColor);

        Debug.Log($"[BoardManager] 네트워크 착수 처리: ({x}, {y}), color={remoteColor}, success={success}");
    }

    /// <summary>
    /// [NET][FIX] 보드 위의 모든 돌과 금수 마커를 제거하고 판을 초기화합니다.
    /// </summary>
    public void ClearBoard()
    {
        // 보드에 놓인 모든 돌(자식 오브젝트) 제거
        foreach (Transform child in transform)
        {
            // 미리보기 오브젝트는 제외
            if (child.gameObject == previewBlack || child.gameObject == previewWhite) continue;
            Destroy(child.gameObject);
        }

        // 금수 마커 제거
        foreach (GameObject mark in activeForbiddenMarks)
        {
            Destroy(mark);
        }
        activeForbiddenMarks.Clear();
        
        Debug.Log("[BoardManager] 판이 초기화되었습니다.");
    }

    private void SpawnStoneVisual(int x, int y, M_GameManager.Player player)
    {
        Vector3 spawnPos = GetWorldPosition(x, y, spawnHeight);

        GameObject prefabToSpawn = (player == M_GameManager.Player.Black) ? blackStonePrefab : whiteStonePrefab;
        if (prefabToSpawn != null)
        {
            Quaternion finalRotation = Quaternion.Euler(boardRotation) * Quaternion.Euler(stoneRotationOffset);
            Instantiate(prefabToSpawn, spawnPos, finalRotation, this.transform);
        }


        HidePreviews();
    }

    private void OnDrawGizmos()
    {
        int size = (gameManager != null) ? gameManager.boardSize : 15;
        Gizmos.color = Color.red;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector3 pos = GetWorldPosition(x, y, 0f);
                Gizmos.DrawWireSphere(pos, Mathf.Min(gridSizeX, gridSizeZ) * 0.2f);
            }
        }
    }
}