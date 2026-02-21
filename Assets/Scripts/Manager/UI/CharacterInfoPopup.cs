using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using XCharts.Runtime;

/// <summary>
/// 캐릭터 상세 정보 팝업 (기획서 9번 영역).
/// 4개 레이아웃: 순위 그래프 / 일러스트+승률 / 레이더차트 / 스킬
/// </summary>
public class CharacterInfoPopup : MonoBehaviour
{
    // ═══ UI 참조 (Init에서 캐싱) ═══
    private Text charTypeLabel;
    private Text noRecordLabel;
    private Text winRateLabel;
    private Text skillDescLabel;
    private Image illustration;
    private Image skillIcon;
    private Image storyIcon;
    private Button closeBtn;

    // XCharts 영역
    private GameObject rankChartArea;
    private GameObject radarChartArea;
    private LineChart rankChart;
    private RadarChart radarChart;

    // ═══ 상태 ═══
    private bool isInitialized;
    private string currentCharName;
    private Coroutine slideCoroutine;
    private Vector2 targetPosition;

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
            noRecordLabel = FindText(layout1, "NoRecordLabel");

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

            Transform chartObj = layout1.Find("RankChartArea");
            if (chartObj != null)
                rankChartArea = chartObj.gameObject;
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

        // XCharts 초기화
        InitRankChart();
        InitRadarChart();

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

        // 5) 순위 그래프
        UpdateRankChart(record);

        // 6) 레이더차트
        UpdateRadarChart(data);

