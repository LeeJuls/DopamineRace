#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// CharacterStoryPopup.prefab 에 우측 세로 스크롤바를 추가하는 in-place 패처.
///
/// 배경: 기존 프리팹은 ScrollRect 와 viewport 가 같은 GameObject(ScrollArea, 2계층)라
///       AutoHideAndExpandViewport 가 viewport 를 self-resize 하면 RectMask2D 마스크·앵커가 흔들린다.
///       → FinishLeaderboardUIPrefabCreator.cs:191-232 에서 검증된 표준 3계층으로 승격한다:
///
///   ScrollArea (ScrollRect only; RectMask2D 제거; viewport 참조 → 새 Viewport)
///   ├── Viewport (신규; RectMask2D 이관; fill-stretch)   ← AutoHide 축소 대상
///   │   └── Content (기존 이동; top-stretch+ContentSizeFitter 유지)
///   └── Scrollbar Vertical (Image=트랙 + Scrollbar[BottomToTop])  ← Viewport 의 형제
///       └── Sliding Area └── Handle (Image)
///
/// 구현: his046(NameEntryModalDecorator) 패턴 —
///       PrefabUtility.LoadPrefabContents → 계층 재구성 → SaveAsPrefabAsset → UnloadPrefabContents.
///       Factory(CharacterStoryPopupPrefabFactory) 재생성은 절대 하지 않는다
///       (fontSize 26↔34 등 수동조정 롤백 위험). 이 패처만 1회 실행.
///
/// 멱등: 이미 Viewport 또는 Scrollbar Vertical 이 있으면 중단.
/// </summary>
public static class CharacterStoryScrollbarPatcher
{
    private const string PREFAB = "Assets/Prefabs/UI/CharacterStoryPopup.prefab";

    // 스타일 확정값 (팝업 하늘색 톤 — WaterDropDecor dropColor 와 동일)
    private static readonly Color HANDLE_COLOR = new Color(0.45f, 0.82f, 1f, 1f);
    private static readonly Color TRACK_COLOR  = new Color(0.85f, 0.90f, 0.95f, 0.45f);
    private const float SB_WIDTH = 16f;

