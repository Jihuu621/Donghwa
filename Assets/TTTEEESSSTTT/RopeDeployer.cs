using UnityEngine;

public class RopeDeployer : MonoBehaviour
{
    public GameObject linkPrefab; // 위에서 만든 마디 프리팹
    public Rigidbody2D startPoint; // 연결 시작점 (플랫폼 A)
    public Rigidbody2D endPoint;   // 연결 끝점 (플랫폼 B)
    public int linkCount = 5;      // 마디 개수

    void Start()
    {
        GenerateRope();
    }

    void GenerateRope()
    {
        Rigidbody2D lastChain = startPoint;

        for (int i = 0; i < linkCount; i++)
        {
            GameObject link = Instantiate(linkPrefab, transform);
            HingeJoint2D joint = link.GetComponent<HingeJoint2D>();
            joint.connectedBody = lastChain;

            if (i < linkCount - 1)
            {
                lastChain = link.GetComponent<Rigidbody2D>();
            }
            else
            {
                // 마지막 마디를 끝점(플랫폼 B)에 연결
                HingeJoint2D endJoint = endPoint.gameObject.AddComponent<HingeJoint2D>();
                endJoint.connectedBody = link.GetComponent<Rigidbody2D>();
                // 끝점의 앵커 위치를 조절해야 할 수 있습니다.
            }
        }
    }
}