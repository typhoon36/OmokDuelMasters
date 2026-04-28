using UnityEngine;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
    [Header("Game Manager Reference")]
    public GameManager gameManager;

    [Header("Stone Prefabs")]
    public GameObject blackStonePrefab;
    public GameObject whiteStonePrefab;
    public GameObject forbiddenMarkPrefab; 
    
    [Header("Last Stone Marker")]
    [Tooltip("마지막으로 둔 돌 위치에 띄울 마커(빨간 점 등) 프리팹")]
    public GameObject lastStoneMarkerPrefab;
    [Tooltip("마커의 위치를 미세 조절합니다. (X, Y, Z)")]
    public Vector3 markerPositionOffset = new Vector3(0f, 0.05f, 0f); 
    [Tooltip("마커의 기본 회전값을 조절합니다. (기본 X축 90도)")]
    public Vector3 markerRotationOffset = new Vector3(90f, 0f, 0f); 
    private GameObject currentLastMarker; 

    [Header("Board Visual Settings")]
    public float gridSizeX = 0.05f; 
    public float gridSizeZ = 0.05f; 
    public Vector3 boardOrigin = new Vector3(-0.35f, 0, -0.35f);
    public Vector3 boardRotation = Vector3.zero; 
    public float spawnHeight = 0.5f;

    [Header("Stone Rotation Settings")]
    public Vector3 stoneRotationOffset = new Vector3(90f, 0f, 0f); 

    [Header("Pool Settings")]
    public int defaultPoolSize = 50; 
    private Queue<GameObject> blackStonePool = new Queue<GameObject>();
    private Queue<GameObject> whiteStonePool = new Queue<GameObject>();
    private Transform poolParent;

    [Header("Audio Settings")]
    public AudioSource audioSource;           
    public AudioClip blackStonePlaceSound;    
    public AudioClip whiteStonePlaceSound;    

    [Header("Preview Settings")]
    [Range(0f, 1f)]
    public float previewAlpha = 0.5f;
    private GameObject previewBlack;
    private GameObject previewWhite;

    private List<GameObject> activeForbiddenMarks = new List<GameObject>();
    
    private bool isSpaceViewMode = false;
    private bool isHiddenByViewModeDrop = false; 
       
    private void Start()
    {
        if (gameManager == null)
            gameManager = FindAnyObjectByType<GameManager>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        InitializeStonePools(); 

        previewBlack = CreatePreviewObject(blackStonePrefab);
        previewWhite = CreatePreviewObject(whiteStonePrefab);

        if (lastStoneMarkerPrefab != null)
        {
            currentLastMarker = Instantiate(lastStoneMarkerPrefab, this.transform);
            currentLastMarker.SetActive(false);
        }

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

    private void InitializeStonePools()
    {
        GameObject parentObj = new GameObject("StonePools");
        parentObj.transform.SetParent(this.transform);
        poolParent = parentObj.transform;

        for (int i = 0; i < defaultPoolSize; i++)
        {
            AddStoneToPool(GameManager.Player.Black);
            AddStoneToPool(GameManager.Player.White);
        }
    }

    private void AddStoneToPool(GameManager.Player player)
    {
        GameObject prefab = (player == GameManager.Player.Black) ? blackStonePrefab : whiteStonePrefab;
        if (prefab == null) return;

        GameObject stone = Instantiate(prefab, poolParent);
        stone.SetActive(false); 

        if (player == GameManager.Player.Black)
            blackStonePool.Enqueue(stone);
        else
            whiteStonePool.Enqueue(stone);
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
        if (isSpaceViewMode || (gameManager != null && gameManager.isSpaceHeld)) 
        {
            HidePreviews();
            return;
        }

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

                if (currentPlayer != gameManager.localPlayer || gameManager.IsForbidden(x, y, currentPlayer))
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
        if (isSpaceViewMode || (gameManager != null && gameManager.isSpaceHeld)) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane boardPlane = GetBoardPlane();

        if (boardPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            Vector2Int gridIndex = GetGridIndex(hitPoint);

            int x = gridIndex.x;
            int y = gridIndex.y;

            gameManager.PlaceStone(x, y, gameManager.localPlayer); 
        }
    }

    private void SpawnStoneVisual(int x, int y, GameManager.Player player)
    {
        Vector3 spawnPos = GetWorldPosition(x, y, spawnHeight);
        Quaternion finalRotation = Quaternion.Euler(boardRotation) * Quaternion.Euler(stoneRotationOffset);

        Queue<GameObject> pool = (player == GameManager.Player.Black) ? blackStonePool : whiteStonePool;
        GameObject stoneToPlace = null;

        if (pool.Count > 0)
        {
            stoneToPlace = pool.Dequeue();
        }
        else
        {
            GameObject prefab = (player == GameManager.Player.Black) ? blackStonePrefab : whiteStonePrefab;
            if (prefab != null)
            {
                stoneToPlace = Instantiate(prefab, poolParent);
            }
        }

        if (stoneToPlace != null)
        {
            stoneToPlace.transform.position = spawnPos;
            stoneToPlace.transform.rotation = finalRotation;
            stoneToPlace.SetActive(true);
        }

        if (currentLastMarker != null)
        {
            // ★ GameManager의 실제 spaceHeld 상태를 직접 확인하여 완벽 차단
            bool isCurrentlyHoldingSpace = (gameManager != null && gameManager.isSpaceHeld);

            if (isCurrentlyHoldingSpace) 
            {
                // 상대가 내가 스페이스 누른 사이에 돌을 둠 -> 영구 숨김 처리
                isHiddenByViewModeDrop = true;
                
                // SetActive 뿐만 아니라 아예 카메라 안 보이는 지하실로 던져버림 (2중 방어선)
                currentLastMarker.transform.position = new Vector3(0, -999f, 0); 
                currentLastMarker.SetActive(false);
            }
            else 
            {
                // 정상적으로 돌을 둠 -> 마커 위치 및 각도 갱신
                isHiddenByViewModeDrop = false;

                Vector3 worldOffset = Quaternion.Euler(boardRotation) * markerPositionOffset;
                currentLastMarker.transform.position = spawnPos + worldOffset;
                currentLastMarker.transform.rotation = Quaternion.Euler(boardRotation) * Quaternion.Euler(markerRotationOffset); 

                currentLastMarker.SetActive(true);
            }
        }

        AudioClip soundToPlay = (player == GameManager.Player.Black) ? blackStonePlaceSound : whiteStonePlaceSound;
        if (audioSource != null && soundToPlay != null)
        {
            audioSource.PlayOneShot(soundToPlay);
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

    public void UpdateMarkerVisibility(bool isVisible)
    {
        isSpaceViewMode = !isVisible; 

        if (currentLastMarker != null)
        {
            if (!isVisible) 
            {
                // 스페이스바 누른 순간 -> 무조건 끈다.
                currentLastMarker.SetActive(false);
            }
            else 
            {
                // 스페이스바 뗀 순간 -> 
                // 누른 도중 착수가 있었다면 계속 꺼둠, 아니면 원래대로 켬
                if (isHiddenByViewModeDrop)
                {
                    currentLastMarker.SetActive(false);
                }
                else
                {
                    currentLastMarker.SetActive(true);
                }
            }
        }
    }
}