    [MenuItem("DopamineRace/프리팹 패치/CharacterStory 세로 스크롤바 추가")]
    public static void Patch()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PREFAB);
        if (root == null)
        {
            Debug.LogError($"[ScrollbarPatcher] 프리팹 로드 실패: {PREFAB}");
            return;
        }

        try
        {
            Transform scrollArea = root.transform.Find("Box/ScrollArea");
            if (scrollArea == null)
            {
                Debug.LogError("[ScrollbarPatcher] Box/ScrollArea 없음 — 중단");
                return;
            }

            var sr      = scrollArea.GetComponent<ScrollRect>();
            var mask    = scrollArea.GetComponent<RectMask2D>();
            Transform content = scrollArea.Find("Content");
            if (sr == null || content == null)
            {
                Debug.LogError("[ScrollbarPatcher] ScrollRect 또는 Content 없음 — 중단");
                return;
            }

            // 멱등 가드
            if (scrollArea.Find("Viewport") != null || scrollArea.Find("Scrollbar Vertical") != null)
            {
                Debug.LogWarning("[ScrollbarPatcher] 이미 패치됨(Viewport/Scrollbar Vertical 존재) — 중단");
                return;
            }

            // ── 1) Viewport 신규(RectMask2D 이관) ──
            var viewportGo = new GameObject("Viewport", typeof(RectTransform));
            viewportGo.transform.SetParent(scrollArea, false);
            viewportGo.transform.SetAsFirstSibling();
            var vpRt = viewportGo.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.pivot = new Vector2(0f, 1f);
            vpRt.anchoredPosition = Vector2.zero;
            vpRt.sizeDelta = Vector2.zero;                 // 우측 여백 0 — AutoHide 가 런타임에 축소
            var vpMask = viewportGo.AddComponent<RectMask2D>();
            if (mask != null) vpMask.padding = mask.padding;

            // ScrollArea 의 기존 RectMask2D 제거(스크롤바 마스킹 방지)
            if (mask != null) Object.DestroyImmediate(mask);

            // ── 2) Content 를 Viewport 아래로 이동 (앵커/피벗/CSF 유지) ──
            content.SetParent(vpRt, false);

            // ── 3) Scrollbar Vertical (viewport 의 형제, ScrollArea 자식) ──
            var sbGo = new GameObject("Scrollbar Vertical", typeof(RectTransform));
            sbGo.transform.SetParent(scrollArea, false);
            sbGo.transform.SetAsLastSibling();
            var sbRt = sbGo.GetComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(1f, 0f);
            sbRt.anchorMax = new Vector2(1f, 1f);
            sbRt.pivot     = new Vector2(1f, 0.5f);
            sbRt.anchoredPosition = Vector2.zero;          // 우측 안쪽 고정(피벗 우측)
            sbRt.sizeDelta = new Vector2(SB_WIDTH, 0f);

            var track = sbGo.AddComponent<Image>();
            track.color = TRACK_COLOR;
            track.raycastTarget = true;                    // 트랙 클릭 페이지 점프 허용

            var sb = sbGo.AddComponent<Scrollbar>();
            sb.direction = Scrollbar.Direction.BottomToTop;
            sb.interactable = true;
            sb.value = 1f;                                 // 초기 최상단(핸들 상단) — Show() 리셋과 일치

            // Sliding Area
            var slidingGo = new GameObject("Sliding Area", typeof(RectTransform));
            slidingGo.transform.SetParent(sbGo.transform, false);
            var slRt = slidingGo.GetComponent<RectTransform>();
            slRt.anchorMin = Vector2.zero;
            slRt.anchorMax = Vector2.one;
            slRt.pivot = new Vector2(0.5f, 0.5f);
            slRt.anchoredPosition = Vector2.zero;
            slRt.sizeDelta = new Vector2(-4f, -4f);        // 트랙 안쪽 여백

            // Handle
            var handleGo = new GameObject("Handle", typeof(RectTransform));
            handleGo.transform.SetParent(slidingGo.transform, false);
            var hRt = handleGo.GetComponent<RectTransform>();
            hRt.anchorMin = Vector2.zero;
            hRt.anchorMax = Vector2.one;
            hRt.pivot = new Vector2(0.5f, 0.5f);
            hRt.anchoredPosition = Vector2.zero;
            hRt.sizeDelta = new Vector2(4f, 4f);           // Sliding Area 대비 최소 크기
            var handleImg = handleGo.AddComponent<Image>();
            handleImg.color = HANDLE_COLOR;

            sb.handleRect    = hRt;
            sb.targetGraphic = handleImg;

            // ── 4) ScrollRect 배선 ──
            sr.viewport = vpRt;                            // ★ viewport 를 새 Viewport 로 교체(핵심)
            sr.content  = content.GetComponent<RectTransform>();
            sr.verticalScrollbar = sb;
            sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            sr.verticalScrollbarSpacing = 0f;

            // SaveAsPrefabAsset 후 일부 프로퍼티가 리셋되는 quirk 방지 — 핵심 값은 SerializedObject 로 재확정
            var srSo = new SerializedObject(sr);
            srSo.FindProperty("m_Viewport").objectReferenceValue = vpRt;
            srSo.FindProperty("m_VerticalScrollbar").objectReferenceValue = sb;
            srSo.FindProperty("m_VerticalScrollbarVisibility").enumValueIndex =
                (int)ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            srSo.FindProperty("m_VerticalScrollbarSpacing").floatValue = 0f;
            srSo.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(root);
            PrefabUtility.SaveAsPrefabAsset(root, PREFAB);
            Debug.Log("[ScrollbarPatcher] 완료 — 표준 3계층 승격 + 우측 세로 스크롤바 추가");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
#endif