        // 7) 표시 + 슬라이드인
        gameObject.SetActive(true);
        if (slideCoroutine != null)
            StopCoroutine(slideCoroutine);
        slideCoroutine = StartCoroutine(SlideInAnimation());
    }

    /// <summary>
    /// 팝업 닫기
    /// </summary>
    public void Hide()
    {
        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
            slideCoroutine = null;
        }
        currentCharName = null;
        gameObject.SetActive(false);
    }

    // ═══ XCharts: 순위 꺾은선 그래프 ═══

    private void InitRankChart()
    {
        if (rankChartArea == null) return;

        rankChart = rankChartArea.GetComponent<LineChart>();
        if (rankChart == null)
            rankChart = rankChartArea.AddComponent<LineChart>();

        rankChart.Init();
        rankChart.SetSize(rankChartArea.GetComponent<RectTransform>().rect.width,
                          rankChartArea.GetComponent<RectTransform>().rect.height);

        // 기본 설정
        rankChart.RemoveData();
        rankChart.ClearComponentData();

        // 제목 비활성
        var title = rankChart.EnsureChartComponent<Title>();
        title.show = false;

        // X축 (회차)
        var xAxis = rankChart.EnsureChartComponent<XAxis>();
        xAxis.type = Axis.AxisType.Category;
        xAxis.show = false;

        // Y축 (순위 1~12, inverse)
        var yAxis = rankChart.EnsureChartComponent<YAxis>();
        yAxis.type = Axis.AxisType.Value;
        yAxis.inverse = true;
        yAxis.min = 1;
        yAxis.max = 12;
        yAxis.show = false;

        // 그리드
        var grid = rankChart.EnsureChartComponent<GridCoord>();
        grid.left = 10;
        grid.right = 10;
        grid.top = 10;
        grid.bottom = 10;

        // 배경 투명
        var bg = rankChart.EnsureChartComponent<Background>();
        bg.show = false;
    }

    private void UpdateRankChart(CharacterRecord record)
    {
        if (rankChart == null) return;

        bool hasData = record != null && record.recentRaceEntries != null
                       && record.recentRaceEntries.Count > 0;

        rankChartArea.SetActive(hasData);
        if (noRecordLabel != null)
            noRecordLabel.text = hasData ? "" : Loc.Get("str.ui.char.no_record");
        if (noRecordLabel != null)
            noRecordLabel.gameObject.SetActive(!hasData);

        if (!hasData) return;

        rankChart.RemoveData();

        // X축 카테고리 (오래된순 → 최신순: 리스트 역순)
        var xAxis = rankChart.GetChartComponent<XAxis>();
        if (xAxis != null)
            xAxis.data.Clear();

        // 시리즈 추가
        rankChart.AddSerie<Line>("rank");
        var serie = rankChart.GetSerie(0);
        if (serie != null)
        {
            serie.lineType = LineType.Normal;
            serie.symbol.show = true;
            serie.symbol.type = SymbolType.Circle;
            serie.symbol.size = 6;
            serie.itemStyle.color = new Color(1f, 0.3f, 0.3f);
            serie.lineStyle.color = new Color(0.8f, 0.8f, 0.8f);
            serie.lineStyle.width = 2f;
            serie.label.show = true;
            serie.label.position = LabelStyle.Position.Top;
            serie.label.textStyle.fontSize = 11;
            serie.label.textStyle.color = new Color(1f, 0.3f, 0.3f);
        }

        // 데이터 (역순 = 오래된→최신)
        var entries = record.recentRaceEntries;
        var gs = GameSettings.Instance;
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            var e = entries[i];
            string distStr = "";
            if (e.laps > 0 && gs != null)
                distStr = Loc.Get(gs.GetDistanceKey(e.laps));
            string label = Loc.Get("str.ui.char.rank_label", e.rank, distStr);

            rankChart.AddXAxisData((entries.Count - i).ToString());
            var serieData = rankChart.AddData(0, e.rank);
            if (serieData != null)
                serieData.name = label;
        }

        // 라벨 포맷터 (커스텀 이름 사용)
        if (serie != null)
        {
            serie.label.formatter = "{b}";
        }

        rankChart.RefreshChart();
    }

    // ═══ XCharts: 능력치 레이더차트 ═══

    private void InitRadarChart()
    {
        if (radarChartArea == null) return;

        radarChart = radarChartArea.GetComponent<RadarChart>();
        if (radarChart == null)
            radarChart = radarChartArea.AddComponent<RadarChart>();

        radarChart.Init();
        radarChart.SetSize(radarChartArea.GetComponent<RectTransform>().rect.width,
                           radarChartArea.GetComponent<RectTransform>().rect.height);

        radarChart.RemoveData();
        radarChart.ClearComponentData();

        // 제목 비활성
        var title = radarChart.EnsureChartComponent<Title>();
        title.show = false;

        // 배경 투명
        var bg = radarChart.EnsureChartComponent<Background>();
        bg.show = false;

        // RadarCoord 설정 (6각형)
        var radar = radarChart.EnsureChartComponent<RadarCoord>();
        radar.shape = RadarCoord.Shape.Polygon;
        radar.splitNumber = 4; // 4구역 (5, 10, 15, 20)

        // 6개 축 정의
        string[] statKeys = {
            "str.ui.char.stat.speed", "str.ui.char.stat.power",
            "str.ui.char.stat.brave", "str.ui.char.stat.calm",
            "str.ui.char.stat.endurance", "str.ui.char.stat.luck"
        };
        foreach (string key in statKeys)
        {
            radar.AddIndicator(Loc.Get(key), 0, 20);
        }

        // 인디케이터 라벨 스타일은 RadarCoord의 기본 설정 사용
        // (XCharts 최신 버전에서는 Indicator에 직접 textStyle 없음)
    }

    private void UpdateRadarChart(CharacterData data)
    {
        if (radarChart == null || data == null) return;

        radarChart.RemoveData();

        // 4색 배경 시리즈 (구역 표현)
        Color[] zoneColors = {
            new Color(0f, 0.8f, 0.27f, 0.4f),   // 16~20: #00CC44
            new Color(0.67f, 0.8f, 0f, 0.4f),    // 11~15: #AACC00
            new Color(1f, 0.53f, 0f, 0.3f),       // 6~10:  #FF8800
            new Color(0.8f, 0.2f, 0.2f, 0.3f),    // 1~5:   #CC3333
        };
        float[] zoneValues = { 20f, 15f, 10f, 5f };

        for (int z = 0; z < 4; z++)
        {
            radarChart.AddSerie<Radar>("zone_" + z);
            var zoneSerie = radarChart.GetSerie(z);
            if (zoneSerie != null)
            {
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
        radarChart.AddSerie<Radar>("stats");
        int statIdx = 4;
        var statSerie = radarChart.GetSerie(statIdx);
        if (statSerie != null)
        {
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

        radarChart.RefreshChart();
    }

    // ═══ 슬라이드인 애니메이션 ═══

    private IEnumerator SlideInAnimation()
    {
        RectTransform rt = GetComponent<RectTransform>();
        if (rt == null) yield break;

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
        slideCoroutine = null;
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
