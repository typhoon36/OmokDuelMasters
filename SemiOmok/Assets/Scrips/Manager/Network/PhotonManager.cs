using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

/// <summary>
/// Photon 네트워크 테스트용 매니저
/// 현재 단계: SCRUM-26 Photon 연결
/// 이후 SCRUM-27, 29를 순차적으로 확장 예정
/// </summary>
public class PhotonManager : MonoBehaviourPunCallbacks
{
    [Header("Photon Settings")]
    [SerializeField] private string gameVersion = "0.1";
    [SerializeField] private string nickName = "TestPlayer";
    [SerializeField] private bool connectOnStart = true;

    private bool isConnecting = false;
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (connectOnStart)
        {
            Connect();
        }
    }

    /// <summary>
    /// SCRUM-26: Photon 연결
    /// </summary>
    public void Connect()
    {
        // 이미 연결되었거나 연결 시도 중이면 중복 호출 방지
        if (PhotonNetwork.IsConnected || isConnecting)
        {
            Debug.Log("[SCRUM-26] 이미 연결되어 있거나 연결 진행 중입니다.");
            return;
        }

        isConnecting = true;

        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.NickName = string.IsNullOrWhiteSpace(nickName)
            ? $"Player_{Random.Range(1000, 9999)}"
            : nickName;

        // 이후 방 입장/씬 동기화 단계 대비
        PhotonNetwork.AutomaticallySyncScene = true;

        Debug.Log($"[SCRUM-26] Photon 연결 시작 | NickName: {PhotonNetwork.NickName}");
        PhotonNetwork.ConnectUsingSettings();
    }

    /// <summary>
    /// Photon 마스터 서버 연결 성공
    /// </summary>
    public override void OnConnectedToMaster()
    {
        isConnecting = false;
        Debug.Log("[SCRUM-26] Photon 마스터 서버 연결 성공");
    }

    /// <summary>
    /// Photon 연결 해제
    /// </summary>
    public override void OnDisconnected(DisconnectCause cause)
    {
        isConnecting = false;
        Debug.LogWarning($"[SCRUM-26] Photon 연결 해제 | Cause: {cause}");
    }

    /// <summary>
    /// 연결 상태 확인용
    /// </summary>
    public bool IsConnected()
    {
        return PhotonNetwork.IsConnectedAndReady;
    }

    /// <summary>
    /// 현재 닉네임 확인용
    /// </summary>
    public string GetNickName()
    {
        return PhotonNetwork.NickName;
    }
}