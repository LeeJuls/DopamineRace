using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 배팅 화면 장식 고양이 1마리. Animator 미사용 — 코드로 스프라이트 시트 프레임 순환.
/// 폴리곤 영역(CatAreaData) 안에서 Moving(Walk)/Pausing(Idle) 2단계 배회.
/// 방향 전환은 SpriteRenderer.flipX (pivot(0,0) 텔레포트 회피).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class CatController : MonoBehaviour
{
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private float frameInterval = 0.1f;

    private SpriteRenderer sr;
    private CatAreaData area;
    private Coroutine wanderCo;
    private Coroutine animCo;
    private Sprite[] curFrames;

    // ── 프리팩토리/매니저 주입 ──
    public void SetFrames(Sprite[] walk, Sprite[] idle) { walkFrames = walk; idleFrames = idle; }
    public void SetFrameInterval(float v) { frameInterval = Mathf.Max(0.02f, v); }
    public void SetArea(CatAreaData a) { area = a; }

    // 고양이 클릭 → ExchangeModal 열기 (CatDecorationManager가 구독)
    public event System.Action OnClicked;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        // BoxCollider(3D) — OnMouseDown 수신용 (isTrigger, 물리 영향 없음)
        if (GetComponent<Collider>() == null)
        {
            var col = gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(0.5f, 0.5f, 1f);
        }
    }

    private void OnMouseDown()
    {
        // UI(모달 백드롭 등)가 포인터 위면 월드 클릭 무시 — UGUI를 뚫지 않도록
        // (TitleSceneManager.IsPointerOverUI 와 동일 패턴: null 가드 + 마우스 + 터치)
        var es = EventSystem.current;
        if (es != null)
        {
            if (es.IsPointerOverGameObject()) return;                           // 마우스(pointerId=-1)
            if (Input.touchCount > 0 &&
                es.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) return; // 터치
        }
        OnClicked?.Invoke();
    }

    /// <summary>SetActive(true) 시 area가 있으면 자동 배회 재개 (풀링 대응).</summary>
    private void OnEnable() { if (area != null) BeginWander(area); }
    private void OnDisable() { StopAllLocal(); }

    /// <summary>배회 시작. 영역 안 랜덤 위치로 이동 후 루프.</summary>
    public void BeginWander(CatAreaData a)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        area = a;
        StopAllLocal();
        curFrames = PickIdle();
        animCo = StartCoroutine(AnimLoop());
        wanderCo = StartCoroutine(WanderLoop());
    }

    public void StopWander() { StopAllLocal(); }

    private void StopAllLocal()
    {
        if (wanderCo != null) { StopCoroutine(wanderCo); wanderCo = null; }
        if (animCo != null) { StopCoroutine(animCo); animCo = null; }
    }

    private Sprite[] PickWalk() => (walkFrames != null && walkFrames.Length > 0) ? walkFrames : idleFrames;
    private Sprite[] PickIdle() => (idleFrames != null && idleFrames.Length > 0) ? idleFrames : walkFrames;

    // ── 프레임 순환 애니 ──
    private IEnumerator AnimLoop()
    {
        int i = 0;
        while (true)
        {
            var f = curFrames;
            if (f != null && f.Length > 0 && sr != null)
            {
                sr.sprite = f[i % f.Length];
                i++;
            }
            yield return new WaitForSeconds(Mathf.Max(0.02f, frameInterval));
        }
    }

    // ── 폴리곤 배회 (Moving 65% / Pausing 35%) ──
    private IEnumerator WanderLoop()
    {
        if (area != null)
        {
            var sp = area.RandomPointInside();
            transform.position = new Vector3(sp.x, sp.y, 0f);
        }

        while (true)
        {
            if (area != null && Random.value < 0.65f)
            {
                // Moving → Walk
                curFrames = PickWalk();
                var t = area.RandomPointInside();
                Vector3 target = new Vector3(t.x, t.y, 0f);
                float speed = Random.Range(0.8f, 2.0f);
                if (sr != null) sr.flipX = (target.x < transform.position.x); // 기본 우향 가정 (Play 확인)

                while (Vector3.Distance(transform.position, target) > 0.05f)
                {
                    transform.position = Vector3.MoveTowards(
                        transform.position, target, speed * Time.deltaTime);
                    yield return null;
                }
            }
            else
            {
                // Pausing → Idle
                curFrames = PickIdle();
                yield return new WaitForSeconds(Random.Range(0.6f, 2.0f));
            }
        }
    }
}
