using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using XCharts.Runtime;

/// <summary>
/// 캐릭터 상세 정보 팝업 (기획서 9번 영역).
/// 3개 레이아웃: 최근 경기기록(텍스트) / 일러스트+승률 / 레이더차트 / 스킬
/// </summary>
public class CharacterInfoPopup : MonoBehaviour
{
    // ═══ UI 참조 (Init에서 캐싱) ═══
    private Text charTypeLabel;
    private Text winRateLabel;
    private Text skillDescLabel;
    private Image illustration;
    private Image skillIcon;
    private Image storyIcon;
    private Button closeBtn;

    // 최근 경기기록 텍스트 (Layout1_TopArea)
    private Text recentRecordHeader;
    private Text shortDistLabel;
    private Text shortDistRanks;
    private Text midDistLabel;
    private Text midDistRanks;
    private Text longDistLabel;
    private Text longDistRanks;

    // 차트 영역
    private GameObject radarChartArea;

    // XCharts — 레이더차트 (능력치)
    private RadarChart radarChart;

    // ═══ 상태 ═══
    private bool isInitialized;
    private string currentCharName;
    private Coroutine showCoroutine;
    private Vector2 targetPosition;

    // 코루틴 전달용 (Show → ShowSequence)
    private CharacterData pendingData;
    private CharacterRecord pendingRecord;

    /// <summary>현재 표시 중인 캐릭터 UID</summary>
    public string CurrentCharName => currentCharName;

    /// <summary>팝업이 열려있는지</summary>
    public bool IsShowing => gameObject.activeSelf;

    // ═══ 초기화 ═══

    /// <summary>
    /// 자식 참조 캐싱 (프리팹 Instantiate 후 한 번 호출)
    /// </summary>
    public void Init()
    {
        // Layout1_TopArea
        Transform layout1 = transform.Find("Layout1_TopArea");
        if (layout1 != null)
        {
            charTypeLabel = FindText(layout1, "CharTypeLabel");

            Transform closeBtnObj = layout1.Find("CloseBtn");
            if (closeBtnObj != null)
            {
                closeBtn = closeBtnObj.GetComponent<Button>();
                if (closeBtn != null)
                    closeBtn.onClick.AddListener(Hide);

                Text closeText = FindText(closeBtnObj, "Text");
                if (closeText != null)
                    closeText.text = "X";
            }

            // 최근 경기기록 텍스트 (프리팹 Patch로 생성됨)
            recentRecordHeader = FindText(layout1, "RecentRecordHeader");

            Transform shortRow = layout1.Find("ShortDistRow");
            if (shortRow != null)
            {
                shortDistLabel = FindText(shortRow, "ShortDistLabel");
                shortDistRanks = FindText(shortRow, "ShortDistRanks");
            }

            Transform midRow = layout1.Find("MidDistRow");
            if (midRow != null)
            {
                midDistLabel = FindText(midRow, "MidDistLabel");
                midDistRanks = FindText(midRow, "MidDistRanks");
            }

            Transform longRow = layout1.Find("LongDistRow");
            if (longRow != null)
            {
                longDistLabel = FindText(longRow, "LongDistLabel");
                longDistRanks = FindText(longRow, "LongDistRanks");
            }
        }

        // Layout2_Left
        Transform layout2Left = transform.Find("Layout2_Left");
        if (layout2Left != null)
        {
            Transform illustObj = layout2Left.Find("Illustration");
            if (illustObj != null)
                illustration = illustObj.GetComponent<Image>();

            Transform storyObj = layout2Left.Find("StoryIconBtn");
            if (storyObj != null)
            {
                storyIcon = storyObj.GetComponent<Image>();
                // book_icon 로드
                Sprite bookSpr = Resources.Load<Sprite>("Icon/book_icon");
                if (bookSpr != null && storyIcon != null)
                    storyIcon.sprite = bookSpr;
            }

            // WinRateBg/WinRateLabel (패치 후) 또는 WinRateLabel (패치 전) 양쪽 호환
            Transform winRateBg = layout2Left.Find("WinRateBg");
            if (winRateBg != null)
                winRateLabel = FindText(winRateBg, "WinRateLabel");
            else
                winRateLabel = FindText(layout2Left, "WinRateLabel");
        }

        // Layout2_Right → RadarChartArea
        Transform layout2Right = transform.Find("Layout2_Right");
        if (layout2Right != null)
        {
            Transform radarObj = layout2Right.Find("RadarChartArea");
            if (radarObj != null)
                radarChartArea = radarObj.gameObject;
        }

        // Layout3_Bottom
        Transform layout3 = transform.Find("Layout3_Bottom");
        if (layout3 != null)
        {
            Transform skillObj = layout3.Find("SkillIcon");
            if (skillObj != null)
            {
                skillIcon = skillObj.GetComponent<Image>();
                // sword 아이콘 로드
                Sprite swordSpr = Resources.Load<Sprite>("Icon/sword");
                if (swordSpr != null && skillIcon != null)
                    skillIcon.sprite = swordSpr;
            }

            skillDescLabel = FindText(layout3, "SkillDescLabel");
        }

        // 차트는 Show()에서 레이아웃 확정 후 초기화 (RectTransform 0 방지)

        // 타겟 위치 저장 (슬라이드 애니메이션용)
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null)
            targetPosition = rt.anchoredPosition;

