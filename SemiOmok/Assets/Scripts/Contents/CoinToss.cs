using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class CoinToss : MonoBehaviour
{
    [Header("Coin Manager Reference")]
    public CoinManager coinManager; // ★ 매니저에게 결과 보고용

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


