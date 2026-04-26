using UnityEngine;
using System.Collections;

/// <summary>
/// RacerController — 워밍업 배회 연출 (SPEC-025)
/// Round 1 Betting 상태에서 화면을 자유롭게 돌아다니는 동작.
/// Moving(60%) / Pausing(25%) / Swaying(15%) 상태머신.
/// </summary>
public partial class RacerController : MonoBehaviour
{
    // ══════════════════════════════════════
    //  배회 상태머신 필드
    // ══════════════════════════════════════

    private enum WanderPhase { Moving, Pausing, Swaying }

    // 외부에서 isWandering 상태 읽기 가능
    public bool IsWandering => _isWandering;

    private bool _isWandering;
    private Coroutine _wanderCoroutine;
    private WanderPhase _wanderPhase;
    private Vector3 _wanderTarget;
    private float _wanderSpeed;       // world units / s
    private Vector3 _wanderBasePos;   // Swaying 진동 기준점
    private Bounds _wanderBounds;

    // ══════════════════════════════════════
    //  공개 API
    // ══════════════════════════════════════

    /// <summary>배회 시작. bounds 안에서 자유롭게 이동.</summary>
    public void StartWandering(Bounds bounds)
    {
        _wanderBounds = bounds;
        _isWandering = true;
        if (_wanderCoroutine != null) StopCoroutine(_wanderCoroutine);
        _wanderCoroutine = StartCoroutine(WanderRoutine());
    }

    /// <summary>배회 중단. 코루틴 정지 + Idle 애니.</summary>
    public void StopWandering()
    {
        if (!_isWandering && _wanderCoroutine == null) return;
        _isWandering = false;
        if (_wanderCoroutine != null)
        {
            StopCoroutine(_wanderCoroutine);
            _wanderCoroutine = null;
        }
        if (animator != null) animator.SetTrigger("Idle");
    }

    /// <summary>
    /// 골인 후 입상 여부에 따른 모션 트리거 (SPEC-026).
    /// trigger 예: "AttackMagic" (입상), "Death" (권외).
    /// </summary>
    public void PlayFinishMotion(string trigger)
    {
        if (animator != null && !string.IsNullOrEmpty(trigger))
            animator.SetTrigger(trigger);
    }

    /// <summary>지정 위치까지 SmoothStep Lerp 이동. 완료 시 onDone 호출.</summary>
    public IEnumerator MoveToPosition(Vector3 target, float duration, System.Action onDone)
    {
        Vector3 start = transform.position;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            if (duration <= 0f) break;
            transform.position = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / duration));
            FlipSprite();
            yield return null;
        }
        transform.position = target;
        lastPosition = target;
        onDone?.Invoke();
    }

    // ══════════════════════════════════════
    //  내부: 배회 코루틴 (무한 루프)
    // ══════════════════════════════════════

    private IEnumerator WanderRoutine()
    {
        while (_isWandering)
        {
            float r = UnityEngine.Random.value;

            if (r < 0.60f)
            {
                // ── Moving: 랜덤 목표로 달려감 ──
                _wanderPhase = WanderPhase.Moving;
                _wanderTarget = new Vector3(
                    UnityEngine.Random.Range(_wanderBounds.min.x, _wanderBounds.max.x),
                    UnityEngine.Random.Range(_wanderBounds.min.y, _wanderBounds.max.y),
                    0f);
                _wanderSpeed = UnityEngine.Random.Range(1.5f, 4.0f);
                if (animator != null) animator.SetTrigger("Run");

                // 목표 도달 또는 배회 중단 시까지 대기
                yield return new WaitUntil(() =>
                    !_isWandering ||
                    Vector3.Distance(transform.position, _wanderTarget) < 0.08f);
            }
            else if (r < 0.85f)
            {
                // ── Pausing: 잠깐 멈춤 ──
                _wanderPhase = WanderPhase.Pausing;
                float pauseTime = UnityEngine.Random.Range(0.5f, 2.0f);
                if (animator != null) animator.SetTrigger("Idle");
                yield return new WaitForSeconds(pauseTime);
            }
            else
            {
                // ── Swaying: 좌우 흔들림 ──
                _wanderPhase = WanderPhase.Swaying;
                float swayDur = UnityEngine.Random.Range(1.0f, 2.5f);
                _wanderBasePos = transform.position;
                if (animator != null) animator.SetTrigger("Idle");
                yield return new WaitForSeconds(swayDur);
            }
        }
    }

    // ══════════════════════════════════════
    //  내부: Update에서 호출되는 배회 이동
    // ══════════════════════════════════════

    private void UpdateWander()
    {
        switch (_wanderPhase)
        {
            case WanderPhase.Moving:
                transform.position = Vector3.MoveTowards(
                    transform.position, _wanderTarget, _wanderSpeed * Time.deltaTime);
                break;

            case WanderPhase.Swaying:
                // 위치 고정 — 좌우 둘러보기는 sprite flip 토글로만 표현
                // (FlipSprite는 lastPosition 기반이므로 transform을 살짝 흔들지 않으면 flip 발생 안 함
                //  → lastPosition을 직접 토글해서 시각적 둘러보기 효과)
                transform.position = _wanderBasePos;
                // 1초 주기로 좌우 방향 토글 (Sin 부호로 결정)
                float lookSign = Mathf.Sign(Mathf.Sin(Time.time * 2f));
                float absScale = Mathf.Abs(transform.localScale.x);
                transform.localScale = new Vector3(
                    lookSign * absScale, transform.localScale.y, transform.localScale.z);
                break;

            case WanderPhase.Pausing:
                // 이동 없음
                break;
        }
    }
}
