using UnityEngine;

public class follow : MonoBehaviour
{
    [Header("Follow Settings")]
    public float speed = 8f;

    [Header("Y Clamp")]
    public float minY = -100f;
    public float maxY = -3f;

    private void FixedUpdate()
    {
        GameObject player = GameObject.FindGameObjectWithTag("MainCamera");
        if (player == null) return;

        float targetY = Mathf.Clamp(player.transform.position.y, minY, maxY);

        Vector3 target = new Vector3(
            player.transform.position.x,
            targetY + 2,
            transform.position.z
        );

        transform.position = Vector3.Lerp(
            transform.position,
            target,
            speed * Time.fixedDeltaTime
        );

        // Lerp 이후에도 한 번 더 클램프해서 범위를 절대 벗어나지 않게 보장
        Vector3 p = transform.position;
        p.y = Mathf.Clamp(p.y, minY, maxY);
        transform.position = p;
    }
}
