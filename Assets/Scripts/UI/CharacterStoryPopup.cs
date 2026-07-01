using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터 스토리(세계관 텍스트) 팝업.
/// CharacterInfoPopup의 PenIconBtn 클릭 시 표시된다.
/// BettingPanel.prefab 루트의 nested PrefabInstance로 1개 인스턴스를 <see cref="Instance"/>로 노출.
///
/// ※ 보이기/숨기기는 자식 <c>box</c>를 토글한다(루트는 항상 활성).
///   루트를 SetActive(false) 하면 Awake가 다시 실행되지 않아 Instance 등록이 깨지므로
///   (ItemInfoPopup과 동일 원칙 — CharacterInfoPopup처럼 루트를 직접 끄는 방식과 혼동 금지).
///
/// 프리팹: Assets/Prefabs/UI/CharacterStoryPopup.prefab
/// 스토리 텍스트는 StringTable 키(str.char.NNN.story), CharacterDataV4.charStory 경유 조회.
/// </summary>
public class CharacterStoryPopup : MonoBehaviour
{
    public static CharacterStoryPopup Instance { get; private set; }

    [Header("UI 참조")]
    [SerializeField] private GameObject box;         // 보이기/숨기기 대상 (루트 아님)
    [SerializeField] private Text storyText;         // ScrollRect/Content 안의 본문 텍스트
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Button closeBtn;

    /// <summary>현재 표시 중인 캐릭터 UID</summary>
    public string CurrentCharId { get; private set; }

    /// <summary>팝업이 열려있는지</summary>
    public bool IsShowing => box != null && box.activeSelf;

    private void Awake()
    {
        Instance = this;
        if (closeBtn != null)
            closeBtn.onClick.AddListener(Hide);
        if (box != null)
            box.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 캐릭터 스토리 표시. charId로 CharacterDatabaseV4 조회 → charStory(StringUID) → Loc.Get.
    /// </summary>
    public void Show(string charId)
    {
        CurrentCharId = charId;

        string storyKey = null;
        var v4 = CharacterDatabaseV4.FindById(charId);
        if (v4 != null) storyKey = v4.charStory;

        if (storyText != null)
        {
            string resolved = !string.IsNullOrEmpty(storyKey) ? Loc.Get(storyKey) : "";
            // 키는 있으나 해당 언어 값이 비어있는 경우(예: 시나리오 미작성 캐릭터)도 폴백 처리
            storyText.text = !string.IsNullOrEmpty(resolved)
                ? resolved
                : Loc.Get("str.char.story.empty");
        }

        if (box != null) box.SetActive(true);

        // 스크롤 위치 최상단 리셋 + 콘텐츠 높이 강제 리빌드 (ContentSizeFitter 반영 대기 없이 즉시)
        if (scrollRect != null && scrollRect.content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
            scrollRect.verticalNormalizedPosition = 1f;
        }

        ApplyFont();
    }

    /// <summary>팝업 닫기</summary>
    public void Hide()
    {
        CurrentCharId = null;
        if (box != null) box.SetActive(false);
    }

    /// <summary>현재 언어 폰트 일괄 적용 (CJK·한글 라우팅) — ItemInfoPopup과 동일 정책.</summary>
    private void ApplyFont()
    {
        Font font = FontHelper.GetFontForCurrentLanguage();
        if (font == null) font = FontHelper.GetMainFont();
        if (font == null) return;

        foreach (var t in GetComponentsInChildren<Text>(true))
            t.font = font;
    }
}
