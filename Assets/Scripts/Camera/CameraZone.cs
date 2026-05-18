using Cinemachine;
using UnityEngine;

public class CameraZone : MonoBehaviour
{
    [Header("이 구역에서 사용할 카메라")]
    public CinemachineVirtualCamera zoneCamera;

    [Header("우선순위 설정")]
    public int activePriority = 20;
    public int inactivePriority = 10;

    private void Awake()
    {
        if (zoneCamera == null) zoneCamera = GetComponentInChildren<CinemachineVirtualCamera>();
        // 시작할 때 이 구역 카메라는 기본 우선순위로 설정
        if (zoneCamera != null) { 
            Debug.Log(transform.name);
            zoneCamera.Priority = inactivePriority;
        }
            


    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어가 들어오면 해당 구역 카메라를 활성화
        if (other.CompareTag("Player") && zoneCamera != null)
        {
            Debug.Log(transform.name);
            zoneCamera.Priority = activePriority;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // 플레이어가 나가면 다시 우선순위를 낮춤
        if (other.CompareTag("Player") && zoneCamera != null)
        {
            zoneCamera.Priority = inactivePriority;
        }
    }
}