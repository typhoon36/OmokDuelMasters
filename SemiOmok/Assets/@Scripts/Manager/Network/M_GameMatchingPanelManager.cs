using System.Text;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

namespace Manager.Network
{
    /// <summary>
    /// [D4] 매칭 상태 동기화 관리 스크립트
    /// 매칭 완료 시 카운트다운을 진행하고, 코인토스 패널을 활성화한다.
    /// 모든 클라이언트 간의 시작 시점을 동기화하는 역할을 한다.
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class M_GameMatchingPanelManager : MonoBehaviourPunCallbacks, IOnEventCallback
    {
        // --- Photon Event Codes ---
        private const byte EVENT_START_GAME = 3;       // 실제 게임 씬 진입 및 시작
        private const byte EVENT_START_COINTOSS_UI = 5; // 코인토스 연출 시작
        private const byte EVENT_START_COUNTDOWN = 6;  // [NET][FIX] [NEW] 매칭 완료 후 3,2,1 카운트다운 시작

        [Header("UI References")]
        [SerializeField] private GameObject matchingPanel;
        [SerializeField] private TextMeshProUGUI matchingText;
        [SerializeField] private TextMeshProUGUI playerListText;
        [SerializeField] private GameObject coinTossPanel;

        [Header("Settings")]
        [SerializeField] private byte maxPlayers = 2;

        private bool isStartRequested = false;
        private bool isGameStarted = false;
        private bool isCoinTossStarted = false;

        private void Awake()
        {
            // 이벤트 콜백 등록
            PhotonNetwork.AddCallbackTarget(this);
        }

        private void OnDestroy()
        {
            // 이벤트 콜백 해제
            PhotonNetwork.RemoveCallbackTarget(this);
        }

        private void Start()
        {
            if (coinTossPanel != null)
                coinTossPanel.SetActive(false);

            // GameManager에 필요한 패널 참조를 자동으로 연결해줍니다.
            M_GameManager gm = FindAnyObjectByType<M_GameManager>();
            if (gm != null)
            {
                if (gm.matchingPanel == null) gm.matchingPanel = matchingPanel;
                if (gm.coinTossPanel == null) gm.coinTossPanel = coinTossPanel;
            }

            UpdateMatchingState();
        }

        public override void OnJoinedRoom()
        {
            UpdateMatchingState();
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            Debug.Log("플레이어 입장: " + newPlayer.NickName);
            UpdateMatchingState();
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            Debug.Log("[GameMatching] 상대방 퇴장 감지: " + otherPlayer.NickName);
            
            // [NET][FIX] 어떤 타이밍에서든 상대가 나가면 모든 진행 중인 프로세스(카운트다운 등) 중단
            StopAllCoroutines(); 
            CancelInvoke(nameof(StartGame));

            isStartRequested = false;
            isGameStarted = false;
            isCoinTossStarted = false;

            // UI 즉시 초기화
            if (coinTossPanel != null) coinTossPanel.SetActive(false);
            if (matchingPanel != null) matchingPanel.SetActive(true);

            // [NET][FIX] 게임 데이터 및 보드 상태 초기화
            M_GameManager gm = FindAnyObjectByType<M_GameManager>();
            if (gm != null) gm.InitializeGame();

            M_BoardManager bm = FindAnyObjectByType<M_BoardManager>();
            if (bm != null) bm.ClearBoard();

            // 코인 매니저 강제 종료
            M_CoinManager cm = FindAnyObjectByType<M_CoinManager>();
            if (cm != null) cm.ForceClosePanel();

            UpdateMatchingState();
        }

        /// <summary>
        /// 방 인원수를 체크하여 매칭 UI를 업데이트하고, 인원이 차면 카운트다운을 시작합니다.
        /// </summary>
        private void UpdateMatchingState()
        {
            if (PhotonNetwork.CurrentRoom == null) return;

            UpdatePlayerListUI();

            // 인원이 부족할 때 (매칭 대기 중)
            if (PhotonNetwork.CurrentRoom.PlayerCount < maxPlayers)
            {
                if (matchingPanel != null) matchingPanel.SetActive(true);
                if (matchingText != null) matchingText.text = "상대를 찾는 중...";
                if (coinTossPanel != null) coinTossPanel.SetActive(false);

                // [NET][FIX] 누군가 나가서 자리가 비었다면 다시 방을 공개하여 매칭이 가능하게 합니다.
                if (PhotonNetwork.IsMasterClient)
                {
                    PhotonNetwork.CurrentRoom.IsOpen = true;
                    PhotonNetwork.CurrentRoom.IsVisible = true;
                    Debug.Log("[GameMatching] 방을 다시 공개 상태로 전환합니다.");
                }
                return;
            }

            // 매칭 완료 상태 (인원 충족)
            if (isCoinTossStarted || isGameStarted) return;
            isCoinTossStarted = true;

            if (matchingPanel != null) matchingPanel.SetActive(true);
            if (matchingText != null) matchingText.text = "매칭 완료!";

            // 마스터 클라이언트가 모든 인원에게 카운트다운 시작 신호를 보냅니다.
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.CurrentRoom.IsOpen = false;
                PhotonNetwork.CurrentRoom.IsVisible = false;

                RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.All };
                PhotonNetwork.RaiseEvent(EVENT_START_COUNTDOWN, null, options, SendOptions.SendReliable);
            }
        }

        /// <summary>
        /// [NET][FIX] [NEW] 모든 클라이언트에서 동시에 실행되는 3-2-1 카운트다운 코루틴
        /// </summary>
        private System.Collections.IEnumerator CountdownRoutine()
        {
            yield return new WaitForSeconds(1f);
            if (matchingText != null) matchingText.text = "매칭 완료!";
            yield return new WaitForSeconds(1f);

            // 3, 2, 1 카운트 진행
            for (int i = 3; i > 0; i--)
            {
                if (matchingText != null) matchingText.text = i.ToString();
                yield return new WaitForSeconds(1f);
            }

            if (matchingText != null) matchingText.text = "시작!";
            yield return new WaitForSeconds(0.5f);

            // 마스터가 코인토스 결과를 결정하여 배포합니다.
            if (PhotonNetwork.IsMasterClient)
            {
                int masterResult = Random.Range(0, 2); 
                RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.All };
                PhotonNetwork.RaiseEvent(EVENT_START_COINTOSS_UI, masterResult, options, SendOptions.SendReliable);
            }
        }

        /// <summary>
        /// 코인토스 연출 패널을 활성화하고 실제 로직을 실행합니다.
        /// </summary>
        private void StartCoinToss(int masterResult)
        {
            if (coinTossPanel != null) coinTossPanel.SetActive(true);
            if (matchingPanel != null) matchingPanel.SetActive(false);

            int myResult;
            if (PhotonNetwork.IsMasterClient)
                myResult = masterResult;
            else
                myResult = (masterResult == 0) ? 1 : 0;

            M_CoinToss coinToss = FindFirstObjectByType<M_CoinToss>();
            if (coinToss != null) coinToss.StartToss(myResult);
        }

        /// <summary>
        /// 현재 방에 있는 플레이어 목록을 텍스트로 표시합니다.
        /// </summary>
        private void UpdatePlayerListUI()
        {
            if (playerListText == null) return;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<b>[Player List]</b>");

            foreach (var player in PhotonNetwork.PlayerList)
            {
                string suffix = player.IsLocal ? " (Me)" : "";
                sb.AppendLine("- " + player.NickName + suffix);
            }

            playerListText.text = sb.ToString();
        }

        /// <summary>
        /// 코인토스 연출이 모두 종료된 후 호출되어 실제 게임 시작을 준비합니다.
        /// </summary>
        public void OnCoinTossFinished()
        {
            if (coinTossPanel != null)
                coinTossPanel.SetActive(false);

            TryStartGame();
        }

        /// <summary>
        /// [D5] 게임 시작 조건(인원수)을 최종 확인하고 시작 요청을 보냅니다.
        /// </summary>
        private void TryStartGame()
        {
            if (PhotonNetwork.CurrentRoom == null) return;

            if (PhotonNetwork.CurrentRoom.PlayerCount == maxPlayers)
            {
                if (PhotonNetwork.IsMasterClient && !isStartRequested)
                {
                    isStartRequested = true;
                    Invoke(nameof(StartGame), 1f);
                }
            }
        }

        private void StartGame()
        {
            RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.All, CachingOption = EventCaching.AddToRoomCache };
            PhotonNetwork.RaiseEvent(EVENT_START_GAME, null, options, SendOptions.SendReliable);
        }

        /// <summary>
        /// Photon 이벤트를 수신하여 처리하는 메인 핸들러
        /// </summary>
        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code == EVENT_START_GAME)
                ProcessStartGame();
            else if (photonEvent.Code == EVENT_START_COINTOSS_UI)
                StartCoinToss((int)photonEvent.CustomData);
            else if (photonEvent.Code == EVENT_START_COUNTDOWN)
                StartCoroutine(CountdownRoutine());
        }

        private void ProcessStartGame()
        {
            if (isGameStarted) return;
            isGameStarted = true;

            if (matchingPanel != null) matchingPanel.SetActive(false);
            if (coinTossPanel != null) coinTossPanel.SetActive(false);
        }
    }
}