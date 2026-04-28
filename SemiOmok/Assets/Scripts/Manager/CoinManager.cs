using UnityEngine;
using System.Collections;
using TMPro;

public class CoinManager : MonoBehaviour
{
    [Header("Manager References")]
    public GameManager gameManager;
    public AIManager aiManager;

    [Header("UI References")]
    public GameObject mainCoinTossPanel; // 전체 패널
    public GameObject choicePanel;       // 흑백 선택 패널
    public TextMeshProUGUI resultText;   // 결과 텍스트

    [Header("Delay Settings")]
    public float closeDelay = 2f;        // 창 닫히는 시간

    private bool isSelected = false;

    /// <summary>
    /// 동전 앞면(승리)이 나왔을 때 코인에서 호출
    /// </summary>
    public void TossResultWin()
    {
        if (resultText != null)
        {
            resultText.color = Color.white;
            resultText.text = "코인 토스 승리! 돌의 색상을 선택하세요.";
        }
        if (choicePanel != null) choicePanel.SetActive(true);
    }

    /// <summary>
    /// 동전 뒷면(패배)이 나왔을 때 코인에서 호출
    /// </summary>
    public void TossResultLose()
    {
        if (resultText != null)
        {
            resultText.color = Color.white;
            resultText.text = "당신이 '백' 후공입니다.";
        }
        if (choicePanel != null) choicePanel.SetActive(false);

        // 플레이어를 백, AI를 흑으로 강제 설정
        if (gameManager != null) gameManager.localPlayer = GameManager.Player.White;
        if (aiManager != null) aiManager.SetAIColor(GameManager.Player.Black);

        StartCoroutine(ClosePanelRoutine());
    }

    /// <summary>
    /// 플레이어가 UI 버튼으로 '흑'을 선택했을 때
    /// </summary>
    public void OnClickBlackButton()
    {
        if (isSelected) return;
        isSelected = true;

        if (resultText != null)
        {
            resultText.color = Color.black; 
            resultText.text = "당신이 '흑' 선공입니다.";
        }

        if (gameManager != null) gameManager.localPlayer = GameManager.Player.Black;
        if (aiManager != null) aiManager.SetAIColor(GameManager.Player.White);

        StartCoroutine(ClosePanelRoutine());
    }

    /// <summary>
    /// 플레이어가 UI 버튼으로 '백'을 선택했을 때
    /// </summary>
    public void OnClickWhiteButton()
    {
        if (isSelected) return;
        isSelected = true;

        if (resultText != null)
        {
            resultText.color = Color.black; 
            resultText.text = "당신이 '백' 후공입니다.";
        }

        if (gameManager != null) gameManager.localPlayer = GameManager.Player.White;
        if (aiManager != null) aiManager.SetAIColor(GameManager.Player.Black);

        StartCoroutine(ClosePanelRoutine());
    }

    private IEnumerator ClosePanelRoutine()
    {
        yield return new WaitForSeconds(closeDelay);
        
        isSelected = false;
        if (resultText != null) resultText.text = "";
        
        // 전체 코인 토스 UI 비활성화
        if (mainCoinTossPanel != null) mainCoinTossPanel.SetActive(false);
    }
}