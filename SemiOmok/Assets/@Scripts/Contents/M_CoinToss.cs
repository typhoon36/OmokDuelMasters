using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class M_CoinToss : MonoBehaviour
{
    [Header("Coin Manager Reference")]
    public M_CoinManager coinManager; 

    [Header("Toss Animation Settings")]
    public float tossHeight = 300f; 
    public float tossDuration = 2f; 
    public float flipSpeed = 5f;    

    [Header("Coin Sprites (50% Chance)")]
    public Sprite frontSprite; // 앞면 이미지 (승리)
    public Sprite backSprite;  // 뒷면 이미지 (패배)

    private RectTransform rectTransform;
    private Image coinImage;
    private Vector2 originalPosition;
    private bool isTossing = false;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        coinImage = GetComponent<Image>();

        originalPosition = rectTransform.anchoredPosition;

        // 멀티플레이가 아닐 때만 자동으로 시작 (멀티플레이는 NetworkTurnManager에서 호출)
        if (Photon.Pun.PhotonNetwork.InRoom == false)
        {
            StartToss();
        }
    }

    public void StartToss(int? forcedResult = null)
    {
        // 네트워크 호출 등으로 Start()보다 먼저 실행될 경우를 대비해 초기화 확인
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        
        if (coinImage == null) coinImage = GetComponent<Image>();
       
        if (originalPosition == Vector2.zero && rectTransform != null) 
            originalPosition = rectTransform.anchoredPosition;

        if (!isTossing)
        {
            StartCoroutine(TossRoutine(forcedResult));
        }
    }

    private IEnumerator TossRoutine(int? forcedResult = null)
    {
        isTossing = true;
        float elapsed = 0f;

        int result = forcedResult ?? Random.Range(0, 2); 
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

        // ★ 애니메이션 종료 후 매니저에게 결과 하달
        if (coinManager != null)
        {
            if (result == 0) coinManager.TossResultWin();
            else coinManager.TossResultLose();
        }
        else
        {
            Debug.LogWarning("CoinToss에 CoinManager가 연결되지 않았습니다.");
        }

        isTossing = false;
    }
}


