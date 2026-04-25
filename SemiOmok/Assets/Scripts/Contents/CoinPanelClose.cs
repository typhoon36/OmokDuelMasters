using System.Collections;
using UnityEngine;
using TMPro; // TextMeshPro를 사용하기 위해 추가

public class CoinPanelClose : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI resultText; 
    
    [Tooltip("인스펙터에서 코인 토스 전체를 감싸는 최상단 부모 패널을 연결해 주세요.")]
    public GameObject mainCoinTossPanel; // ★ 추가: 초이스 패널뿐만 아니라 코인토스 전체를 닫기 위한 참조

    [Header("옵션: 게임 매니저 제어용")]
    public GameManager gameManager; 

    // 중복 클릭을 방지하기 위한 플래그
    private bool isSelected = false;
    [Header("옵션: 씬넘어갈타이밍")]
    public float timer = 1f;
    /// <summary>
    /// 흑돌(Black) 버튼의 OnClick 이벤트에 연결하세요.
    /// </summary>
    public void OnClickBlackButton()
    {
        if (isSelected) return; // 이미 눌렀다면 무시
        
        Debug.Log("플레이어: 흑돌 선공 선택!");
        
        // TMP 텍스트 변경
        if (resultText != null)
        {
            resultText.color = Color.black;
            resultText.text = "당신이 '흑' 선공입니다.";
        }

        // 패널 전체 닫기 코루틴 시작 (2초 대기)
        StartCoroutine(CloseAfterDelay(timer));
    }

    /// <summary>
    /// 백돌(White) 버튼의 OnClick 이벤트에 연결하세요.
    /// </summary>
    public void OnClickWhiteButton()
    {
        if (isSelected) return;

        Debug.Log("플레이어: 백돌 후공 선택!");

        // TMP 텍스트 변경
        if (resultText != null)
        { 
            resultText.color = Color.black;
            resultText.text = "당신이 '백' 후공입니다.";
        }

        StartCoroutine(CloseAfterDelay(timer));
    }

    /// <summary>
    /// 외부(예: CoinToss 스크립트에서 뒷면이 나왔을 때)에서 전체 패널을 닫고 싶을 때 호출합니다.
    /// </summary>
    public void StartDelayedClose(float delay)
    {
        if (!isSelected)
        {
            StartCoroutine(CloseAfterDelay(delay));
        }
    }

    /// <summary>
    /// 지정된 시간만큼 대기한 후 선택 패널과 상위의 코인토스 전체 패널을 닫습니다.
    /// </summary>
    private IEnumerator CloseAfterDelay(float delay)
    {
        isSelected = true; // 대기 중에는 다른 버튼을 누르지 못하도록 잠금

        // delay(2초) 만큼 대기
        yield return new WaitForSeconds(delay);

        // 다음에 다시 창이 열릴 때를 대비해 상태 초기화
        isSelected = false;
        
        if (resultText != null)
            resultText.text = ""; // 남아있는 텍스트 지우기

        // ★ 초이스 패널 자기 자신뿐만 아니라, 코인 토스 창 전체(부모 패널)를 닫아줍니다.
        if (mainCoinTossPanel != null)
        {
            mainCoinTossPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("전체 창 역할을 할 mainCoinTossPanel이 연결되어 있지 않습니다!");
        }

        // 패널 비활성화 (닫기)
        gameObject.SetActive(false);
    }
}
