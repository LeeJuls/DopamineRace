using UnityEngine;

// ══════════════════════════════════════════
//  충돌 흔들림 컴포넌트
// ══════════════════════════════════════════
public class CollisionShake : MonoBehaviour
{
    private float shakeTimer = 0f;
    private float shakeMagnitude = 0.05f;
    private bool isShaking = false;
    private Vector3 totalDrift = Vector3.zero;
    private const float MAX_DRIFT = 0.3f;

    public void StartShake(float duration, float magnitude)
    {
        shakeMagnitude = magnitude;
        shakeTimer = duration;
        isShaking = true;
    }

    private void Update()
    {
        if (!isShaking) return;
        shakeTimer -= Time.deltaTime;
        if (shakeTimer <= 0f)
        {
            isShaking = false;
            return;
        }
        float offsetX = Random.Range(-shakeMagnitude, shakeMagnitude);
        float offsetY = Random.Range(-shakeMagnitude, shakeMagnitude);
        Vector3 frameOffset = new Vector3(offsetX, offsetY, 0f);

        // 누적 드리프트 캡: 위치 저장 없이 오프셋만 추적
        Vector3 newDrift = totalDrift + frameOffset;
        if (newDrift.magnitude > MAX_DRIFT)
        {
            newDrift = newDrift.normalized * MAX_DRIFT;
            frameOffset = newDrift - totalDrift;
        }
        totalDrift = newDrift;
        transform.localPosition += frameOffset;
    }
}
