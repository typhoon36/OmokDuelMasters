using UnityEngine;
using UnityEngine.EventSystems; 
using UnityEngine.SceneManagement; 
using UnityEngine.UI; 

public class UIManager : MonoBehaviour
{
    [Header("Menu UI")]
    [Tooltip("ESC를 눌렀을 때 띄울 메뉴 패널을 연결하세요.")]
    public GameObject menuPanel;
    private bool isMenuOpen = false;

    [Header("UI Hover Effects - Chalk")]
    [Tooltip("분필 느낌이 나는 UI 요소들을 여기에 다 연결하세요.")]
    public Image[] chalkHoverImages; 
    [Tooltip("분필 UI용 호버 사운드를 연결하세요.")]
    public AudioClip chalkHoverSoundClip;

    [Header("UI Hover Effects - Other")]
    [Tooltip("그 외의 일반 UI 요소들을 여기에 다 연결하세요.")]
    public Image[] otherHoverImages; 
    [Tooltip("그 외 일반 UI용 호버 사운드를 연결하세요.")]
    public AudioClip otherHoverSoundClip;

    [Header("Hover Common Settings")]
    public float hoverScaleMultiplier = 1.2f; 
    private Vector3 originalScale = Vector3.one;

    [Header("Audio Settings")]
    public AudioSource audioSource;

    [Header("Scene Settings")]
    public string titleSceneName = "Title";

    [Header("Custom Cursor Settings")]
    public GameObject customCursorPrefab; 
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

        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
            isMenuOpen = false;
        }

        if (hideDefaultCursor)
        {
            Cursor.visible = false;
        }

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

        // Chalk 이미지 배열에 호버 이벤트와 해당 사운드 연결
        foreach (Image img in chalkHoverImages)
        {
            if (img != null)
            {
                SetupHoverEvents(img.gameObject, chalkHoverSoundClip);
            }
        }

        // Other 이미지 배열에 호버 이벤트와 해당 사운드 연결
        foreach (Image img in otherHoverImages)
        {
            if (img != null)
            {
                SetupHoverEvents(img.gameObject, otherHoverSoundClip);
            }
        }
    }

    private void Update()
    {
        // ESC 키 입력 감지: 메뉴창 켜기/끄기
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }

        if (actualCursor != null && canvasTransform != null && parentCanvas != null)
        {
            actualCursor.SetAsLastSibling();

            Vector2 mousePos = Input.mousePosition;
            Vector2 localPoint;

            Camera cam = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCam;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasTransform, mousePos, cam, out localPoint))
            {
                actualCursor.localPosition = new Vector3(localPoint.x, localPoint.y, 0f) + cursorOffset;
            }
        }
    }

    // ==========================================
    // ★ 인게임 메뉴 컨트롤 함수
    // ==========================================

    /// <summary>
    /// ESC 키를 누르거나 관련 버튼을 누를 때 메뉴창을 열고 닫는 함수
    /// </summary>
    public void ToggleMenu()
    {
        if (menuPanel == null) return;

        isMenuOpen = !isMenuOpen;
        menuPanel.SetActive(isMenuOpen);

        // 메뉴가 열리면 게임 정지
        Time.timeScale = isMenuOpen ? 0f : 1f;

        // 메뉴창이 떴을 때 사용자가 클릭하기 쉽도록 혹시나 꺼져있었을 커서를 켜줍니다.
        if (isMenuOpen)
        {
            SetCursorVisible(true);
        }
    }

    /// <summary>
    /// 인스펙터의 '계속하기' 버튼 OnClick에 직접 연결할 함수입니다.
    /// </summary>
    public void ResumeGame()
    {
        if (isMenuOpen)
        {
            ToggleMenu();
        }
    }

    // ==========================================
    // ★ 호버 이벤트 로직
    // ==========================================

    private void SetupHoverEvents(GameObject targetObj, AudioClip hoverClip)
    {
        EventTrigger trigger = targetObj.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = targetObj.AddComponent<EventTrigger>();
        }

        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => { OnHoverEnter(targetObj.transform, hoverClip); });
        trigger.triggers.Add(enterEntry);

        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => { OnHoverExit(targetObj.transform); });
        trigger.triggers.Add(exitEntry);
    }

    private void OnHoverEnter(Transform targetTransform, AudioClip clip)
    {
        // Time.timeScale이 0이어도 크기는 즉시 바뀝니다.
        targetTransform.localScale = originalScale * hoverScaleMultiplier;

        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void OnHoverExit(Transform targetTransform)
    {
        targetTransform.localScale = originalScale;
    }

    // ==========================================

    public void GoToTitle()
    {
        Debug.Log($"[UIManager] GoToTitle 호출됨. 대상 씬: {titleSceneName}");
        
        // 타이틀로 돌아가기 전 타임스케일을 무조건 1로 강제 복구 (필수)
        Time.timeScale = 1f; 

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
