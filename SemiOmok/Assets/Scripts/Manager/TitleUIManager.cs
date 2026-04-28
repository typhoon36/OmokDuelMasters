using UnityEngine;
using UnityEngine.EventSystems; // UI 이벤트 시스템 사용을 위해 추가

public class TitleUIManager : MonoBehaviour
{
    [Header("UI Buttons")]
    [Tooltip("마우스를 올렸을 때 커질 버튼들을 4개 연결하세요.")]
    public GameObject[] titleButtons;

    [Header("Hover Settings")]
    public float hoverScaleMultiplier = 1.2f;
    private Vector3 originalScale = Vector3.one;

    [Header("Audio Settings")]
    [Tooltip("사운드를 재생할 오디오 소스(Audio Source)를 연결하세요.")]
    public AudioSource audioSource;
    [Tooltip("버튼에 마우스를 올렸을 때 한 번(OneShot) 재생할 클립을 연결하세요.")]
    public AudioClip hoverSoundClip;

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

        // 기본 커서 숨기기 옵션
        if (hideDefaultCursor)
        {
            Cursor.visible = false;
        }

        // 커스텀 커서 생성 및 초기화
        if (customCursorPrefab != null && canvasTransform != null)
        {
            GameObject spawnedCursor = Instantiate(customCursorPrefab, canvasTransform);
            actualCursor = spawnedCursor.GetComponent<RectTransform>();
            
            // 프리팹 앵커를 정중앙으로 초기화하여 계산 오류 방지
            actualCursor.anchorMin = new Vector2(0.5f, 0.5f);
            actualCursor.anchorMax = new Vector2(0.5f, 0.5f);
            actualCursor.pivot = new Vector2(0.5f, 0.5f);

            actualCursor.localScale = cursorScale;
            actualCursor.localRotation = Quaternion.Euler(cursorRotation);

            // UI 렌더링 최상단 설정
            actualCursor.SetAsLastSibling();

            parentCanvas = canvasTransform.GetComponentInParent<Canvas>();
        }

        // 배열에 연결된 모든 버튼에 호버 이벤트를 자동 세팅합니다.
        foreach (GameObject btn in titleButtons)
        {
            if (btn != null)
            {
                SetupButtonHoverEvents(btn);
            }
        }
    }

    private void Update()
    {
        // 커서 추적 로직 (캔버스 모드 완벽 대응)
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

    private void SetupButtonHoverEvents(GameObject btnObj)
    {
        EventTrigger trigger = btnObj.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = btnObj.AddComponent<EventTrigger>();
        }

        // Pointer Enter (마우스 진입)
        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => { OnHoverEnter(btnObj.transform); });
        trigger.triggers.Add(enterEntry);

        // Pointer Exit (마우스 이탈)
        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => { OnHoverExit(btnObj.transform); });
        trigger.triggers.Add(exitEntry);
    }

    private void OnHoverEnter(Transform btnTransform)
    {
        // 크기 증가
        btnTransform.localScale = originalScale * hoverScaleMultiplier;

        // ★ 사운드 재생 추가
        if (audioSource != null && hoverSoundClip != null)
        {
            audioSource.PlayOneShot(hoverSoundClip);
        }
    }

    private void OnHoverExit(Transform btnTransform)
    {
        // 크기 복구
        btnTransform.localScale = originalScale;
    }
}
