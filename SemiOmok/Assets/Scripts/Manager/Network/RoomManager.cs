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
        [SerializeField] private string gameSceneName = "GameScene";

        [Header("UI References")]
        [SerializeField] private Button onlineMatchBtn;

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

            Debug.Log("[SCRUM-28] 랜덤 방 참가 시도");
            PhotonNetwork.JoinRandomRoom();
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
            Debug.Log($"[SCRUM-28] 방 참가 성공: {PhotonNetwork.CurrentRoom.Name}");
            // 방에 입장하자마자 GameScene으로 즉시 이동 (혼자라도 이동)
            PhotonNetwork.LoadLevel(gameSceneName);
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            Debug.LogWarning($"[SCRUM-28] 랜덤 방 참가 실패: {message}. 새로운 방을 생성합니다.");
            CreateRoom($"Room_{Random.Range(1000, 9999)}");
        }

        public override void OnLeftRoom()
        {
            isMatching = false;
            Debug.Log("[SCRUM-28] 방에서 퇴장했습니다.");
        }
    }
}