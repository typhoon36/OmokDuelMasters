using UnityEngine;
using System.Collections;
using TMPro;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class M_CoinManager : MonoBehaviour
{
    [Header("Manager References")]
    public M_GameManager gameManager;
    public M_AIManager aiManager;

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

        // [NET][FIX] 10초 타임아웃 코루틴 시작
        StartCoroutine(AutoSelectWhiteRoutine());
    }

    /// <summary>
    /// 동전 뒷면(패배)이 나왔을 때 코인에서 호출
    /// </summary>
    public void TossResultLose()
    {
        if (resultText != null)
        {
            resultText.color = Color.white;
            // [NET][FIX] 멀티플레이어 환경에서는 상대방의 선택을 기다리는 대기 메시지를 표시합니다.
            if (gameManager != null && gameManager.currentMode == M_GameManager.GameMode.MultiPlay)
            {
                resultText.text = "상대방이 색상을 선택 중입니다...";
            }
            else
            {
                resultText.text = "당신은 '백' 후공입니다.";
            }
        }
        if (choicePanel != null) choicePanel.SetActive(false);

        // [NET][FIX] 멀티플레이는 상대방의 선택(이벤트)을 기다려야 하므로 패널 닫기 루틴을 여기서 호출하지 않습니다.
        if (gameManager != null && gameManager.currentMode == M_GameManager.GameMode.Local)
        {
            gameManager.localPlayer = M_GameManager.Player.White;
            
            if (aiManager != null) 
            {
                aiManager.SetAIColor(M_GameManager.Player.Black);
                aiManager.enabled = true;
            }
            StartCoroutine(ClosePanelRoutine());
        }
    }

    /// <summary>
    /// [NET][FIX] 멀티플레이에서 내가 선택권이 없을 때, 상대방이 선택을 완료하면 호출됩니다.
    /// </summary>
    public void ShowRemoteSelectionResult(M_GameManager.Player remoteColor)
    {
        if (isSelected) return; // 내가 이미 선택한 경우(승리자)는 무시

        M_GameManager.Player myColor = (remoteColor == M_GameManager.Player.Black) 
            ? M_GameManager.Player.White 
            : M_GameManager.Player.Black;

        if (resultText != null)
        {
            // [NET][FIX] 문장 전체는 기본 색상(흰색)으로 두되, 단어만 리치 텍스트로 강조합니다.
            resultText.color = Color.white;

            string remoteColorStr = (remoteColor == M_GameManager.Player.Black) 
                ? "<color=black>흑</color>" 
                : "<color=white>백</color>";
            
            string myColorStr = (myColor == M_GameManager.Player.Black) 
                ? "<color=black>흑</color>" 
                : "<color=white>백</color>";
            
            resultText.text = $"상대가 {remoteColorStr}을 선택했습니다.\n당신은 {myColorStr}입니다!";
        }

        // 1.5초 뒤에 패널 닫기
        StartCoroutine(ClosePanelRoutine());
    }

    /// <summary>
    /// 플레이어가 UI 버튼으로 '흑'을 선택했을 때
    /// </summary>
    public void OnClickBlackButton()
    {
        if (isSelected) return;
        isSelected = true;

        // [NET][FIX] 선택이 완료되었으므로 타임아웃 코루틴 중단
        StopAllCoroutines();

        if (resultText != null)
        {
            resultText.color = Color.black; 
            resultText.text = "당신이 '흑' 선공입니다.";
        }

        if (gameManager != null)
        {
            gameManager.localPlayer = M_GameManager.Player.Black;
            SetStoneColorProperty(M_GameManager.Player.Black);
        }

        // AI 설정은 로컬 모드일 때만 수행
        if (gameManager != null && gameManager.currentMode == M_GameManager.GameMode.Local)
        {
            if (aiManager != null) 
            {
                aiManager.SetAIColor(M_GameManager.Player.White);
                aiManager.enabled = true;
            }
        }
        else
        {
            if (aiManager != null) aiManager.enabled = false;
        }

        StartCoroutine(ClosePanelRoutine());
    }

    /// <summary>
    /// 플레이어가 UI 버튼으로 '백'을 선택했을 때
    /// </summary>
    public void OnClickWhiteButton()
    {
        if (isSelected) return;
        isSelected = true;

        // [NET][FIX] 선택이 완료되었으므로 타임아웃 코루틴 중단
        StopAllCoroutines();

        if (resultText != null)
        {
            resultText.color = Color.white; 
            resultText.text = "당신이 '백' 후공입니다.";
        }

        if (gameManager != null)
        {
            gameManager.localPlayer = M_GameManager.Player.White;
            SetStoneColorProperty(M_GameManager.Player.White);
        }

        if (gameManager != null && gameManager.currentMode == M_GameManager.GameMode.Local)
        {
            if (aiManager != null) 
            {
                aiManager.SetAIColor(M_GameManager.Player.Black);
                aiManager.enabled = true;
            }
        }
        else
        {
            if (aiManager != null) aiManager.enabled = false;
        }

        StartCoroutine(ClosePanelRoutine());
    }

    private void SetStoneColorProperty(M_GameManager.Player color)
    {
        if (!PhotonNetwork.InRoom)
        {
            Debug.LogWarning("[CoinManager] Photon 방에 없어서 StoneColor 저장 실패");
            return;
        }

        Hashtable props = new Hashtable
        {
            ["StoneColor"] = color == M_GameManager.Player.Black ? 1 : 2
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        Debug.Log($"[CoinManager] 내 StoneColor 저장: {color}");
    }

    private IEnumerator ClosePanelRoutine()
    {
        yield return new WaitForSeconds(closeDelay);
        
        isSelected = false;
        if (resultText != null) resultText.text = "";
        
        // 전체 코인 토스 UI 비활성화
        if (mainCoinTossPanel != null) mainCoinTossPanel.SetActive(false);
    }

    /// <summary>
    /// [NET][FIX] 상대방이 나갔을 때 호출되어 코인 패널을 강제로 닫고 상태를 초기화합니다.
    /// </summary>
    public void ForceClosePanel()
    {
        StopAllCoroutines(); // 진행 중인 모든 연출 및 닫기 예약 중단
        isSelected = false;
        if (resultText != null) resultText.text = "";
        if (mainCoinTossPanel != null) mainCoinTossPanel.SetActive(false);
        if (choicePanel != null) choicePanel.SetActive(false);
        Debug.Log("[CoinManager] 상대방 퇴장으로 인해 코인 패널이 강제 종료되었습니다.");
    }

    /// <summary>
    /// [NET][FIX] 10초 동안 선택하지 않을 경우 자동으로 백돌을 선택하는 코루틴
    /// </summary>
    private IEnumerator AutoSelectWhiteRoutine()
    {
        float timer = 10f;
        while (timer > 0)
        {
            if (resultText != null)
                resultText.text = $"코인 토스 승리! 색상을 선택하세요. ({Mathf.CeilToInt(timer)}초)";
            
            yield return new WaitForSeconds(1f);
            timer -= 1f;
        }

        // 10초 경과 시 강제로 백돌 선택 실행
        if (!isSelected)
        {
            Debug.Log("[CoinManager] 10초 경과 - 자동으로 백돌을 선택합니다.");
            OnClickWhiteButton();
        }
    }
}