using UnityEngine;

public class CameraCollisionDetector : MonoBehaviour
{
    // ★ global:: 을 붙여서 Photon 등 다른 네임스페이스의 GameManager와 충돌하는 것을 완벽하게 방지합니다.
    [Tooltip("게임의 전체 상태를 관리하는 GameManager를 연결하세요.")]
    public GameManager gameManager;

    private void OnTriggerEnter(Collider other)
    {
        // 태그가 'sam'인 오브젝트와 닿았을 때
        if (other.CompareTag("Sam"))
        {
            Debug.Log("⚠️ 카메라가 'Sam' 태그를 가진 물체와 충돌했습니다!");

            if (gameManager != null)
            {
                // 스페이스바를 떼서 앞을 바라보고 있는 상태일 때 체력 감소
                if (!gameManager.isSpaceHeld)
                {
                    Debug.Log("💥 판정: 플레이어가 앞을 보고 있어서 데미지(1)를 입었습니다.");
                    gameManager.TakeDamage(1);
                    
                    // 만약 맞은 물체를 사라지게 하고 싶다면 주석을 해제하세요.
                    // Destroy(other.gameObject); 
                }
                else
                {
                    // 스페이스바를 누르고 있어서 고개를 숙인 상태일 때
                    Debug.Log("🛡️ 판정: 플레이어가 스페이스를 누르고 있어서 회피에 성공했습니다!");
                }
            }
            else
            {
                Debug.LogWarning("CameraCollisionDetector에 GameManager가 연결되지 않았습니다!");
            }
        }
    }
}