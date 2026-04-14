using UnityEngine;

public class Sliceable : MonoBehaviour
{
    // 무한 절단을 위해 원본 이미지의 영역 정보를 저장하는 변수들입니다.
    [HideInInspector] public Rect originalRect;
    [HideInInspector] public Bounds originalBounds;
}