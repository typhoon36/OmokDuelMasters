using UnityEngine;
using UnityEngine.Video;
using TMPro; // TextMeshPro 사용을 위해 추가
using UnityEngine.SceneManagement; // ★ 추가됨: 씬 재시작(SceneManager)을 사용하기 위해 필요

public class VideoPanelPlayer : MonoBehaviour
{
    [Header("UI")]
    public GameObject videoPanel;

    [Header("Video Settings")]
    public VideoPlayer videoPlayer;
    
    [Tooltip("승리 시 재생할 영상 클립")]
    public VideoClip winClip;  
    
    [Tooltip("패배 시 재생할 영상 클립")]
    public VideoClip loseClip; 

    [Header("UI Settings")]
    public GameObject resultBox; // 영상을 틀어줄 결과 텍스트 패널
    [Tooltip("승리/패배의 이유를 띄워줄 TextMeshPro 텍스트를 연결하세요.")]
    public TextMeshProUGUI reasonText; // 이유 표시용 텍스트

    private void Awake()
    {
        // 1. 디폴트 값으로 비디오 패널을 닫아둡니다.
        if (videoPanel != null)
            videoPanel.SetActive(false);

        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            
            // 영상이 무한 반복되지 않고 딱 1회만 실행되게 합니다.
            videoPlayer.isLooping = false; 
            
            // 영상이 끝났을 때(1회 컷) 실행할 이벤트 연결
            videoPlayer.loopPointReached += OnVideoFinished; 
        }
    }

    /// <summary>
    /// 승리/패배 여부에 따라 패널을 켜고 영상을 1회 재생합니다.
    /// </summary>
    /// <param name="isWin">true면 승리 영상, false면 패배 영상</param>
    /// <param name="reason">승패의 이유 (선택 사항)</param>
    public void PlayResultVideo(bool isWin, string reason = "")
    {
        // 메인 비디오 패널을 확실하게 켜줍니다.
        if (videoPanel != null)
        {
            videoPanel.SetActive(true);
        }

        if (resultBox != null)
        {
            resultBox.SetActive(true);
        }

        // 이유 텍스트 적용
        if (reasonText != null)
        {
            reasonText.text = reason;
        }

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            
            // 결과에 맞는 클립으로 교체
            videoPlayer.clip = isWin ? winClip : loseClip;
            
            // 영상 재생 (isLooping이 false라 1회만 재생됨)
            videoPlayer.Play();
        }
    }

    /// <summary>
    /// 기존 호환성을 위해 남겨둔 기본 영상 재생 함수
    /// </summary>
    public void PlayVideo()
    {
        if (videoPanel != null) videoPanel.SetActive(true);
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.Play();
        }
    }

    public void SkipVideo()
    {
        if (videoPlayer != null) videoPlayer.Stop();
        if (videoPanel != null) videoPanel.SetActive(false);
    }

    /// <summary>
    /// 영상 1회 재생이 완전히 끝났을 때 자동으로 호출됩니다.
    /// </summary>
    private void OnVideoFinished(VideoPlayer vp)
    {
        // 영상이 끝난 직후 패널을 바로 닫으려면 아래 주석을 푸세요.
        // 현재는 하단 리스타트 버튼을 사용자가 눌러야 하므로 닫지 않고 유지합니다.
        // videoPanel.SetActive(false); 
    }

    // ★ 추가됨: 재시작 버튼의 OnClick() 이벤트 등에 연결할 재시작 함수
    public void RestartScene()
    {
        // 타임스케일이 멈춰있을 수 있으니 원래대로 돌려놓습니다.
        Time.timeScale = 1f;
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
}