/**
 * [수정 내역 ]
 * 1. UI 의존성 제거: TitleUIManager가 버튼 클릭을 전담하도록 구조 변경 (RoomManager의 UI 참조 필드 삭제)
 * 2. DontDestroyOnLoad 버그 수정: 부모 오브젝트가 있을 경우 DDOL이 작동하지 않는 문제 해결 (Awake에서 SetParent(null) 추가)
 * 3. 씬 관리 최적화: 싱글/멀티 플레이 진입 시 씬 전환 로직 정립
 */
using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Manager.Network
{
    public class RoomManager : MonoBehaviourPunCallbacks
    {
        [Header("Room Settings")]
        [SerializeField] private byte maxPlayers = 2;
        [SerializeField] private string multiSceneName = "Game_Multiplayer";
        [SerializeField] private string singleSceneName = "Game_Singleplayer"; // 실제 싱글플레이 씬 이름에 맞게 수정 필요



        public static RoomManager Instance { get; private set; }

        private bool isMatching = false;
        private int joinRetryCount = 0;
        private const int MAX_JOIN_RETRIES = 2;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // DontDestroyOnLoad는 루트 오브젝트에서만 작동하므로 부모가 있다면 해제해줍니다.
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }

            DontDestroyOnLoad(gameObject);

            PhotonNetwork.AutomaticallySyncScene = true;
        }



        public void StartSinglePlayer()
        {
            isMatching = false;
            Debug.Log("[RoomManager] 싱글 플레이 시작");

            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom();
            }

            SceneManager.LoadScene(singleSceneName);
        }

        public void StartMatch()
        {
            isMatching = true;

            if (!PhotonNetwork.InLobby)
            {
                if (!PhotonNetwork.IsConnected)
                {
                    PhotonManager.Instance?.ConnectToPhoton();
                }
                return;
            }

            JoinRandomRoom();
        }

        public void CreateRoom(string roomName)
        {
            if (!PhotonNetwork.IsConnectedAndReady) return;

            RoomOptions roomOptions = new RoomOptions
            {
                MaxPlayers = maxPlayers,
                IsVisible = true,
                IsOpen = true
            };

            PhotonNetwork.CreateRoom(roomName, roomOptions);
        }

        public void JoinRandomRoom()
        {
            if (!PhotonNetwork.IsConnectedAndReady) return;

            Debug.Log("[RoomManager] 랜덤 방 참가 시도");
            PhotonNetwork.JoinRandomRoom(null, maxPlayers);
        }

        public override void OnJoinedLobby()
        {
            if (isMatching && !PhotonNetwork.InRoom)
            {
                JoinRandomRoom();
            }
        }

        public override void OnJoinedRoom()
        {
            Debug.Log($"[RoomManager] 방 참가 성공: {PhotonNetwork.CurrentRoom.Name}");
            PhotonNetwork.LoadLevel(multiSceneName);
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            if (joinRetryCount < MAX_JOIN_RETRIES)
            {
                joinRetryCount++;
                float randomDelay = Random.Range(0.5f, 1.5f);
                Invoke(nameof(JoinRandomRoom), randomDelay);
            }
            else
            {
                joinRetryCount = 0;
                CreateRoom($"Room_{Random.Range(1000, 9999)}");
            }
        }

        public override void OnLeftRoom()
        {
            isMatching = false;
            Debug.Log("[RoomManager] 방에서 퇴장했습니다.");
        }
    }
}