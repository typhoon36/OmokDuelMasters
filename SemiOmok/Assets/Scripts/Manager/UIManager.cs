using UnityEngine;
using UnityEngine.EventSystems; 
using UnityEngine.SceneManagement; 

public class UIManager : MonoBehaviour
{
    [Header("UI Buttons")]
    public GameObject titleButton;
    public GameObject restartButton;

    [Header("Hover Settings")]
    public float hoverScaleMultiplier = 1.2f; 
    private Vector3 originalScale = Vector3.one;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip titleHoverSound;
    public AudioClip restartHoverSound;

    [Header("Scene Settings")]
    public string titleSceneName = "Title";

    [Header("Custom Cursor Settings")]
    [Tooltip("마우스 커서를 대신할 프리팹(Prefab)을 넣으세요. (UI Image 권장)")]
    public GameObject customCursorPrefab; 
    
    [Tooltip("커서가 생성될 캔버스(또는 패널)를 연결해 주세요.")]
    public RectTransform canvasTransform; 
    
    public bool hideDefaultCursor = true;
    
    public Vector3 cursorOffset = Vector3.zero;
    public Vector3 cursorScale = Vector3.one;
    public Vector3 cursorRotation = Vector3.zero;

    private Camera mainCam;
    private RectTransform actualCursor; 
    private Canvas parentCanvas;

    private void Start()
    {
        mainCam = Camera.main;

        if (hideDefaultCursor)
        {
            Cursor.visible = false;
        }

        // ★ 수정됨: 현재 씬이 타이틀 화면이 아닐 때만 커스텀 커서(이전 커서)를 생성합니다.
        if (SceneManager.GetActiveScene().name != titleSceneName)
        {
            if (customCursorPrefab != null && canvasTransform != null)
            {
                GameObject spawnedCursor = Instantiate(customCursorPrefab, canvasTransform);
                actualCursor = spawnedCursor.GetComponent<RectTransform>();
                
                actualCursor.anchorMin = new Vector2(0.5f, 0.5f);
                actualCursor.anchorMax = new Vector2(0.5f, 0.5f);
                actualCursor.pivot = new Vector2(0.5f, 0.5f);

                actualCursor.localScale = cursorScale;
                actualCursor.localRotation = Quaternion.Euler(cursorRotation);

                actualCursor.SetAsLastSibling();

                parentCanvas = canvasTransform.GetComponentInParent<Canvas>();
            }
        }

        if (audioSource == null)    
            audioSource = GetComponent<AudioSource>();

        if (titleButton != null)
            SetupButtonHoverEvents(titleButton, titleHoverSound);

        if (restartButton != null)
            SetupButtonHoverEvents(restartButton, restartHoverSound);
    }

    private void Update()
    {
        if (actualCursor != null && canvasTransform != null && parentCanvas != null)
        {
            Vector2 mousePos = Input.mousePosition;
            Vector2 localPoint;

            Camera cam = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCam;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasTransform, mousePos, cam, out localPoint))
            {
                actualCursor.localPosition = new Vector3(localPoint.x, localPoint.y, 0f) + cursorOffset;
            }
        }
    }

    private void SetupButtonHoverEvents(GameObject btnObj, AudioClip hoverClip)
    {
        EventTrigger trigger = btnObj.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = btnObj.AddComponent<EventTrigger>();
        }

        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => { OnHoverEnter(btnObj.transform, hoverClip); });
        trigger.triggers.Add(enterEntry);

        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => { OnHoverExit(btnObj.transform); });
        trigger.triggers.Add(exitEntry);
    }

    private void OnHoverEnter(Transform btnTransform, AudioClip clip)
    {
        btnTransform.localScale = originalScale * hoverScaleMultiplier;

        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void OnHoverExit(Transform btnTransform)
    {
        btnTransform.localScale = originalScale;
    }

    public void GoToTitle()
    {
        Debug.Log($"[UIManager] GoToTitle 호출됨. 대상 씬: {titleSceneName}");
        Time.timeScale = 1f; 

        // [NET][FIX] 멀티플레이 중이라면 방을 나갑니다.
        if (Photon.Pun.PhotonNetwork.InRoom)
        {
            Debug.Log("[UIManager] Photon 방 나가는 중...");
            Photon.Pun.PhotonNetwork.LeaveRoom();
        }

        Debug.Log($"[UIManager] {titleSceneName} 씬 로드 시작");
        SceneManager.LoadScene(titleSceneName);
    }

    public void RestartScene()
    {
        Time.timeScale = 1f;
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    public void SetCursorVisible(bool isVisible)
    {
        if (actualCursor != null)
        {
            actualCursor.gameObject.SetActive(isVisible);
        }
    }
}
