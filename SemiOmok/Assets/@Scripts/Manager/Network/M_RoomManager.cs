using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Manager.Network
{
    public class RoomManager : MonoBehaviourPunCallbacks
    {
        [Header("Room Settings")]
        [SerializeField] private byte maxPlayers = 2;
        [SerializeField] private string multiSceneName = "MultiPlayer";
        [SerializeField] private string singleSceneName = "Single";

        [Header("UI References")]
        [SerializeField] private Button onlineMatchBtn;
        [SerializeField] private Button singleMatchBtn;

        public static RoomManager Instance { get; private set; }

        private bool isMatching = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            PhotonNetwork.AutomaticallySyncScene = true;
        }

        private void Start()
        {
            if (onlineMatchBtn != null)
                onlineMatchBtn.onClick.AddListener(StartMatch);

            if (singleMatchBtn != null)
                singleMatchBtn.onClick.AddListener(StartSinglePlayer);
        }

        public void StartSinglePlayer()
        {
            isMatching = false;
            Debug.Log("[RoomManager] 싱글 플레이 시작 - 씬 로드");
            
            // 방에 입장해 있다면 퇴장 후 로드 (또는 그냥 로드)
            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom();
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene(singleSceneName);
        }

        public void StartMatch()
        {
            isMatching = true;

            if (!PhotonNetwork.InLobby)
            {
                Debug.LogWarning("[SCRUM-28] 로비에 아직 접속되지 않았습니다. 접속 완료 시 자동으로 매칭을 시작합니다.");
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
            if (!PhotonNetwork.IsConnectedAndReady)
            {
                Debug.LogWarning("[SCRUM-28] 서버에 연결되지 않아 방을 생성할 수 없습니다.");
                return;
            }

            RoomOptions roomOptions = new RoomOptions
            {
                MaxPlayers = maxPlayers,
                IsVisible = true,
                IsOpen = true
            };

            Debug.Log($"[SCRUM-28] 방 생성 시도: {roomName}");
            PhotonNetwork.CreateRoom(roomName, roomOptions);
        }

        public void JoinRandomRoom()
        {
            if (!PhotonNetwork.InLobby && PhotonNetwork.Server != ServerConnection.MasterServer)
            {
                Debug.LogWarning("[SCRUM-28] Master Server 또는 로비에 연결되지 않아 방에 참가할 수 없습니다.");
                return;
            }

            Debug.Log("[SCRUM-28] 랜덤 방 참가 시도 (MaxPlayers: " + maxPlayers + ")");
            // [NET][FIX] 인원수 제한을 명시하여 매칭 정확도를 높입니다.
            PhotonNetwork.JoinRandomRoom(null, maxPlayers);
        }

        public override void OnJoinedLobby()
        {
            if (isMatching && !PhotonNetwork.InRoom)
            {
                JoinRandomRoom();
            }
        }

        public override void OnCreatedRoom()
        {
            Debug.Log($"[SCRUM-28] 방 생성 성공: {PhotonNetwork.CurrentRoom.Name}");
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.LogError($"[SCRUM-28] 방 생성 실패: {message} (Code: {returnCode})");
        }

        public override void OnJoinedRoom()
        {
            Debug.Log($"[SCRUM-28] 방 입장 성공: {PhotonNetwork.CurrentRoom.Name}");
            // 방에 입장하자마자 멀티플레이 전용 씬으로 이동
            PhotonNetwork.LoadLevel(multiSceneName);
        }

        private int joinRetryCount = 0;
        private const int MAX_JOIN_RETRIES = 2;

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            // [NET][FIX] 엇갈림 방지: 즉시 생성하지 않고 잠시 후 다시 조인 시도
            if (joinRetryCount < MAX_JOIN_RETRIES)
            {
                joinRetryCount++;
                float randomDelay = Random.Range(0.5f, 2.0f); // [NET][FIX] 재시도 시간을 무작위로 설정하여 클라이언트 간 충돌 방지
                Debug.Log($"[SCRUM-28] 랜덤 방 참가 실패. {randomDelay:F1}초 후 재시도 중... ({joinRetryCount}/{MAX_JOIN_RETRIES})");
                Invoke(nameof(JoinRandomRoom), randomDelay); 
            }
            else
            {
                joinRetryCount = 0;
                Debug.LogWarning("[SCRUM-28] 여러 번의 시도 후에도 방이 없어 새로운 방을 생성합니다.");
                CreateRoom($"Room_{Random.Range(1000, 9999)}");
            }
        }

        public override void OnLeftRoom()
        {
            isMatching = false;
            Debug.Log("[SCRUM-28] 방에서 퇴장했습니다.");
        }
    }
}