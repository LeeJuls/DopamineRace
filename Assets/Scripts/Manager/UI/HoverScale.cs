using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 마우스 오버 시 살짝 커지는(튀어나오는) 호버 팝 효과. UGUI 요소에 부착.
/// - Selectable(Button 등)이 있으면 interactable일 때만 반응.
/// - localScale을 unscaledDeltaTime 기반 Lerp로 부드럽게 보간(타임스케일 영향 없음).
/// - 재활성/비활성 시 기준 크기로 안전 복귀.
/// </summary>
[DisallowMultipleComponent]
public class HoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("오버 시 배율 (1.10 = 10% 확대)")]
    [SerializeField] private float hoverScale = 1.10f;

    [Tooltip("보간 속도 (클수록 빠릿)")]
    [SerializeField] private float speed = 14f;

    private Vector3 _baseScale = Vector3.one;
    private Vector3 _targetScale = Vector3.one;
    private bool _hovering;

    private void Awake()
    {
        _baseScale = transform.localScale;
        _targetScale = _baseScale;
    }

    private void OnEnable()
    {
        // 재활성 시 기준 크기로 즉시 복귀 (호버 잔상 방지)
        _hovering = false;
        _targetScale = _baseScale;
        transform.localScale = _baseScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        var sel = GetComponent<Selectable>();
        if (sel != null && !sel.IsInteractable()) return;   // 비활성 버튼은 반응 안 함
        _hovering = true;
        _targetScale = _baseScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovering = false;
        _targetScale = _baseScale;
    }

    private void Update()
    {
        // 비활성 버튼인데 호버 중이면 기준으로 복귀
        if (_hovering)
        {
            var sel = GetComponent<Selectable>();
            if (sel != null && !sel.IsInteractable()) _targetScale = _baseScale;
        }

        if ((transform.localScale - _targetScale).sqrMagnitude > 0.000001f)
            transform.localScale = Vector3.Lerp(transform.localScale, _targetScale,
                Time.unscaledDeltaTime * speed);
        else
            transform.localScale = _targetScale;
    }
}
