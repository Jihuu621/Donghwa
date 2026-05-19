using UnityEngine;

public class Parallax : MonoBehaviour
{
    private float length, startPos;
    public GameObject cam;           // 메인 카메라를 연결할 변수
    public float parallaxEffect;     // 카메라 대비 이동 속도 비율 (0 ~ 1 소수점)

    void Start()
    {
        startPos = transform.position.x;
        // Sprite Renderer의 가로 크기를 가져옴
        length = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    void FixedUpdate()
    {
        // 카메라가 움직인 거리 계산
        float dist = (cam.transform.position.x * parallaxEffect);

        // 배경의 위치를 업데이트
        transform.position = new Vector3(startPos + dist, transform.position.y, transform.position.z);
    }
}