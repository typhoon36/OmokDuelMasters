using System.Collections;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [Header("게임 매니저 연결")]
    [Tooltip("첫 착수 타이밍을 알기 위해 GameManager를 연결하세요.")]
    public GameManager gameManager;

    [Header("랜덤 대기 시간 설정 (회전 전)")]
    public float minRandomTime = 2f; 
    public float maxRandomTime = 5f; 

    [Header("회전 유지 시간 설정 (원상복귀 전)")]
    public float returnDelay = 3f; 

    [Header("경고 색상 변환 설정")]
    [Tooltip("색상이 변할 선생님의 머티리얼(Material)을 직접 연결하세요.")]
    public Material teacherMaterial; 
    public Color normalColor = Color.white;
    public Color dangerColor = Color.red;

    private Quaternion originalRotation;
    private bool hasStartedSequence = false;
    
    // URP 등에서 Base Map 색상에 접근하기 위한 프로퍼티 ID
    private readonly int baseColorId = Shader.PropertyToID("_BaseColor");

    void Start()
    {
        // 시작할 때의 초기 회전값을 저장해 둡니다.
        originalRotation = transform.rotation;

        // 초기 Base Map 색상을 하얀색(normalColor)으로 맞춰줍니다.
        if (teacherMaterial != null)
        {
            teacherMaterial.SetColor(baseColorId, normalColor);
        }

        // GameManager를 못 찾았다면 자동으로 찾아봅니다.
        if (gameManager == null)
        {
            gameManager = FindAnyObjectByType<GameManager>();
        }

        // 게임 시작 시 동작하지 않고, 돌이 놓일 때 이벤트를 듣도록 대기합니다.
        if (gameManager != null)
        {
            gameManager.OnStonePlaced += HandleFirstStonePlaced;
        }
        else
        {
            // 매니저가 없다면 그냥 바로 시작
            StartCoroutine(EnemySequence());
        }
    }

    /// <summary>
    /// 누군가가 첫 번째 돌을 두었을 때 호출됩니다.
    /// </summary>
    private void HandleFirstStonePlaced(int x, int y, GameManager.Player player)
    {
        if (!hasStartedSequence)
        {
            hasStartedSequence = true;
            
            // 첫 돌이 놓이면 비로소 선생님의 감시 루프가 시작됩니다.
            StartCoroutine(EnemySequence());
            
            // 이후에는 더 이상 이벤트를 들을 필요가 없으므로 구독 해제
            if (gameManager != null)
            {
                gameManager.OnStonePlaced -= HandleFirstStonePlaced;
            }
        }
    }

    private IEnumerator EnemySequence()
    {
        // 무한루프를 돌며 시퀀스를 반복합니다.
        while (true)
        {
            // 1. 인스펙터에서 설정한 범위 내에서 랜덤 대기 시간 설정
            float randomWaitTime = Random.Range(minRandomTime, maxRandomTime);
            
            // 지정된 시간 동안 기다리면서 매 프레임 머티리얼 색상을 붉게 물들입니다.
            float elapsedTime = 0f;
            while (elapsedTime < randomWaitTime)
            {
                elapsedTime += Time.deltaTime;
                float lerpFactor = elapsedTime / randomWaitTime; // 0.0에서 1.0으로 증가

                if (teacherMaterial != null)
                {
                    // 시간이 지날수록 normalColor에서 dangerColor로 자연스럽게 섞입니다.
                    Color lerpedColor = Color.Lerp(normalColor, dangerColor, lerpFactor);
                    teacherMaterial.SetColor(baseColorId, lerpedColor);
                }

                yield return null; // 한 프레임 대기 후 다음 루프 실행
            }

            // 2. Y축으로 180도 회전 (돌아보기)
            transform.rotation = originalRotation * Quaternion.Euler(0, 0, 180f);

            // 3. 인스펙터에서 지정한 일정 시간(returnDelay) 동안 대기 (새빨간 상태 유지)
            yield return new WaitForSeconds(returnDelay);

            // 4. 원래 방향으로 원상 복귀 및 색상을 다시 원래대로 즉시 초기화
            transform.rotation = originalRotation;
            if (teacherMaterial != null)
            {
                teacherMaterial.SetColor(baseColorId, normalColor); 
            }

            // 5. 5초 대기(휴식) 후 시퀀스 재시작
            yield return new WaitForSeconds(5f);
        }
    }

    // ★ 중요: 유니티 에디터 환경에서 플레이를 껐을 때 머티리얼 원본 파일이 빨간색으로 고정되는 것을 막아줍니다.
    private void OnDestroy()
    {
        // 스크립트가 파괴될 때 이벤트 구독 해제 및 색상 원상복구
        if (gameManager != null)
        {
            gameManager.OnStonePlaced -= HandleFirstStonePlaced;
        }

        if (teacherMaterial != null)
        {
            teacherMaterial.SetColor(baseColorId, normalColor);
        }
    }
}
