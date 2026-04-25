using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class CoinToss : MonoBehaviour
{
    [Header("Toss Animation Settings")]
    public float tossHeight = 300f; 
    public float tossDuration = 2f; 
    public float flipSpeed = 5f;    

    [Header("Coin Sprites (50% Chance)")]
    public Sprite frontSprite; // 앞면 이미지 (승리)
    public Sprite backSprite;  // 뒷면 이미지 (패배)

    [Header("UI References")]
    [Tooltip("코인토스 전체를 감싸는 최상단 부모 패널")]
    public GameObject mainCoinTossPanel; // ★ 추가됨: 패배 시 직접 전체 창을 끄기 위함
    public GameObject choicePanel; // 앞면일 때 띄울 흑백 선택 창
    public TextMeshProUGUI resultText; // 떨어졌을 때 결과 문구를 띄울 외부 TMP 

    private RectTransform rectTransform;
    private Image coinImage;
    private Vector2 originalPosition;
    private bool isTossing = false;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        coinImage = GetComponent<Image>();

        originalPosition = rectTransform.anchoredPosition;

        // 시작 시 초기화 (패널 닫기, 텍스트 지우기)
        if (choicePanel != null) choicePanel.SetActive(false);
        if (resultText != null) resultText.text = "";

        StartToss();
    }

    public void StartToss()
    {
        if (!isTossing)
        {
            StartCoroutine(TossRoutine());
        }
    }

    private IEnumerator TossRoutine()
    {
        isTossing = true;
        float elapsed = 0f;

        if (choicePanel != null) choicePanel.SetActive(false);
        if (resultText != null) resultText.text = "동전을 던지는 중...";

        int result = Random.Range(0, 2); 
        coinImage.color = Color.white;

        while (elapsed < tossDuration)
        {
            elapsed += Time.deltaTime;
            float timePercent = elapsed / tossDuration; 

            float heightOffset = Mathf.Sin(timePercent * Mathf.PI) * tossHeight;
            rectTransform.anchoredPosition = originalPosition + new Vector2(0, heightOffset);

            float scaleY = Mathf.Cos(timePercent * Mathf.PI * flipSpeed * 2f);
            rectTransform.localScale = new Vector3(1f, scaleY, 1f);

            yield return null;
        }

        rectTransform.anchoredPosition = originalPosition;
        rectTransform.localScale = Vector3.one;

        coinImage.sprite = (result == 0) ? frontSprite : backSprite;

        // ★ 결과 판정 및 UI 활성화
        if (result == 0) // 앞면 (승리)
        {
            if (resultText != null) resultText.text = "코인 토스 승리! 돌의 색상을 선택하세요.";
            if (choicePanel != null) choicePanel.SetActive(true);
            
            isTossing = false; // 선택을 기다리므로 동전 상태는 여기서 해제
        }
        else // 뒷면 (패배)
        {
            if (resultText != null)
            {
                resultText.color = Color.white;
                resultText.text = "당신이 '백' 후공입니다.";
            }

                    if (choicePanel != null)
                choicePanel.SetActive(false); 

            // 꺼져있는 패널 대신, 자기 자신(CoinToss)이 직접 전체를 꺼주는 코루틴 실행
            StartCoroutine(CloseMainPanelDelay(2f));
        }
    }

    /// <summary>
    /// 뒷면(패배) 텍스트를 보여준 뒤 2초 후 코인 토스 전체 창을 닫습니다.
    /// </summary>
    private IEnumerator CloseMainPanelDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (resultText != null) resultText.text = "";
        
        isTossing = false; // 동전 던지기 상태 해제

        // 전체 패널 비활성화
        if (mainCoinTossPanel != null)
        {
            mainCoinTossPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("전체를 닫기 위한 mainCoinTossPanel이 할당되지 않았습니다.");
        }
    }
}
