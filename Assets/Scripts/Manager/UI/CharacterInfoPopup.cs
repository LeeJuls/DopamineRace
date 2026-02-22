using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using XCharts.Runtime;
using EasyChart;
using EasyChart.UGUI;

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

    // 차트 영역
    private GameObject rankChartArea;
    private GameObject radarChartArea;
    // EasyChart Lite — 순위 꺾은선 그래프
    private UGUIChartBridge rankChartBridge;
    private ChartProfile rankProfile;
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
        // Bridge/Profile은 유지 — OnDisable이 ChartElement를 정리하고
        // 다음 OnEnable + LateUpdate에서 자동 재생성됨
    }

    /// <summary>
    /// 차트 초기화 → 데이터 갱신 → 슬라이드인 통합 코루틴
    /// EasyChart bridge는 UIDocument → rootVisualElement → ChartElement 순으로 초기화되며
    /// 각 단계가 1프레임씩 필요하므로 ChartElement 가용성을 폴링합니다.
    /// </summary>
    private IEnumerator ShowSequence()
    {
        // ── 1프레임 대기: 중첩 stretch 앵커 RectTransform 크기 확정 ──
        yield return null;

        // ── 차트 컴포넌트 생성 (최초 1회) ──
        if (rankChartBridge == null)
            InitRankChart();
        if (radarChart == null)
            InitRadarChart();

        // 차트 영역 활성화 보장 (이전 Show에서 no-data로 비활성됐을 수 있음)
        // bridge의 OnEnable이 _isInitialized=false로 리셋 → LateUpdate에서 ChartElement 재생성
        if (rankChartArea != null && !rankChartArea.activeSelf)
            rankChartArea.SetActive(true);

        // ── EasyChart bridge ChartElement 가용 대기 ──
        // OnDisable→OnEnable 사이클 후 bridge.LateUpdate()가:
        //   frame N: UIDocument + PanelSettings 생성
        //   frame N+1: rootVisualElement 활성화 → ChartElement 생성
        // 최대 8프레임 폴링 (여유있게)
        int waitFrames = 0;
        while (rankChartBridge != null && rankChartBridge.ChartElement == null && waitFrames < 8)
        {
            yield return null;
            waitFrames++;
        }

        bool chartReady = rankChartBridge != null && rankChartBridge.ChartElement != null;
        Debug.Log($"[CharInfoPopup] ShowSequence: bridge wait={waitFrames}f, " +
                  $"chartReady={chartReady}, bridge={rankChartBridge != null}");

        // ── 데이터 갱신 ──
        UpdateRankChart(pendingRecord);
        UpdateRadarChart(pendingData);

        // ── EasyChart 강제 갱신 (ChartElement가 준비된 경우) ──
        if (chartReady)
        {
            rankChartBridge.Refresh();
            // UI Toolkit 레이아웃 계산 + 렌더 파이프라인 실행을 위해 추가 프레임 대기
            yield return null;
            if (rankChartBridge != null)
                rankChartBridge.Refresh();
        }

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

    // ═══ EasyChart Lite: 순위 꺾은선 그래프 ═══
    // 순위 뒤집기 상수: rank 1(최상위)을 차트 상단에, rank 12를 하단에 표시
    // RANK_FLIP - rank → rank1=12(상단), rank12=1(하단)
    private const int RANK_FLIP = 13;

    private void InitRankChart()
    {
        if (rankChartArea == null)
        {
            Debug.LogWarning("[CharInfoPopup] rankChartArea is null — skip InitRankChart");
            return;
        }

        // Layout1_TopArea에 RectMask2D 추가 (차트 오버플로 클리핑)
        Transform layout1 = rankChartArea.transform.parent;
        if (layout1 != null && layout1.GetComponent<RectMask2D>() == null)
            layout1.gameObject.AddComponent<RectMask2D>();

        // 기존 XCharts LineChart 컴포넌트 제거 (XCharts → EasyChart 전환 시)
        var oldLineChart = rankChartArea.GetComponent<LineChart>();
        if (oldLineChart != null) Destroy(oldLineChart);

        // ── ChartProfile 생성 (런타임 전용) ──
        rankProfile = ScriptableObject.CreateInstance<ChartProfile>();
        rankProfile.hideFlags = HideFlags.DontSave;
        rankProfile.chartName = "RankChart";

        // 차트 크기: RectTransform 실제 크기 기반
        RectTransform chartRt = rankChartArea.GetComponent<RectTransform>();
        float w = chartRt.rect.width > 0 ? chartRt.rect.width : 300f;
        float h = chartRt.rect.height > 0 ? chartRt.rect.height : 200f;
        rankProfile.chartWidth = w;
        rankProfile.chartHeight = h;

        // 패딩 (Left, Right, Top, Bottom)
        rankProfile.padding = new Vector4(15f, 15f, 30f, 10f);
        rankProfile.paddingInitialized = true;

        // 좌표계: Cartesian 2D
        rankProfile.coordinateSystem = CoordinateSystemType.Cartesian2D;
        rankProfile.xAxisId = AxisId.XBottom;
        rankProfile.yAxisId = AxisId.YLeft;
        rankProfile.axisSelectionInitialized = true;
        rankProfile.axisTypeSelectionInitialized = true;

        // 배경: 반투명 어두운 색 (차트 영역 확인용)
        rankProfile.background = new BackgroundSettings
        {
            show = true,
            textureFill = new TextureFillSettings
            {
                color = new Color(0.1f, 0.1f, 0.15f, 0.6f) // 반투명 다크블루
            }
        };

        // 범례 비활성
        rankProfile.legendSettings = new LegendSettings { enabled = false };

        // 그리드: 미세한 라인 (디버깅용)
        rankProfile.cartesianGrid = new CartesianGridSettings
        {
            xGridColor = new Color(0.5f, 0.5f, 0.5f, 0.15f),
            yGridColor = new Color(0.5f, 0.5f, 0.5f, 0.15f)
        };
        rankProfile.gridSettingsInitialized = true;

        // 애니메이션
        rankProfile.animationDuration = 0.3f;

        // ── 축 설정 ──
        rankProfile.axes = new List<AxisConfig>();

        // X축: Category (숨김 — 라벨은 시리즈 데이터 위에 표시)
        var xAxis = new AxisConfig
        {
            id = AxisId.XBottom,
            axisType = AxisType.Category,
            visible = true,
            showLabels = false,
            labels = new List<string>()
        };
        rankProfile.axes.Add(xAxis);

        // Y축: Value (숨김, 고정 범위 0~RANK_FLIP)
        var yAxis = new AxisConfig
        {
            id = AxisId.YLeft,
            axisType = AxisType.Value,
            visible = true,
            showLabels = false,
            autoRangeMin = false,
            autoRangeMax = false,
            minValue = 0f,
            maxValue = RANK_FLIP
        };
        rankProfile.axes.Add(yAxis);

        // ── Line 시리즈 (초기 더미 데이터 포함 — 빈 시리즈는 렌더러 미생성) ──
        var serie = new EasyChart.Serie
        {
            name = "rank",
            type = SerieType.Line,
            settings = new LineSettings
            {
                stroke = new LineStrokeSettings
                {
                    lineType = EasyChart.LineType.Straight,
                    width = 3f,
                    color = new Color(0.7f, 0.7f, 0.7f) // 회색 연결선
                },
                point = new PointSettings
                {
                    show = true,
                    size = 8f,
                    textureFill = new TextureFillSettings
                    {
                        color = new Color(1f, 0.3f, 0.3f) // 빨간 꼭지점
                    }
                },
                area = new AreaFillSettings { show = false }
            },
            labelSettings = new SerieLabelSettings
            {
                enabled = true,
                showName = true,
                color = new Color(1f, 0.3f, 0.3f), // 빨간 라벨
                fontSize = 13,
                position = LabelPosition.Outside,
                offset = new Vector2(0, 5f)
            }
        };

        // 초기 더미 데이터 2포인트 (빈 시리즈는 라인 렌더러를 생성하지 않을 수 있음)
        serie.seriesData = new List<SeriesData>
        {
            new SeriesData { x = 0, value = 6 },
            new SeriesData { x = 1, value = 6 }
        };
        xAxis.labels.Add("A");
        xAxis.labels.Add("B");

        rankProfile.series.Add(serie);

        // Profile 무결성 검증
        rankProfile.EnsureRuntimeData();

        // ── UGUIChartBridge 컴포넌트 추가 (매 Show마다 새로 생성) ──
        rankChartBridge = rankChartArea.AddComponent<UGUIChartBridge>();

        // WorldSpace 모드: RenderTexture → RawImage로 UGUI 안에서 직접 렌더링
        rankChartBridge.RenderMode = ChartRenderMode.WorldSpace;
        rankChartBridge.Profile = rankProfile;

        Debug.Log($"[CharInfoPopup] InitRankChart EasyChart rect={w:F0}x{h:F0}, " +
                  $"seriesData={serie.seriesData.Count}pts, axes={rankProfile.axes.Count}");
    }

    private void UpdateRankChart(CharacterRecord record)
    {
        if (rankProfile == null) return;

        bool hasData = record != null && record.recentRaceEntries != null
                       && record.recentRaceEntries.Count > 0;

        if (rankChartArea != null) rankChartArea.SetActive(hasData);
        if (noRecordLabel != null)
            noRecordLabel.text = hasData ? "" : Loc.Get("str.ui.char.no_record");
        if (noRecordLabel != null)
            noRecordLabel.gameObject.SetActive(!hasData);

        if (!hasData) return;

        // X축 카테고리 라벨 초기화
        AxisConfig xAxis = null;
        if (rankProfile.axes != null)
        {
            for (int a = 0; a < rankProfile.axes.Count; a++)
            {
                if (rankProfile.axes[a] != null && rankProfile.axes[a].id == AxisId.XBottom)
                {
                    xAxis = rankProfile.axes[a];
                    break;
                }
            }
        }
        if (xAxis != null)
            xAxis.labels.Clear();

        // 시리즈 데이터 초기화
        if (rankProfile.series.Count > 0 && rankProfile.series[0].seriesData != null)
            rankProfile.series[0].seriesData.Clear();

        // 데이터 추가 (역순 = 오래된→최신)
        var entries = record.recentRaceEntries;
        var gs = GameSettings.Instance;
        int dataIdx = 0;
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            var e = entries[i];
            string distStr = "";
            if (e.laps > 0 && gs != null)
                distStr = Loc.Get(gs.GetDistanceKey(e.laps));
            string label = Loc.Get("str.ui.char.rank_label", e.rank, distStr);

            // X축 카테고리 라벨 추가
            if (xAxis != null)
                xAxis.labels.Add(label);

            // 시리즈 데이터 추가: RANK_FLIP - rank → rank1=12(상단), rank12=1(하단)
            if (rankProfile.series.Count > 0)
            {
                rankProfile.series[0].seriesData.Add(new SeriesData
                {
                    x = dataIdx,
                    value = RANK_FLIP - e.rank,
                    name = label // 라벨 텍스트 (showName=true 시 표시)
                });
            }
            dataIdx++;
        }

        Debug.Log($"[CharInfoPopup] UpdateRankChart: {entries.Count} entries, " +
                  $"ranks=[{string.Join(",", entries.ConvertAll(e => e.rank))}], " +
                  $"flipped=[{string.Join(",", entries.ConvertAll(e => RANK_FLIP - e.rank))}]");

        // 차트 갱신
        if (rankChartBridge != null)
            rankChartBridge.Refresh();
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
