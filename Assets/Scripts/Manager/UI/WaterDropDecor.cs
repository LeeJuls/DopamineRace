using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 정적 물방울 장식 컴포넌트.
/// CharacterItem 패널 전체에 작은 원형 방울을 흩뿌려 배치.
///
/// Inspector 조절 항목:
///   dropCount  — 표시할 방울 수 (최대 8)
///   dropColor  — 기본 색상 (알파는 ALPHAS 배열로 각각 조정)
///   sizeMin/Max — 방울 크기 범위(px)
/// </summary>
public class WaterDropDecor : MonoBehaviour
{
    [Header("물방울 설정")]
    [SerializeField] private int   dropCount = 5;
    [SerializeField] private Color dropColor = new Color(0.45f, 0.82f, 1f, 1f);
    [SerializeField] private float sizeMin   = 4f;
    [SerializeField] private float sizeMax   = 10f;

    // 3번째 스샷 패턴 참고 — 세로 방향으로 흩뿌린 배치
    // (anchorMin/Max 기준 정규화 좌표, 오른쪽 영역에 집중)
    private static readonly Vector2[] POSITIONS =
    {
        new Vector2(0.88f, 0.75f),
        new Vector2(0.84f, 0.55f),
        new Vector2(0.91f, 0.38f),
        new Vector2(0.86f, 0.20f),
        new Vector2(0.93f, 0.60f),
        new Vector2(0.89f, 0.42f),
        new Vector2(0.82f, 0.68f),
        new Vector2(0.95f, 0.28f),
    };

    // 각 방울 크기 비율 (0~1)
    private static readonly float[] SIZE_RATIOS = { 0.9f, 0.6f, 0.8f, 0.5f, 0.7f, 0.4f, 0.6f, 0.5f };

    // 각 방울 알파값
    private static readonly float[] ALPHAS = { 0.40f, 0.25f, 0.35f, 0.20f, 0.30f, 0.18f, 0.28f, 0.15f };

    private void Awake()
    {
        BuildDrops();
    }

    private void BuildDrops()
    {
        Sprite circleSpr = Resources.Load<Sprite>("VFX/vfx_circle");
        int count = Mathf.Min(dropCount, POSITIONS.Length);

        for (int i = 0; i < count; i++)
        {
            GameObject dot = new GameObject("WaterDrop_" + i);
            dot.transform.SetParent(transform, false);

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
    }
}
