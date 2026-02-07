using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;       // 따라갈 플레이어
    public float smoothSpeed = 0.125f; // 카메라 이동 부드러움 정도
    public Vector3 offset;         // 카메라 오프셋 (Z는 보통 -10으로 고정)

    private void FixedUpdate()
    {
        if (target == null) return;

        // 목표 위치 (플레이어의 X, Y만 따라감, Z는 그대로 둠)
        Vector3 desiredPosition = new Vector3(
            target.position.x + offset.x,
            target.position.y + offset.y,
            transform.position.z // Z는 카메라 고정값 유지
        );

        // 부드럽게 이동
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }
}
