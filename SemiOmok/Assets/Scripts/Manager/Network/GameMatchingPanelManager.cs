using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using TMPro;

namespace Manager.Network
{
    /// <summary>
    /// GameScene에서 매칭 패널을 관리하는 스크립트
    /// </summary>
    public class GameMatchingPanelManager : MonoBehaviourPunCallbacks
    {
        [Header("UI References")]
        [SerializeField] private GameObject matchingPanel;
        [SerializeField] private TextMeshProUGUI matchingText;

        [Header("Settings")]
        [SerializeField] private byte maxPlayers = 2;

        private void Start()
        {
            CheckPlayerCountAndUI();
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            Debug.Log($"[GameMatching] 플레이어 입장: {newPlayer.NickName}");
            CheckPlayerCountAndUI();
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            Debug.Log($"[GameMatching] 플레이어 퇴장: {otherPlayer.NickName}");
            CheckPlayerCountAndUI();
        }

        private void CheckPlayerCountAndUI()
        {
            if (PhotonNetwork.CurrentRoom == null) return;

            if (PhotonNetwork.CurrentRoom.PlayerCount < maxPlayers)
            {
                // 인원이 부족하면 패널을 켬
                if (matchingPanel != null) matchingPanel.SetActive(true);
                if (matchingText != null) matchingText.text = "상대를 찾고 있습니다...";
            }
            else
            {
                // 인원이 꽉 차면 패널을 끄고 게임 시작
                if (matchingPanel != null) matchingPanel.SetActive(false);
                Debug.Log("[GameMatching] 2명 매칭 완료! 게임을 시작합니다.");
                
                // 필요하다면 방을 닫아 더 이상 난입하지 못하게 설정
                if (PhotonNetwork.IsMasterClient)
                {
                    PhotonNetwork.CurrentRoom.IsOpen = false;
                    PhotonNetwork.CurrentRoom.IsVisible = false;
                }
                
                // TODO: 실제 게임 시작 로직 호출 (GameManager.StartGame() 등)
            }
        }
    }
}
