using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace Manager.Network
{
    public class NetworkTurnManager : MonoBehaviourPunCallbacks, IOnEventCallback
    {
        public static NetworkTurnManager Instance { get; private set; }

        private const byte EVENT_PLACE_STONE = 4; // 기존 (미사용 예정 또는 하위 호환)
        private const byte EVENT_REQUEST_PLACE_STONE = 10;
        private const byte EVENT_CONFIRM_PLACE_STONE = 11;
        private const byte EVENT_SYNC_BOARD_STATE = 15; // [NET][FIX] 보드 상태 동기화 이벤트

        private M_GameManager gameManager;
        private M_BoardManager boardManager;

        private void Awake()
        {
            Instance = this;
            PhotonNetwork.AddCallbackTarget(this);
        }

        private void Start()
        {
            gameManager = FindFirstObjectByType<M_GameManager>();
            boardManager = FindFirstObjectByType<M_BoardManager>();

            Debug.Log($"[NetworkTurnManager] Start 실행됨 / InRoom={PhotonNetwork.InRoom}");

            if (gameManager == null)
                Debug.LogError("[NetworkTurnManager] GameManager를 찾을 수 없습니다.");

            if (boardManager == null)
                Debug.LogError("[NetworkTurnManager] BoardManager를 찾을 수 없습니다.");

            SyncColorFromRoomPlayers();
        }

        public void OnDestroy()
        {
            PhotonNetwork.RemoveCallbackTarget(this);

            if (Instance == this)
                Instance = null;
        }

        /// =========================
        /// [MULTI FIX] 로컬 착수 이벤트 직접 송신
        /// OnStonePlacedLocal 이벤트 누락 문제 해결
        /// =========================
        
        /// <summary>
        /// [NET] 마스터 클라이언트에게 착수 요청을 보냅니다.
        /// </summary>
        public void RequestStonePlacement(int x, int y)
        {
            if (gameManager == null) gameManager = FindFirstObjectByType<M_GameManager>();
            if (!PhotonNetwork.InRoom || gameManager == null) return;

            int colorCode = gameManager.localPlayer == M_GameManager.Player.Black ? 1 : 2;
            int[] content = { x, y, colorCode };

            // 마스터 클라이언트에게만 요청 전송
            RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
            PhotonNetwork.RaiseEvent(EVENT_REQUEST_PLACE_STONE, content, options, SendOptions.SendReliable);

            Debug.Log($"[NetworkTurnManager] 착수 요청 송신 (To Master): ({x}, {y})");
        }

        /// <summary>
        /// [NET] 마스터 클라이언트가 검증된 착수를 모든 플레이어에게 방송합니다.
        /// </summary>
        private void BroadcastConfirmStone(int x, int y, int colorCode)
        {
            int[] content = { x, y, colorCode };
            RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(EVENT_CONFIRM_PLACE_STONE, content, options, SendOptions.SendReliable);
        }
        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            // [NET][FIX] 플레이어 입장 시 마스터가 현재 보드 상태를 동기화해줍니다.
            Debug.Log($"[NetworkTurnManager] 플레이어 입장: {newPlayer.NickName}. 마스터라면 보드 상태를 동기화합니다.");
            
            if (PhotonNetwork.IsMasterClient)
            {
                SyncBoardStateToPlayer(newPlayer);
            }
        }

        private void SyncBoardStateToPlayer(Player targetPlayer)
        {
            if (gameManager == null) return;

            // 15x15 보드를 1차원 배열로 변환 (0: None, 1: Black, 2: White)
            byte[] boardData = new byte[gameManager.boardSize * gameManager.boardSize];
            for (int i = 0; i < gameManager.boardSize; i++)
            {
                for (int j = 0; j < gameManager.boardSize; j++)
                {
                    M_GameManager.Player state = gameManager.GetCellState(i, j);
                    boardData[i * gameManager.boardSize + j] = (byte)state;
                }
            }

            // 현재 턴 정보도 포함하여 전송 (데이터 패킹)
            object[] syncContent = new object[] 
            { 
                boardData, 
                (byte)gameManager.currentPlayer,
                (byte)gameManager.localPlayer // 참고용
            };

            RaiseEventOptions options = new RaiseEventOptions { TargetActors = new int[] { targetPlayer.ActorNumber } };
            PhotonNetwork.RaiseEvent(EVENT_SYNC_BOARD_STATE, syncContent, options, SendOptions.SendReliable);
            
            Debug.Log($"[NetworkTurnManager] 플레이어 {targetPlayer.NickName}에게 보드 상태 전송 완료");
        }

        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code == EVENT_REQUEST_PLACE_STONE)
            {
                HandleRequestEvent(photonEvent);
            }
            else if (photonEvent.Code == EVENT_CONFIRM_PLACE_STONE)
            {
                HandleConfirmEvent(photonEvent);
            }
            else if (photonEvent.Code == EVENT_SYNC_BOARD_STATE)
            {
                HandleSyncBoardEvent(photonEvent);
            }
            else if (photonEvent.Code == EVENT_PLACE_STONE)
            {
                // 기존 레거시 대응 (필요 시)
                HandleConfirmEvent(photonEvent);
            }
        }

        private void HandleRequestEvent(EventData photonEvent)
        {
            // 마스터 클라이언트만 요청을 처리함
            if (!PhotonNetwork.IsMasterClient) return;

            int[] data = photonEvent.CustomData as int[];
            if (data == null || data.Length < 3) return;

            int x = data[0];
            int y = data[1];
            int colorCode = data[2];
            M_GameManager.Player requestColor = colorCode == 1 ? M_GameManager.Player.Black : M_GameManager.Player.White;

            Debug.Log($"[NetworkTurnManager] 마스터: 착수 요청 수신 ({x}, {y}) from Color {requestColor}");

            // 검증: 현재 턴과 일치하는지, 이미 돌이 있는지 등은 GameManager.PlaceStone 내부에서 수행됨
            // 단, Master의 GameManager 상태가 기준이 됨
            if (gameManager != null)
            {
                
                // 금수 및 턴 체크만 수행 (실제 착수는 Confirm에서)
                if (gameManager.currentPlayer == requestColor && 
                    gameManager.GetCellState(x, y) == M_GameManager.Player.None &&
                    !gameManager.isGameOver)
                {
                    BroadcastConfirmStone(x, y, colorCode);
                }
                else
                {
                    Debug.LogWarning($"[NetworkTurnManager] 마스터: 유효하지 않은 착수 요청 기각 ({x}, {y})");
                }
            }
        }

        private void HandleSyncBoardEvent(EventData photonEvent)
        {
            // [NET][FIX] 마스터로부터 전달받은 보드 데이터를 로컬에 적용합니다.
            object[] data = photonEvent.CustomData as object[];
            if (data == null || data.Length < 2) return;

            byte[] boardData = (byte[])data[0];
            M_GameManager.Player nextTurn = (M_GameManager.Player)(byte)data[1];

            Debug.Log($"[NetworkTurnManager] 보드 상태 동기화 수신. 턴: {nextTurn}");

            if (gameManager != null && boardManager != null)
            {
                // 보드 데이터 복구
                for (int i = 0; i < gameManager.boardSize; i++)
                {
                    for (int j = 0; j < gameManager.boardSize; j++)
                    {
                        M_GameManager.Player stoneColor = (M_GameManager.Player)boardData[i * gameManager.boardSize + j];
                        if (stoneColor != M_GameManager.Player.None)
                        {
                            // 이미 돌이 있는 자리는 무시하고 없는 자리에만 배치 (또는 전체 초기화 후 배치)
                            if (gameManager.GetCellState(i, j) == M_GameManager.Player.None)
                            {
                                boardManager.PlaceStoneFromNetwork(i, j, stoneColor);
                            }
                        }
                    }
                }
                
                // 턴 상태 동기화 (필요 시)
                // gameManager.SetCurrentTurn(nextTurn); // GameManager에 해당 메서드가 있다면 호출
            }
        }

        private void HandleConfirmEvent(EventData photonEvent)
        {
            if (boardManager == null) boardManager = FindFirstObjectByType<M_BoardManager>();
            int[] data = photonEvent.CustomData as int[];
            if (data == null || data.Length < 3) return;

            int x = data[0];
            int y = data[1];
            int colorCode = data[2];
            M_GameManager.Player confirmedColor = colorCode == 1 ? M_GameManager.Player.Black : M_GameManager.Player.White;

            Debug.Log($"[NetworkTurnManager] 착수 확정 수신: ({x}, {y}), color={confirmedColor}");

            // 모든 클라이언트에서 동일하게 착수 실행
            boardManager.PlaceStoneFromNetwork(x, y, confirmedColor);
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            if (!changedProps.ContainsKey("StoneColor")) return;

            if (gameManager == null)
                gameManager = FindFirstObjectByType<M_GameManager>();

            int colorCode = (int)changedProps["StoneColor"];

            M_GameManager.Player selectedColor =
                colorCode == 1 ? M_GameManager.Player.Black : M_GameManager.Player.White;

            if (targetPlayer.IsLocal)
            {
                gameManager.localPlayer = selectedColor;
                Debug.Log($"[NetworkTurnManager] 내가 선택한 색상 적용: {gameManager.localPlayer}");
            }
            else
            {
                gameManager.localPlayer =
                    selectedColor == M_GameManager.Player.Black
                        ? M_GameManager.Player.White
                        : M_GameManager.Player.Black;

                Debug.Log($"[NetworkTurnManager] 상대가 {selectedColor} 선택 → 나는 {gameManager.localPlayer}");

                // [NET][FIX] [MULTI FIX] 선택권이 없던 플레이어에게 상대의 선택 결과를 알려줍니다.
                // 빌드 환경에서 비활성화된 오브젝트도 찾을 수 있도록 FindAnyObjectByType 사용
                M_CoinManager coinManager = FindAnyObjectByType<M_CoinManager>(FindObjectsInactive.Include);
                if (coinManager != null)
                {
                    coinManager.ShowRemoteSelectionResult(selectedColor);
                }
            }
        }

        private void SyncColorFromRoomPlayers()
        {
            if (!PhotonNetwork.InRoom || gameManager == null) return;

            foreach (Player player in PhotonNetwork.PlayerList)
            {
                //커스텀 프로퍼티에 색상 정보가 없는 경우는 건너뜁니다.
                if (!player.CustomProperties.ContainsKey("StoneColor")) continue;

                //커스텀 프로퍼티에서 색상 정보를 가져옵니다.
                int colorCode = (int)player.CustomProperties["StoneColor"];

                //색상 코드를 M_GameManager.Player 열거형으로 변환합니다.
                M_GameManager.Player selectedColor = colorCode == 1 ? M_GameManager.Player.Black : M_GameManager.Player.White;

                /// 로컬 플레이어와 상대 플레이어를 구분하여 색상 정보를 동기화합니다.
                //각 플레이어의 색상 정보를 로그로 출력합니다.
                if (player.IsLocal)
                {
                    gameManager.localPlayer = selectedColor;
                    Debug.Log($"[NetworkTurnManager] 기존 내 색상 동기화: {gameManager.localPlayer}");
                }
                //상대 플레이어의 색상 정보를 기반으로 내 색상을 결정합니다.
                else
                {
                    gameManager.localPlayer = selectedColor == M_GameManager.Player.Black
                            ? M_GameManager.Player.White
                            : M_GameManager.Player.Black;

                    Debug.Log($"[NetworkTurnManager] 기존 상대 색상 기준 동기화: 상대={selectedColor}, 나={gameManager.localPlayer}");
                }
            }
        }
    }
}