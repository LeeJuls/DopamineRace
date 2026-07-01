using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 표준 우상단 X 닫기 버튼 생성 헬퍼.
/// 모든 팝업이 동일 아이콘(Btn_Close_01~04)·동일 클릭 동작을 재사용하도록 표준화.
///
/// 사용법 (프리팹 팩토리 스크립트 또는 런타임 Init()/Awake() 어디서든 동일):
///   Button closeBtn = CloseButtonFactory.Attach(popupRoot.transform, () => popup.Hide());
///
/// 기존 팝업을 이 컴포넌트로 전환할 때(예: CharacterInfoPopup의 Text"X" → 아이콘):
///   기존 CloseBtn GameObject를 제거하고 위 한 줄로 교체.
/// </summary>
public static class CloseButtonFactory
{
    private const string SPRITE_PATH = "UI/Btn_Close"; // _01~_04 접미사
    private static readonly Vector2 DEFAULT_SIZE = new Vector2(53f, 39f); // 원본 픽셀 크기
    private static readonly Vector2 DEFAULT_ANCHORED_POS = new Vector2(-30f, -25f);

    /// <summary>
    /// parent 우상단에 표준 X 버튼을 생성해 부착하고 onClick에 콜백을 건다.
    /// anchoredPos 생략 시 우상단 앵커 기준 (-30, -25) 기본 여백 사용.
    /// </summary>
    public static Button Attach(Transform parent, Action onClick, Vector2? anchoredPos = null)
    {
        var go = new GameObject("CloseBtn");
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f); // 우상단
        rt.sizeDelta = DEFAULT_SIZE;
        rt.anchoredPosition = anchoredPos ?? DEFAULT_ANCHORED_POS;

        var img = go.AddComponent<Image>();
        img.sprite = Resources.Load<Sprite>(SPRITE_PATH + "_01");

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.SpriteSwap;
        btn.spriteState = new SpriteState
        {
            highlightedSprite = Resources.Load<Sprite>(SPRITE_PATH + "_02"),
            pressedSprite     = Resources.Load<Sprite>(SPRITE_PATH + "_03"),
            disabledSprite    = Resources.Load<Sprite>(SPRITE_PATH + "_04"),
        };
        if (onClick != null) btn.onClick.AddListener(onClick.Invoke);

        return btn;
    }
}
