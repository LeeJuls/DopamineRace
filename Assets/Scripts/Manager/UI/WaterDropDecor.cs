using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 정적 물방울 장식 컴포넌트.
/// CharacterItem 패널 우 하단에 원형 방울을 배치.
/// 렌더링 순서: 배경 다음 → 모든 label보다 뒤 (sibling index 1).
///
/// Inspector 조절 항목 (변경 후 Play 모드 진입 시 반영):
///   dropCount  — 표시할 방울 수 (최대 8)
///   dropColor  — 기본 색상 (알파는 ALPHAS 배열로 각각 조정)
///   sizeMin/Max — 방울 크기 범위(px)
/// </summary>
public class WaterDropDecor : MonoBehaviour
{
    [Header("물방울 설정")]
    [SerializeField] private int   dropCount = 7;
    [SerializeField] private Color dropColor = new Color(0.45f, 0.82f, 1f, 1f);
    [SerializeField] private float sizeMin   = 4f;
    [SerializeField] private float sizeMax   = 10f;

    // 우 하단 밀집 패턴
    // 하단 행(y~0.12): 4개, 그 위 행(y~0.28): 3개
    //
    //  ┌──────────────────────────────┐
    //  │ [아이콘]  이름  전적         │
    //  │                              │
    //  │                    ● ● ●     │
    //  │                  ● ● ● ●     │
    //  └──────────────────────────────┘
    private static readonly Vector2[] POSITIONS =
    {
        // 하단 행 (4개)
        new Vector2(0.72f, 0.13f),
        new Vector2(0.79f, 0.10f),
        new Vector2(0.86f, 0.13f),
        new Vector2(0.93f, 0.10f),
        // 상단 행 (3개)
        new Vector2(0.75f, 0.30f),
        new Vector2(0.82f, 0.27f),
        new Vector2(0.89f, 0.30f),
        // 예비 (8번째)
        new Vector2(0.93f, 0.27f),
    };

    // 크기 비율 (0~1) — 하단 행이 조금 더 큼
    private static readonly float[] SIZE_RATIOS = { 0.9f, 0.8f, 0.85f, 0.75f, 0.65f, 0.55f, 0.60f, 0.50f };

    // 알파값
    private static readonly float[] ALPHAS = { 0.38f, 0.28f, 0.35f, 0.25f, 0.22f, 0.18f, 0.20f, 0.15f };

    private void Awake()
    {
        BuildDrops();
    }

    private void BuildDrops()
    {
        Sprite circleSpr = Resources.Load<Sprite>("VFX/vfx_circle");
        int count = Mathf.Min(dropCount, POSITIONS.Length);

        // 컨테이너 하나로 묶어서 sibling index 관리
        GameObject container = new GameObject("WaterDropContainer");
        container.transform.SetParent(transform, false);

        RectTransform containerRT = container.AddComponent<RectTransform>();
        containerRT.anchorMin       = Vector2.zero;
        containerRT.anchorMax       = Vector2.one;
        containerRT.offsetMin       = Vector2.zero;
        containerRT.offsetMax       = Vector2.zero;

        // 방울 생성
        for (int i = 0; i < count; i++)
        {
            GameObject dot = new GameObject("Drop_" + i);
            dot.transform.SetParent(container.transform, false);

            RectTransform rt = dot.AddComponent<RectTransform>();
            rt.anchorMin        = POSITIONS[i];
            rt.anchorMax        = POSITIONS[i];
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;

            float sz = Mathf.Lerp(sizeMin, sizeMax, SIZE_RATIOS[i]);
            rt.sizeDelta = new Vector2(sz, sz);

            Image img = dot.AddComponent<Image>();
            if (circleSpr != null) img.sprite = circleSpr;
            img.raycastTarget = false;

            Color c = dropColor;
            c.a = ALPHAS[i];
            img.color = c;
        }

        // ★ 배경(index 0) 바로 다음인 index 1로 이동
        //   → 모든 label/icon 자식보다 앞서 렌더링되지 않음 (뒤에 위치)
        container.transform.SetSiblingIndex(1);
    }
}
