using System.Collections;
using UnityEngine;

public class PlayerPlatform : MonoBehaviour
{
    [SerializeField] private float disableTime = 0.4f;
    private Collider2D playerCollider;
    private GameObject currentPlatform; 

    void Awake()
    {
        playerCollider = GetComponent<Collider2D>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S) && currentPlatform != null)
        {
            if (currentPlatform.GetComponent<PlatformEffector2D>() != null)
            {
                StartCoroutine(DisableCollision());
            }
        }
    }

    private IEnumerator DisableCollision()
    {
        Collider2D platformCollider = currentPlatform.GetComponent<Collider2D>();

        Physics2D.IgnoreCollision(playerCollider, platformCollider, true);
        yield return new WaitForSeconds(disableTime);

        if (platformCollider != null)
        {
            Physics2D.IgnoreCollision(playerCollider, platformCollider, false);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            currentPlatform = collision.gameObject;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject == currentPlatform)
        {
            currentPlatform = null;
        }
    }
}