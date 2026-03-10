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
    private Text oddsLabel;           // Phase 4: 단승 배당률 배지
    private Color oddsLabelBaseColor;  // Inspector 색상 캐싱
    private Text betOrderLabel;
    private Color betOrderLabelBaseColor; // Inspector 색상 캐싱

    // ── 외부에서 사용하는 데이터 ──
    public CharacterData CharData { get; private set; }
    public int RacerIndex { get; set; }

    // ── 배경 색상 상수 ──
    private static readonly Color COLOR_DEFAULT  = Color.white;                          // 스프라이트 원색 유지
    private static readonly Color COLOR_SELECTED = new Color(0.6f, 0.85f, 1f, 1f);      // 선택 시 하늘색 틴트

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
        popularityLabel = FindText("PopularityLabel/Text");
        nameLabel       = FindText("NameLabel");
        recordLabel     = FindText("RecordLabel");
        secondLabel     = FindText("SecondLabel");
        oddsLabel       = FindText("OddsLabel/Text");
        oddsLabelBaseColor    = oddsLabel     != null ? oddsLabel.color     : Color.white;
        betOrderLabel   = FindText("BetOrderLabel/Text");
        betOrderLabelBaseColor = betOrderLabel != null ? betOrderLabel.color : Color.white;
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

        // Phase 4: 단승 배당률 배지 (모든 캐릭터 항상 표시)
        if (oddsLabel != null)
        {
            oddsLabel.color = oddsLabelBaseColor; // Inspector 설정 색 복원
            if (oddsInfo != null && oddsInfo.winOdds > 0f)
                oddsLabel.text = oddsInfo.winOdds.ToString("F1") + "x";
            else
                oddsLabel.text = "-";
        }

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
            // BetOrderLabel 오브젝트는 Image(배경) 컨테이너 → 부모를 on/off
            GameObject container = betOrderLabel.transform.parent.gameObject;
            if (order > 0)
            {
                container.SetActive(true);
                // BetInfo에서 라벨 가져오기
                var bet = GameManager.Instance?.CurrentBet;
                if (bet != null)
                    betOrderLabel.text = bet.GetSelectionLabel(order - 1);
                else
                    betOrderLabel.text = order.ToString();
                betOrderLabel.color = betOrderLabelBaseColor; // Inspector 설정 색 사용
            }
            else
            {
                container.SetActive(false);
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
