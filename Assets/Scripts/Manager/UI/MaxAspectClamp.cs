using UnityEngine;

/// <summary>
/// 화면비 상한 클램프 — 부모(풀스크린 컨테이너)가 16:9보다 넓어질 때만 좌우 대칭 인셋을 적용해
/// 자기 RectTransform을 중앙 16:9 영역으로 가둔다(레터박스/필러박스).
///
/// - 16:9 이하(1080p·1440p·4K·16:10 등): inset=0 → 무동작(일반 모니터 무손상).
/// - 21:9+ 울트라와이드: 좌우 인셋 → 안의 모든 자식이 1920×1080 기준 레이아웃 그대로 렌더.
///
/// 런타임 전용(AddComponent로 부착). 프리팹에 베이크하면 환경의존 직렬 오염 + missing-script 위험이라
/// <see cref="SceneBootstrapper"/>의 패널 Instantiate 직후 코드에서 부착한다. 화면 중립 — Title/Racing/Result
/// 등 동일한 높이우선 CanvasScaler 화면에 재사용 가능.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
public class MaxAspectClamp : MonoBehaviour
{
    [Tooltip("UI 상한 화면비 (가로/세로). 기본 16:9.")]
    [SerializeField] private float maxAspect = 16f / 9f;

    private RectTransform _rt;
    private Vector2 _lastParentSize = new Vector2(-1f, -1f);

    private void OnEnable()
    {
        _rt = (RectTransform)transform;
        _lastParentSize = new Vector2(-1f, -1f);   // 재활성 시 강제 재계산
        Apply();
    }

    // 부모 치수 변경(해상도/창 크기 변화) 시 캐시 무효화 → 같은 프레임 보정(깜빡임 차단)
    private void OnRectTransformDimensionsChange()
    {
        _lastParentSize = new Vector2(-1f, -1f);
    }

    private void Update()
    {
        Apply();
    }

    private void Apply()
    {
        if (!(transform.parent is RectTransform parent)) return;

        Vector2 size = parent.rect.size;
        if (size.x <= 0f || size.y <= 0f) return;          // 레이아웃 전(0-size) 프레임 스킵
        if (size == _lastParentSize) return;               // 변화 없으면 매 프레임 쓰기 회피
        _lastParentSize = size;

        float desiredW = Mathf.Min(size.x, size.y * maxAspect);
        float inset = Mathf.Max(0f, (size.x - desiredW) * 0.5f);

        _rt.anchorMin = Vector2.zero;
        _rt.anchorMax = Vector2.one;
        _rt.offsetMin = new Vector2(inset, 0f);
        _rt.offsetMax = new Vector2(-inset, 0f);
    }
}