        isInitialized = true;
        gameObject.SetActive(false);
    }

    // ═══ 표시/숨기기 ═══

    /// <summary>
    /// 팝업 표시 (캐릭터 클릭 시)
    /// </summary>
    public void Show(CharacterData data, PopularityInfo info, CharacterRecord record)
    {
        if (!isInitialized) Init();
        if (data == null) return;

        currentCharName = data.charName;

        // 1) 캐릭터 타입 라벨 + 컬러
        if (charTypeLabel != null)
        {
            charTypeLabel.text = data.GetTypeName();
            charTypeLabel.color = data.GetTypeColor();
        }

        // 2) 일러스트
        if (illustration != null)
        {
            Sprite spr = data.LoadIllustration();
            if (spr != null)
            {
                illustration.sprite = spr;
                illustration.color = Color.white;
            }
            else
            {
                illustration.color = new Color(0.3f, 0.3f, 0.3f);
            }
        }

        // 3) 승률
        if (winRateLabel != null)
        {
            float winPct = record != null ? record.WinRate * 100f : 0f;
            winRateLabel.text = Loc.Get("str.ui.char.winrate", winPct.ToString("F0"));
        }

        // 4) 스킬 설명
        if (skillDescLabel != null)
        {
            string skillKey = data.charSkillDesc;
            if (!string.IsNullOrEmpty(skillKey))
                skillDescLabel.text = Loc.Get(skillKey);
            else
                skillDescLabel.text = Loc.Get(data.charName.Replace(".name", ".ability"));
        }

        // 5) 코루틴 전달용 저장
        pendingData = data;
        pendingRecord = record;

        // 6) 활성화 + 레이아웃 강제 갱신
        gameObject.SetActive(true);
        Canvas.ForceUpdateCanvases();

        // 7) 차트 초기화 + 데이터 갱신 + 슬라이드를 코루틴으로 통합
        //    1프레임 대기하여 중첩 stretch RectTransform 크기 확정 보장
        if (showCoroutine != null)
            StopCoroutine(showCoroutine);
        showCoroutine = StartCoroutine(ShowSequence());
    }

    /// <summary>
    /// 팝업 닫기
    /// </summary>
    public void Hide()
    {
        if (showCoroutine != null)
        {
            StopCoroutine(showCoroutine);
            showCoroutine = null;
        }
        currentCharName = null;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 차트 초기화 → 데이터 갱신 → 슬라이드인 통합 코루틴
    /// </summary>
    private IEnumerator ShowSequence()
    {
        // ── 1프레임 대기: 중첩 stretch 앵커 RectTransform 크기 확정 ──
        yield return null;

        // ── 레이더차트 생성 (최초 1회) ──
        if (radarChart == null)
            InitRadarChart();

        // ── 데이터 갱신 ──
        UpdateRadarChart(pendingData);
        UpdateRecentRecords(pendingRecord);

        // ── 슬라이드인 애니메이션 ──
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            Vector2 startPos = targetPosition + new Vector2(0, -200);
            rt.anchoredPosition = startPos;

            float elapsed = 0f;
            float duration = 0.25f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                rt.anchoredPosition = Vector2.Lerp(startPos, targetPosition, t);
                yield return null;
            }
            rt.anchoredPosition = targetPosition;
        }

        showCoroutine = null;
    }

    // ═══ XCharts: 능력치 레이더차트 ═══

    private void InitRadarChart()
    {
        if (radarChartArea == null)
        {
            Debug.LogWarning("[CharInfoPopup] radarChartArea is null — skip InitRadarChart");
            return;
        }

        radarChart = radarChartArea.GetComponent<RadarChart>();
        if (radarChart == null)
            radarChart = radarChartArea.AddComponent<RadarChart>();

        radarChart.Init();
        // stretch 앵커 → SetSize() 불가, 차트가 RectTransform에 자동 맞춤

        // 배경 투명 (theme.transparentBackground → DrawBackground 폴백도 투명)
        radarChart.theme.transparentBackground = true;
        radarChart.raycastTarget = false; // 클릭 이벤트 통과

        // 제목 비활성
        var title = radarChart.EnsureChartComponent<Title>();
        title.show = false;

        // Background 비활성
        var bg = radarChart.EnsureChartComponent<Background>();
        bg.show = false;

        // RadarCoord 기본 설정 (인디케이터는 UpdateRadarChart에서 매번 설정)
        var radar = radarChart.EnsureChartComponent<RadarCoord>();
        radar.shape = RadarCoord.Shape.Polygon;
        radar.splitNumber = 4; // 4구역 (5, 10, 15, 20)
        radar.startAngle = 90; // 상단부터 시작
        radar.center = new float[] { 0.5f, 0.5f }; // 차트 중앙

        // 반지름: 비율 기반 + 절대 상한 (과대 렌더링 방지)
        RectTransform chartRt = radarChartArea.GetComponent<RectTransform>();
        float w = chartRt.rect.width > 0 ? chartRt.rect.width : 200f;
        float h = chartRt.rect.height > 0 ? chartRt.rect.height : 200f;
        float minDim = Mathf.Min(w, h);
        // 영역의 30%를 반지름으로 사용하되, 최소 25px ~ 최대 80px
        float radius = Mathf.Clamp(minDim * 0.30f, 25f, 80f);
        radar.radius = radius;

        Debug.Log($"[CharInfoPopup] InitRadarChart rect={w:F0}x{h:F0}, radius={radius:F0}");

        radarChart.RefreshChart();
    }

    private void UpdateRadarChart(CharacterData data)
    {
        if (radarChart == null || data == null)
        {
            Debug.LogWarning($"[CharInfoPopup] UpdateRadarChart skip — chart={radarChart != null}, data={data != null}");
            return;
        }

        radarChart.RemoveData();

        // RemoveData()가 인디케이터도 삭제하므로 매번 재설정
        var radar = radarChart.GetChartComponent<RadarCoord>();
        if (radar != null)
        {
            radar.indicatorList.Clear();
            string[] statKeys = {
                "str.ui.char.stat.speed", "str.ui.char.stat.power",
                "str.ui.char.stat.brave", "str.ui.char.stat.calm",
                "str.ui.char.stat.endurance", "str.ui.char.stat.luck"
            };
            foreach (string key in statKeys)
                radar.AddIndicator(Loc.Get(key), 0, 20);
        }

        // 4색 배경 시리즈 (구역 표현: 큰 값부터 → 작은 값 위에 겹침)
        Color[] zoneColors = {
            new Color(0f, 0.8f, 0.27f, 0.5f),   // 16~20: #00CC44
            new Color(0.67f, 0.8f, 0f, 0.5f),    // 11~15: #AACC00
            new Color(1f, 0.53f, 0f, 0.4f),       // 6~10:  #FF8800
            new Color(0.8f, 0.2f, 0.2f, 0.4f),    // 1~5:   #CC3333
        };
        float[] zoneValues = { 20f, 15f, 10f, 5f };

        for (int z = 0; z < 4; z++)
        {
            // AddSerie 반환값 직접 사용 (GetSerie 인덱스 불일치 방지)
            var zoneSerie = radarChart.AddSerie<Radar>("zone_" + z);
            if (zoneSerie != null)
            {
                zoneSerie.EnsureComponent<AreaStyle>();
                zoneSerie.EnsureComponent<LabelStyle>();
                zoneSerie.areaStyle.show = true;
                zoneSerie.areaStyle.color = zoneColors[z];
                zoneSerie.lineStyle.show = false;
                zoneSerie.symbol.show = false;
                zoneSerie.label.show = false;
            }
            var zoneData = new List<double>();
            for (int s = 0; s < 6; s++)
                zoneData.Add(zoneValues[z]);
            radarChart.AddData(z, zoneData);
        }

        // 실제 스탯 시리즈
        var statSerie = radarChart.AddSerie<Radar>("stats");
        int statIdx = 4;
        if (statSerie != null)
        {
            statSerie.EnsureComponent<AreaStyle>();
            statSerie.EnsureComponent<LabelStyle>();
            statSerie.areaStyle.show = true;
            statSerie.areaStyle.color = new Color(1f, 1f, 1f, 0.3f);
            statSerie.lineStyle.show = true;
            statSerie.lineStyle.color = Color.white;
            statSerie.lineStyle.width = 2f;
            statSerie.symbol.show = true;
            statSerie.symbol.type = SymbolType.Circle;
            statSerie.symbol.size = 4;
            statSerie.symbol.color = Color.white;
            statSerie.label.show = false;
        }

        var statValues = new List<double>
        {
            data.charBaseSpeed * 10f, // speed는 0.9~1.2 범위이므로 ×10 스케일링
            data.charBasePower,
            data.charBaseBrave,
            data.charBaseCalm,
            data.charBaseEndurance,
            data.charBaseLuck
        };
        radarChart.AddData(statIdx, statValues);
        Debug.Log($"[CharInfoPopup] UpdateRadarChart: stats=[{string.Join(",", statValues)}], series={radarChart.series.Count}");

        radarChart.RefreshChart();
    }

    // ═══ 최근 경기기록 텍스트 ═══

    private const int MAX_DIST_DISPLAY = 6;

    /// <summary>
    /// 최근 경기기록을 거리별(단/중/장)로 분류하여 텍스트 갱신
    /// </summary>
    private void UpdateRecentRecords(CharacterRecord record)
    {
        var gs = GameSettings.Instance;

        // 헤더
        if (recentRecordHeader != null)
            recentRecordHeader.text = Loc.Get("str.ui.char.recent_record");

        // 거리별 라벨
        if (shortDistLabel != null)
            shortDistLabel.text = Loc.Get("str.ui.track.short");
        if (midDistLabel != null)
            midDistLabel.text = Loc.Get("str.ui.track.mid");
        if (longDistLabel != null)
            longDistLabel.text = Loc.Get("str.ui.track.long");

        // 거리별 순위 분류
        List<int> shortRanks = new List<int>();
        List<int> midRanks = new List<int>();
        List<int> longRanks = new List<int>();

        if (record != null && gs != null)
        {
            foreach (var entry in record.recentRaceEntries)
            {
                if (entry.laps <= 0) continue; // 레거시 마이그레이션 엔트리 스킵

                string distKey = gs.GetDistanceKey(entry.laps);
                if (distKey == "str.ui.track.short" && shortRanks.Count < MAX_DIST_DISPLAY)
                    shortRanks.Add(entry.rank);
                else if (distKey == "str.ui.track.mid" && midRanks.Count < MAX_DIST_DISPLAY)
                    midRanks.Add(entry.rank);
                else if (distKey == "str.ui.track.long" && longRanks.Count < MAX_DIST_DISPLAY)
                    longRanks.Add(entry.rank);
            }
        }

        // 텍스트 갱신
        if (shortDistRanks != null)
            shortDistRanks.text = FormatRankList(shortRanks);
        if (midDistRanks != null)
            midDistRanks.text = FormatRankList(midRanks);
        if (longDistRanks != null)
            longDistRanks.text = FormatRankList(longRanks);
    }

    /// <summary>순위 리스트를 rich text 색상 코딩 문자열로 변환</summary>
    private string FormatRankList(List<int> ranks)
    {
        if (ranks.Count == 0)
            return Loc.Get("str.ui.char.no_dist_record");

        var parts = new List<string>();
        foreach (int rank in ranks)
            parts.Add(ColoredRank(rank));
        return string.Join("  ", parts.ToArray());
    }

    /// <summary>순위에 따른 rich text 색상 적용</summary>
    private string ColoredRank(int rank)
    {
        string rankText = Loc.Get("str.hud.rank", rank);
        switch (rank)
        {
            case 1:  return "<color=#FFD700>" + rankText + "</color>";
            case 2:  return "<color=#C0C0C0>" + rankText + "</color>";
            case 3:  return "<color=#CD7F32>" + rankText + "</color>";
            default: return "<color=#CCCCCC>" + rankText + "</color>";
        }
    }

    // ═══ 유틸 ═══

    private Text FindText(string path)
    {
        Transform t = transform.Find(path);
        return t != null ? t.GetComponent<Text>() : null;
    }

    private Text FindText(Transform parent, string childName)
    {
        Transform t = parent.Find(childName);
        return t != null ? t.GetComponent<Text>() : null;
    }

}
