using UnityEngine;

public class CamaraShake : MonoBehaviour
{
    public static CamaraShake Instance { get; private set; }
    public Vector3 shakeOffset { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void Shake()
    {
        CancelInvoke("StartShake");
        CancelInvoke("StopShake");

        InvokeRepeating("StartShake", 0f, 0.02f);
        Invoke("StopShake", 0.2f);
    }

    void StartShake()
    {
        // insideUnitCircle을 사용하여 X, Y만 흔들리게 하고 Z는 0으로 고정합니다.
        Vector2 shakeCircle = Random.insideUnitCircle * 0.15f;
        shakeOffset = new Vector3(shakeCircle.x, shakeCircle.y, 0f);
    }

    void StopShake()
    {
        CancelInvoke("StartShake");
        shakeOffset = Vector3.zero;
    }
}