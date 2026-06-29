using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 아이템 이름·설명을 보여주는 모듈식 팝업 (트랙 설명창 스타일).
/// <see cref="ItemInfoTrigger"/>가 아이템 클릭 시 <see cref="Show"/>를 호출한다.
/// 씬당 1개 인스턴스를 <see cref="Instance"/>로 노출 → 어느 화면의 아이템이든 재사용.
///
/// 프리팹: Assets/Prefabs/UI/ItemInfoPopup.prefab
/// 이름/설명은 StringTable 키 (str.dopamine.item.name/desc.xxx).
/// SPEC-050
/// </summary>
public class ItemInfoPopup : MonoBehaviour
{
    /// <summary>씬에 배치된 팝업 인스턴스. 트리거가 이걸 통해 Show/Hide 호출.</summary>
    public static ItemInfoPopup Instance { get; private set; }

    [Header("UI 참조")]
    [SerializeField] private Text nameLabel;
    [SerializeField] private Text descLabel;
    [SerializeField] private Button backdropBtn;   // 박스 바깥 클릭 시 닫기

    /// <summary>현재 표시 중인 아이템의 이름 키 (토글 판정용)</summary>
    public string CurrentNameKey { get; private set; }

    /// <summary>팝업이 열려있는지</summary>
    public bool IsShowing => gameObject.activeSelf;

    private void Awake()
    {
        // 프리팹은 활성 상태로 Instantiate → Awake에서 등록 후 스스로 숨김.
        Instance = this;
        if (backdropBtn != null)
            backdropBtn.onClick.AddListener(Hide);
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 아이템 정보 표시. nameKey·descKey는 StringTable 키.
    /// </summary>
    public void Show(string nameKey, string descKey)
    {
        CurrentNameKey = nameKey;
        if (nameLabel != null) nameLabel.text = Loc.Get(nameKey);
        if (descLabel != null) descLabel.text = Loc.Get(descKey);

        gameObject.SetActive(true);
        ApplyFont();
    }

    /// <summary>팝업 닫기</summary>
    public void Hide()
    {
        CurrentNameKey = null;
        gameObject.SetActive(false);
    }

    /// <summary>현재 언어 폰트 일괄 적용 (CJK·한글 라우팅).</summary>
    private void ApplyFont()
    {
        Font font = FontHelper.GetFontForCurrentLanguage();
        if (font == null) font = FontHelper.GetMainFont();
        if (font == null) return;

        foreach (var t in GetComponentsInChildren<Text>(true))
            t.font = font;
    }
}
