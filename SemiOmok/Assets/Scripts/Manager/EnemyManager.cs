using System.Collections;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [Header("랜덤 대기 시간 설정 (회전 전)")]
    public float minRandomTime = 2f; // 최소 대기 시간
    public float maxRandomTime = 5f; // 최대 대기 시간

    [Header("회전 유지 시간 설정 (원상복귀 전)")]
    public float returnDelay = 3f; // 180도 회전 후 머무르는 시간

    private Quaternion originalRotation;

    void Start()
    {
        // 시작할 때의 초기 회전값을 저장해 둡니다.
        originalRotation = transform.rotation;

        // 시퀀스 코루틴 시작
        StartCoroutine(EnemySequence());
    }

    private IEnumerator EnemySequence()
    {
        // 무한루프를 돌며 시퀀스를 반복합니다.
        while (true)
        {
            // 1. 인스펙터에서 설정한 범위 내에서 랜덤 시간 설정 후 대기
            float randomWaitTime = Random.Range(minRandomTime, maxRandomTime);
            yield return new WaitForSeconds(randomWaitTime);

            // 2. Y축으로 180도 회전 (순간 회전)
            transform.rotation = originalRotation * Quaternion.Euler(0, 0, 180f);

            // 3. 인스펙터에서 지정한 일정 시간(returnDelay) 동안 대기
            yield return new WaitForSeconds(returnDelay);

            // 4. 원래 방향으로 원상 복귀
            transform.rotation = originalRotation;

            // 5. 5초 대기 후 시퀀스 재시작
            yield return new WaitForSeconds(5f);
        }
    }
}
