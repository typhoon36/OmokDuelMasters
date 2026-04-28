using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class CoinToss : MonoBehaviour
{
    [Header("Coin Manager Reference")]
    public CoinManager coinManager; // 매니저에게 결과 보고용

    [Header("Toss Animation Settings")]
    public float tossHeight = 300f; 
    public float tossDuration = 2f; 
    public float flipSpeed = 5f;    

    [Header("Coin Sprites (50% Chance)")]
    public Sprite frontSprite; // 앞면 이미지 (승리)
    public Sprite backSprite;  // 뒷면 이미지 (패배)

    [Header("Audio Settings")]
    public AudioSource audioSource; // 오디오 소스
    public AudioClip tossSound;     // 동전 돌아가는 소리 클립

    private RectTransform rectTransform;
    private Image coinImage;
    private Vector2 originalPosition;
    private bool isTossing = false;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        coinImage = GetComponent<Image>();

        // AudioSource가 인스펙터에 안 들어있다면 스스로 찾아옵니다.
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

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

        // ★ 동전 던지기 시작 시 원샷으로 소리 재생
        if (audioSource != null && tossSound != null)
        {
            audioSource.PlayOneShot(tossSound);
        }

        int result = Random.Range(0, 2); 
        coinImage.color = Color.white;

        while (elapsed < tossDuration)
        {
            elapsed += Time.deltaTime;
            float timePercent = elapsed / tossDuration; 

            // 높이 계산
            float heightOffset = Mathf.Sin(timePercent * Mathf.PI) * tossHeight;
            rectTransform.anchoredPosition = originalPosition + new Vector2(0, heightOffset);

            // 회전 연출: 크기가 양수/음수를 오감
            float rawScaleY = Mathf.Cos(timePercent * Mathf.PI * flipSpeed * 2f);
            
            // 절댓값(Abs)을 사용하여 이미지가 상하반전(거꾸로)되는 것을 막음
            rectTransform.localScale = new Vector3(1f, Mathf.Abs(rawScaleY), 1f);

            // rawScaleY가 양수일 땐 앞면, 음수일 땐 뒷면을 실시간으로 띄워줍니다.
            if (rawScaleY >= 0)
            {
                coinImage.sprite = frontSprite;
            }
            else
            {
                coinImage.sprite = backSprite;
            }

            yield return null;
        }

        // 애니메이션이 끝나면 원래 위치와 결과 이미지로 셋팅
        rectTransform.anchoredPosition = originalPosition;
        rectTransform.localScale = Vector3.one;
        coinImage.sprite = (result == 0) ? frontSprite : backSprite;

        // 매니저에게 결과 하달
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


