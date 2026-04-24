using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using TMPro;
using System.Text;

namespace Manager.Network
{
    /// <summary>
    /// [D4] 매칭 상태 동기화 관리 스크립트
    /// </summary>
    public class GameMatchingPanelManager : MonoBehaviourPunCallbacks
    {
        [Header("UI References")]
        [SerializeField] private GameObject matchingPanel;
        [SerializeField] private TextMeshProUGUI matchingText;
        [SerializeField] private TextMeshProUGUI playerListText; // 디버그용 플레이어 리스트 텍스트

        [Header("Settings")]
        [SerializeField] private byte maxPlayers = 2;

        private bool isGameStarted = false; // 게임 시작 중복 방지용 플래그

        private void Start()
        {
            UpdateMatchingState();
        }

        public override void OnJoinedRoom()
        {
            Debug.Log("[GameMatching] OnJoinedRoom 호출됨");
            UpdateMatchingState();
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            Debug.Log($"[GameMatching] 플레이어 입장: {newPlayer.NickName}");
            UpdateMatchingState();
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            Debug.Log($"[GameMatching] 플레이어 퇴장: {otherPlayer.NickName}");
            
            // 플레이어가 나가면 게임 시작 상태 초기화 및 딜레이 실행 취소
            isGameStarted = false;
            CancelInvoke(nameof(StartGame));

            UpdateMatchingState();
        }

        /// <summary>
        /// 방에 입장한 플레이어 수를 기준으로 매칭 상태와 UI를 갱신하는 공통 함수
        /// </summary>
        private void UpdateMatchingState()
        {
            if (PhotonNetwork.CurrentRoom == null) return;

            UpdatePlayerListUI();

            if (PhotonNetwork.CurrentRoom.PlayerCount < maxPlayers)
            {
                if (matchingPanel != null) matchingPanel.SetActive(true);
                if (matchingText != null) matchingText.text = "상대를 찾는 중...";
            }
            else
            {
                if (matchingPanel != null) matchingPanel.SetActive(true);
                if (matchingText != null) matchingText.text = "매칭 완료!";
                Debug.Log("[GameMatching] 2명 매칭 완료! 게임을 시작합니다.");
                
                if (PhotonNetwork.IsMasterClient)
                {
                    PhotonNetwork.CurrentRoom.IsOpen = false;
                    PhotonNetwork.CurrentRoom.IsVisible = false;
                }
                
                TryStartGame();
            }
        }

        /// <summary>
        /// 테스트용 현재 방의 플레이어 닉네임 리스트 갱신 함수
        /// </summary>
        private void UpdatePlayerListUI()
        {
            if (playerListText == null) return;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<b>[Player List]</b>");

            foreach (var player in PhotonNetwork.PlayerList)
            {
                sb.AppendLine($"- {player.NickName} {(player.IsLocal ? "(Me)" : "")}");
            }

            playerListText.text = sb.ToString();
        }

        /// <summary>
        /// [D5] 게임 시작 조건 처리
        /// </summary>
        private void TryStartGame()
        {
            if (PhotonNetwork.CurrentRoom.PlayerCount == maxPlayers)
            {
                if (PhotonNetwork.IsMasterClient && !isGameStarted)
                {
                    isGameStarted = true;
                    Debug.Log("[GameMatching] 1초 후 게임을 시작합니다.");
                    Invoke(nameof(StartGame), 1f);
                }
            }
        }

        /// <summary>
        /// 실제 게임 시작 실행 (씬 이동 없이 로그 출력)
        /// </summary>
        private void StartGame()
        {
            Debug.Log("[GameMatching] 게임 시작!");
        }
    }
}
