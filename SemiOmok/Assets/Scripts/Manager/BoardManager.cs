using UnityEngine;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
    [Header("Game Manager Reference")]
    public GameManager gameManager;

    [Header("Stone Prefabs")]
    public GameObject blackStonePrefab;
    public GameObject whiteStonePrefab;
    public GameObject forbiddenMarkPrefab; // 렌주룰 착수 금지점을 나타낼 X 마크 프리팹

    [Header("Board Visual Settings")]
    public float gridSizeX = 0.05f; 
    public float gridSizeZ = 0.05f; 
    public Vector3 boardOrigin = new Vector3(-0.35f, 0, -0.35f);
    public Vector3 boardRotation = Vector3.zero; // 바둑판 전체 회전값
    public float spawnHeight = 0.5f;

    [Header("Stone Rotation Settings")]
    [Tooltip("돌을 생성할 때 추가로 회전시킬 각도입니다. (기본 X축 90도)")]
    public Vector3 stoneRotationOffset = new Vector3(90f, 0f, 0f); // ★ 추가: 프리팹 개별 회전 오프셋

    [Header("Preview Settings")]
    [Range(0f, 1f)]
    public float previewAlpha = 0.5f;
    private GameObject previewBlack;
    private GameObject previewWhite;

    // 생성된 금지 마크들을 담아둘 리스트
    private List<GameObject> activeForbiddenMarks = new List<GameObject>();
       
    private void Start()
    {
        if (gameManager == null)
            gameManager = FindAnyObjectByType<GameManager>();

        previewBlack = CreatePreviewObject(blackStonePrefab);
        previewWhite = CreatePreviewObject(whiteStonePrefab);

        if (gameManager != null)
        {
            gameManager.OnTurnChanged += UpdateForbiddenMarks;
        }
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.OnTurnChanged -= UpdateForbiddenMarks;
        }
    }

    private void Update()
    {
        if (gameManager != null && gameManager.isGameOver)
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

    /// <summary>
    /// 배열 인덱스(x, y)를 바둑판의 회전값이 적용된 실제 3D 월드 좌표로 변환합니다.
    /// </summary>
    private Vector3 GetWorldPosition(int x, int y, float localHeightOffset = 0f)
    {
        Vector3 localPos = new Vector3(x * gridSizeX, localHeightOffset, y * gridSizeZ);
        return boardOrigin + Quaternion.Euler(boardRotation) * localPos;
    }

    /// <summary>
    /// 실제 3D 월드 좌표를 바둑판의 회전값을 역연산하여 배열 인덱스(x, y)로 반환합니다.
    /// </summary>
    private Vector2Int GetGridIndex(Vector3 worldPosition)
    {
        Vector3 diff = worldPosition - boardOrigin;
        Vector3 localDiff = Quaternion.Inverse(Quaternion.Euler(boardRotation)) * diff;
        
        int x = Mathf.RoundToInt(localDiff.x / gridSizeX);
        int z = Mathf.RoundToInt(localDiff.z / gridSizeZ);
        
        return new Vector2Int(x, z);
    }

    /// <summary>
    /// 마우스를 클릭할 가상의 평면(Plane)을 반환합니다. 
    /// </summary>
    private Plane GetBoardPlane()
    {
        Vector3 planeNormal = Quaternion.Euler(boardRotation) * Vector3.up;
        return new Plane(planeNormal, boardOrigin);
    }

    private void UpdateForbiddenMarks(GameManager.Player currentPlayer)
    {
        foreach (GameObject mark in activeForbiddenMarks)
        {
            Destroy(mark);
        }
        activeForbiddenMarks.Clear();

        if (currentPlayer == GameManager.Player.Black)
        {
            for (int x = 0; x < gameManager.boardSize; x++)
            {
                for (int y = 0; y < gameManager.boardSize; y++)
                {
                    if (gameManager.GetCellState(x, y) == GameManager.Player.None &&
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
                gameManager.GetCellState(x, y) == GameManager.Player.None)
            {
                GameManager.Player currentPlayer = gameManager.currentPlayer;

                if (gameManager.IsForbidden(x, y, currentPlayer))
                {
                    HidePreviews();
                    return;
                }

                bool isBlackTurn = (currentPlayer == GameManager.Player.Black);

                if (previewBlack) previewBlack.SetActive(isBlackTurn);
                if (previewWhite) previewWhite.SetActive(!isBlackTurn);

                GameObject activePreview = isBlackTurn ? previewBlack : previewWhite;

                if (activePreview != null)
                {
                    activePreview.transform.position = GetWorldPosition(x, y, 0f);
                    
                    // ★ 보드 회전값에 돌 전용 오프셋 각도(X:90)를 더해서 적용
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

            GameManager.Player currentPlayer = gameManager.currentPlayer;
            bool isPlaced = gameManager.PlaceStone(x, y);

            if (isPlaced)
            {
                SpawnStoneVisual(x, y, currentPlayer);
                HidePreviews();
            }
        }
    }

    private void SpawnStoneVisual(int x, int y, GameManager.Player player)
    {
        Vector3 spawnPos = GetWorldPosition(x, y, spawnHeight);

        GameObject prefabToSpawn = (player == GameManager.Player.Black) ? blackStonePrefab : whiteStonePrefab;

        if (prefabToSpawn != null)
        {
            // ★ 생성 시 보드 회전값에 돌 전용 각도 오프셋 적용
            Quaternion finalRotation = Quaternion.Euler(boardRotation) * Quaternion.Euler(stoneRotationOffset);
            Instantiate(prefabToSpawn, spawnPos, finalRotation, this.transform);
        }
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