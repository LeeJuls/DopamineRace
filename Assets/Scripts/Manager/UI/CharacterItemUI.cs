using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 배팅 화면의 캐릭터 1행 UI 컴포넌트.
/// 기획서 8-1~8-7 요소를 관리한다.
///
/// 자식 오브젝트 이름 규약:
///   Background, IconContainer/Icon, ConditionIcon,
///   PopularityLabel, NameLabel, RecordLabel, SecondLabel, BetOrderLabel
/// </summary>
public class CharacterItemUI : MonoBehaviour
{
    // ── 자식 참조 (Init에서 캐싱) ──
    private Image background;
    private Image icon;
    private Image conditionIcon;
    private Text popularityLabel;
    private Text nameLabel;
    private Text recordLabel;
    private Text secondLabel;
    private Text betOrderLabel;

    // ── 외부에서 사용하는 데이터 ──
    public CharacterData CharData { get; private set; }
    public int RacerIndex { get; set; }

    // ── 색상 상수 ──
    private static readonly Color COLOR_DEFAULT  = new Color(0.15f, 0.15f, 0.2f, 0.9f);
    private static readonly Color COLOR_SELECTED = new Color(0.25f, 0.3f, 0.5f, 0.95f);
    private static readonly Color COLOR_BET_ORDER = new Color(1f, 0.85f, 0.2f);

    /// <summary>
    /// transform.Find로 자식 캐싱. BuildPrefab 이후 한 번 호출.
    /// </summary>
    public void Init()
    {
        background = GetComponent<Image>();
        if (background == null)
            background = gameObject.AddComponent<Image>();

        icon            = FindImage("IconContainer/Icon");
        conditionIcon   = FindImage("ConditionIcon");
        popularityLabel = FindText("PopularityLabel");
        nameLabel       = FindText("NameLabel");
        recordLabel     = FindText("RecordLabel");
        secondLabel     = FindText("SecondLabel");
        betOrderLabel   = FindText("BetOrderLabel");
    }

    /// <summary>
    /// 캐릭터 데이터 세팅 (매 라운드 RefreshCharacterItems에서 호출)
    /// </summary>
    public void SetData(CharacterData data, int popularityRank,
                        PopularityInfo oddsInfo, CharacterRecord record)
    {
        CharData = data;

        // 8-1: 아이콘
        if (icon != null)
        {
            Sprite spr = data.LoadIcon();
            if (spr != null)
            {
                icon.sprite = spr;
                icon.color = Color.white;
            }
            else
            {
                icon.sprite = null;
                icon.color = new Color(0.3f, 0.3f, 0.3f);
            }
        }

        // 8-2: 컨디션 아이콘
        if (conditionIcon != null && oddsInfo != null)
        {
            Sprite condSpr = ConditionIconFactory.GetIcon(oddsInfo.condition);
            if (condSpr != null)
            {
                conditionIcon.sprite = condSpr;
                conditionIcon.color = Color.white;
                conditionIcon.gameObject.SetActive(true);
            }
        }
        else if (conditionIcon != null)
        {
            conditionIcon.gameObject.SetActive(false);
        }

        // 8-3: 인기순위
        if (popularityLabel != null)
            popularityLabel.text = Loc.Get("str.ui.char.popularity", popularityRank);

        // 8-4: 이름
        if (nameLabel != null)
            nameLabel.text = data.DisplayName;

        // 8-5: 전적 (N전 M승)
        if (recordLabel != null)
        {
            if (record != null && record.TotalRaces > 0)
            {
                int recentCount = record.recentOverallRanks != null ? record.recentOverallRanks.Count : 0;
                int wins = Mathf.RoundToInt(record.WinRate * recentCount);
                recordLabel.text = Loc.Get("str.ui.char.record", record.TotalRaces, wins);
            }
            else
            {
                recordLabel.text = Loc.Get("str.ui.char.record", 0, 0);
            }
        }

        // 8-6: 2착 횟수
        if (secondLabel != null)
        {
            if (record != null && record.recentOverallRanks != null)
            {
                int places = Mathf.RoundToInt(record.PlaceRate * record.recentOverallRanks.Count);
                secondLabel.text = Loc.Get("str.ui.char.second", places);
            }
            else
            {
                secondLabel.text = Loc.Get("str.ui.char.second", 0);
            }
        }

        // 배당률 표시는 oddsInfo.winOdds 사용 (nameLabel 옆이나 tooltip 등에서)
        // 기본 상태: 미선택
        SetBetOrder(0);
    }

    /// <summary>
    /// 배팅 선택 순서 표시.
    /// order=0: 미선택, 1=1착, 2=2착, 3=3착...
    /// </summary>
    public void SetBetOrder(int order)
    {
        if (betOrderLabel != null)
        {
            if (order > 0)
            {
                betOrderLabel.gameObject.SetActive(true);
                // BetInfo에서 라벨 가져오기
                var bet = GameManager.Instance?.CurrentBet;
                if (bet != null)
                    betOrderLabel.text = bet.GetSelectionLabel(order - 1);
                else
                    betOrderLabel.text = order.ToString();
                betOrderLabel.color = COLOR_BET_ORDER;
            }
            else
            {
                betOrderLabel.gameObject.SetActive(false);
            }
        }

        // 배경색 변경
        if (background != null)
            background.color = order > 0 ? COLOR_SELECTED : COLOR_DEFAULT;
    }

    /// <summary>
    /// 전적 정보 숨기기/보이기 (7번 토글)
    /// </summary>
    public void SetHideInfo(bool hide)
    {
        if (recordLabel != null)  recordLabel.gameObject.SetActive(!hide);
        if (secondLabel != null)  secondLabel.gameObject.SetActive(!hide);
    }

    // ═══ 유틸 ═══

    private Image FindImage(string path)
    {
        Transform t = transform.Find(path);
        return t != null ? t.GetComponent<Image>() : null;
    }

    private Text FindText(string path)
    {
        Transform t = transform.Find(path);
        return t != null ? t.GetComponent<Text>() : null;
    }
}
