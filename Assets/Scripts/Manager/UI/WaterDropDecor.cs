using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 정적 물방울 장식 컴포넌트.
/// CharacterItem 패널 우 하단에 원형 방울을 배치.
/// 렌더링 순서: 배경 다음 → 모든 label보다 뒤 (sibling index 1).
///
/// Inspector 조절 항목 (변경 후 Play 모드 진입 시 반영):
///   dropCount  — 표시할 방울 수 (최대 20)
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

    // 우 하단 → 위/좌로 퍼지는 밀집 패턴 (20개)
    //
    //  ┌──────────────────────────────┐
    //  │ [아이콘]  이름  전적         │
    //  │                     · · ·   │  ← 5행 (아주 작고 흐릿)
    //  │                   · · · ·   │  ← 4행 (작음)
    //  │                  ● ● ● ●    │  ← 3행 (중간)
    //  │                 ● ● ● ●     │  ← 2행 (중간-큰)
    //  │               ● ● ● ● ●    │  ← 1행 (가장 크고 진함)
    //  └──────────────────────────────┘
    private static readonly Vector2[] POSITIONS =
    {
        // ── 1행: 최하단 (5개, 가장 크고 진함) ──
        new Vector2(0.65f, 0.12f),
        new Vector2(0.72f, 0.09f),
        new Vector2(0.79f, 0.12f),
        new Vector2(0.86f, 0.09f),
        new Vector2(0.93f, 0.12f),

        // ── 2행 (4개) ──
        new Vector2(0.68f, 0.28f),
        new Vector2(0.75f, 0.25f),
        new Vector2(0.82f, 0.28f),
        new Vector2(0.89f, 0.25f),

        // ── 3행 (4개) ──
        new Vector2(0.71f, 0.43f),
        new Vector2(0.78f, 0.40f),
        new Vector2(0.85f, 0.43f),
        new Vector2(0.92f, 0.40f),

        // ── 4행 (4개) ──
        new Vector2(0.69f, 0.57f),
        new Vector2(0.76f, 0.54f),
        new Vector2(0.83f, 0.57f),
        new Vector2(0.90f, 0.54f),

        // ── 5행: 최상단 (3개, 가장 작고 흐릿) ──
        new Vector2(0.74f, 0.69f),
        new Vector2(0.81f, 0.66f),
        new Vector2(0.88f, 0.69f),
    };

    // 크기 비율 (0~1) — 하단 행이 크고 상단 행으로 갈수록 작아짐
    private static readonly float[] SIZE_RATIOS =
    {
        // 1행
        0.90f, 0.80f, 0.85f, 0.78f, 0.83f,
        // 2행
        0.65f, 0.58f, 0.62f, 0.55f,
        // 3행
        0.48f, 0.42f, 0.45f, 0.40f,
        // 4행
        0.34f, 0.29f, 0.32f, 0.27f,
        // 5행
        0.22f, 0.18f, 0.20f,
    };

    // 알파값 — 하단 진함 → 상단 흐릿
    private static readonly float[] ALPHAS =
    {
        // 1행
        0.38f, 0.28f, 0.35f, 0.26f, 0.32f,
        // 2행
        0.22f, 0.17f, 0.20f, 0.15f,
        // 3행
        0.13f, 0.10f, 0.12f, 0.09f,
        // 4행
        0.08f, 0.06f, 0.07f, 0.05f,
        // 5행
        0.05f, 0.04f, 0.04f,
    };

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
