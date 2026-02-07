using UnityEngine;

public class PlayerSpawnChess : MonoBehaviour
{
    [Header("Prefab & input")]
    public GameObject chessPrefab;
    public KeyCode spawnKey = KeyCode.Q;

    [Header("Spawn placement")]
    public float forwardDistance = 2f;
    public float spawnHeight = 5f;

    [Header("Cooldown")]
    public float cooldown = 3f;
    private float cooldownTimer = 0f;

    [Header("Pooling")]
    private GameObject pooledChess; 
    private bool chessActive = false;

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(spawnKey))
        {
            TrySpawnChess();
        }
    }

    void TrySpawnChess()
    {
        if (cooldownTimer > 0f)
        {
            Debug.Log("쿨 남은시간: " + cooldownTimer.ToString("F1"));
            return;
        }

        if (chessPrefab == null)
        {
            Debug.LogWarning("프리팹없다");
            return;
        }

        if (pooledChess == null)
        {
            pooledChess = Instantiate(chessPrefab);
            pooledChess.SetActive(false);
        }

        if (chessActive)
        {
            Debug.Log("체스말ing");
            return;
        }

        float dir = transform.localScale.x < 0 ? -1f : 1f;

        Vector2 origin = transform.position;
        Vector2 targetPoint = origin + Vector2.right * dir * forwardDistance;
        Vector2 spawnPos = new Vector2(targetPoint.x, targetPoint.y + spawnHeight);
        pooledChess.transform.position = spawnPos;
        pooledChess.SetActive(true);

        Rigidbody2D rb = pooledChess.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 12f;
        }

        Collider2D col = pooledChess.GetComponent<Collider2D>();
        if (col != null)
            col.enabled = true;
        ChessPiece piece = pooledChess.GetComponent<ChessPiece>();
        piece.onDeactivate = OnChessDeactivated;

        chessActive = true;
        cooldownTimer = cooldown;
    }
    void OnChessDeactivated()
    {
        chessActive = false;
    }
}
