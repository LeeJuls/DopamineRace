using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터 상세 정보 팝업 (기획서 9번 영역).
/// 현재는 빈 껍데기 → Coming Soon 텍스트 + 닫기 버튼만.
/// </summary>
public class CharacterInfoPopup : MonoBehaviour
{
    private Text titleText;
    private Text contentText;

    private bool isInitialized;

    /// <summary>
    /// 자식 참조 캐싱 (프리팹 Instantiate 후 한 번 호출)
    /// </summary>
    public void Init()
    {
        titleText   = FindText("TitleText");
        contentText = FindText("ContentText");

        // 닫기 버튼 연결
        Transform closeBtnObj = transform.Find("CloseBtn");
        if (closeBtnObj != null)
        {
            Button closeBtn = closeBtnObj.GetComponent<Button>();
            if (closeBtn != null)
                closeBtn.onClick.AddListener(Hide);

            Text closeText = FindText("CloseBtn/Text");
            if (closeText != null)
                closeText.text = Loc.Get("str.ui.btn.close");
        }

        isInitialized = true;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 팝업 표시 (캐릭터 클릭 시)
    /// </summary>
    public void Show(CharacterData data, PopularityInfo info)
    {
        if (!isInitialized) Init();

        if (titleText != null)
            titleText.text = data != null ? data.DisplayName : "???";

        if (contentText != null)
            contentText.text = Loc.Get("str.ui.charinfo.coming_soon");

        gameObject.SetActive(true);
    }

    /// <summary>
    /// 팝업 닫기
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // ═══ 유틸 ═══

    private Text FindText(string path)
    {
        Transform t = transform.Find(path);
        return t != null ? t.GetComponent<Text>() : null;
    }
}
