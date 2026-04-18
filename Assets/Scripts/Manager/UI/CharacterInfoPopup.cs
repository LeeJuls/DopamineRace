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
    private AspectRatioFitter illustrationFitter; // 원본 비율 유지 크롭용
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
    private List<double> radarTargetValues; // 스탯 애니메이션 목표값
    private const int RADAR_STAT_SERIE_INDEX = 4; // zone 0~3 + stat 4

    // ═══ 상태 ═══
    private bool isInitialized;
    private string currentCharId;
    private Coroutine showCoroutine;
    private Vector2 targetPosition;

    // 코루틴 전달용 (Show → ShowSequence)
    private CharacterData pendingData;
    private CharacterRecord pendingRecord;
    private TrackInfo pendingTrackInfo;

    /// <summary>현재 표시 중인 캐릭터 UID</summary>
    public string CurrentCharId => currentCharId;

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
            charTypeLabel = FindText("Layout2_Left/CharTypeLabel/Text");

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
                shortDistRanks = FindText(shortRow, "RanksScroll/ShortDistRanks");
            }

            Transform midRow = layout1.Find("MidDistRow");
            if (midRow != null)
            {
                midDistLabel = FindText(midRow, "MidDistLabel");
                midDistRanks = FindText(midRow, "RanksScroll/MidDistRanks");
            }

            Transform longRow = layout1.Find("LongDistRow");
            if (longRow != null)
            {
                longDistLabel = FindText(longRow, "LongDistLabel");
                longDistRanks = FindText(longRow, "RanksScroll/LongDistRanks");
            }
        }

        // Layout2_Left
        Transform layout2Left = transform.Find("Layout2_Left");
        if (layout2Left != null)
        {
            // IllustrationMask/Illustration 우선 탐색, 구버전(직속) 호환
            Transform illustObj = layout2Left.Find("IllustrationMask/Illustration");
            if (illustObj == null) illustObj = layout2Left.Find("Illustration");
            if (illustObj != null)
            {
                illustration = illustObj.GetComponent<Image>();
                illustrationFitter = illustObj.GetComponent<AspectRatioFitter>();
            }

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
    public void Show(CharacterData data, PopularityInfo info, CharacterRecord record, TrackInfo trackInfo = null)
    {
        if (!isInitialized) Init();
        if (data == null) return;

        currentCharId = data.charId;

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
                // 원본 비율로 AspectRatioFitter 갱신 → 높이에 맞춰 가로 자동 결정
                if (illustrationFitter != null)
                    illustrationFitter.aspectRatio = spr.texture.width / (float)spr.texture.height;
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

        // 4) 스킬 설명 — SkillRegistry 경유 (액티브 + 패시브, 다국어)
        if (skillDescLabel != null)
        {
            var sb = new System.Text.StringBuilder();

            // ── 액티브 스킬 (CharacterData.charAbility = skillKey) ──
            if (!string.IsNullOrEmpty(data.charAbility) && !data.charAbility.Contains(":"))
            {
                string aName = SkillRegistry.GetDisplayName(data.charAbility);
                string aDesc = SkillRegistry.GetDescription(data.charAbility);
                if (!string.IsNullOrEmpty(aName) && aName != data.charAbility)
                {
                    sb.Append("<b>⚔ ").Append(aName).Append("</b>");
                    if (!string.IsNullOrEmpty(aDesc)) sb.Append("\n").Append(aDesc);
                }
            }

            // ── 패시브 스킬 (V4에서 charPassive = skillKey 조회) ──
            var v4 = CharacterDatabaseV4.FindById(data.charId);
            if (v4 != null && !string.IsNullOrEmpty(v4.charPassive) && !v4.charPassive.Contains(":"))
            {
                string pName = SkillRegistry.GetDisplayName(v4.charPassive);
                string pDesc = SkillRegistry.GetDescription(v4.charPassive);
                if (!string.IsNullOrEmpty(pName) && pName != v4.charPassive)
                {
                    if (sb.Length > 0) sb.Append("\n\n");
                    sb.Append("<b>✨ ").Append(pName).Append("</b>");
                    if (!string.IsNullOrEmpty(pDesc)) sb.Append("\n").Append(pDesc);
                }
            }

            // 폴백: 스킬 미연결 캐릭터는 기존 description 키
            if (sb.Length == 0)
            {
                string fallbackKey = !string.IsNullOrEmpty(data.charSkillDesc)
                    ? data.charSkillDesc
                    : data.charName.Replace(".name", ".ability");
                sb.Append(Loc.Get(fallbackKey));
            }

            skillDescLabel.text = sb.ToString();
        }

        // 5) 코루틴 전달용 저장
        pendingData = data;
        pendingRecord = record;
        pendingTrackInfo = trackInfo;

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
        currentCharId = null;
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
        UpdateRadarChart(pendingData, pendingTrackInfo);
        UpdateRecentRecords(pendingRecord);

        // ── 슬라이드인 + 레이더차트 스탯 선 애니메이션 (동시 진행) ──
        RectTransform rt = GetComponent<RectTransform>();
        Vector2 startPos = targetPosition + new Vector2(0, -200);
        if (rt != null) rt.anchoredPosition = startPos;

        float elapsed = 0f;
        float slideDuration = 0.25f;
        float chartDuration = 0.35f; // 스탯 선은 약간 더 길게
        float totalDuration = Mathf.Max(slideDuration, chartDuration);

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;

            // 슬라이드인
            if (rt != null && elapsed <= slideDuration)
            {
                float st = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);
                rt.anchoredPosition = Vector2.Lerp(startPos, targetPosition, st);
            }

            // 레이더차트 스탯 선: 0 → 실제값 보간
            if (radarChart != null && radarTargetValues != null)
            {
                float ct = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / chartDuration));
                for (int i = 0; i < radarTargetValues.Count; i++)
                {
                    double current = radarTargetValues[i] * ct;
                    radarChart.UpdateData(RADAR_STAT_SERIE_INDEX, 0, i, current);
                }
                radarChart.RefreshChart();
            }

            yield return null;
        }

        // 최종값 확정
        if (rt != null) rt.anchoredPosition = targetPosition;
        if (radarChart != null && radarTargetValues != null)
        {
            for (int i = 0; i < radarTargetValues.Count; i++)
                radarChart.UpdateData(RADAR_STAT_SERIE_INDEX, 0, i, radarTargetValues[i]);
            radarChart.RefreshChart();
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

        // ★ XCharts theme-level 폰트 설정 (textStyle.font보다 확실)
        // AddTextObject()에서 textStyle.font==null이면 theme.font를 사용하므로
        // theme.common.font를 설정하면 모든 라벨에 자동 적용됨
        Font chartFont = FontHelper.GetMainFont();
        if (chartFont != null)
        {
            radarChart.theme.common.font = chartFont;
        }

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
        radar.axisName.labelStyle.textStyle.fontSize = 28; // 인디케이터 레이블 폰트 크기
        radar.axisName.labelStyle.textStyle.color = Color.black; // 레이블 색상

        // 반지름: 비율 기반 + 절대 상한 (과대 렌더링 방지)
        RectTransform chartRt = radarChartArea.GetComponent<RectTransform>();
        float w = chartRt.rect.width > 0 ? chartRt.rect.width : 200f;
        float h = chartRt.rect.height > 0 ? chartRt.rect.height : 200f;
        float minDim = Mathf.Min(w, h);
        // 영역의 42%를 반지름으로 사용하되, 최소 25px ~ 최대 110px
        float radius = Mathf.Clamp(minDim * 0.42f, 25f, 110f);
        radar.radius = radius;

        Debug.Log($"[CharInfoPopup] InitRadarChart rect={w:F0}x{h:F0}, radius={radius:F0}");

        radarChart.RefreshChart();
    }

    private void UpdateRadarChart(CharacterData data, TrackInfo trackInfo = null)
    {
        if (radarChart == null || data == null)
        {
            Debug.LogWarning($"[CharInfoPopup] UpdateRadarChart skip — chart={radarChart != null}, data={data != null}");
            return;
        }

        radarChart.RemoveData();

        // ★ RemoveData() 후 theme 폰트 재확인 (혹시 초기화될 수 있으므로)
        Font chartFont = FontHelper.GetMainFont();
        if (chartFont != null)
        {
            radarChart.theme.common.font = chartFont;
        }

        // RemoveData()가 인디케이터도 삭제하므로 매번 재설정
        var radar = radarChart.GetChartComponent<RadarCoord>();
        bool useV4 = GameSettings.Instance != null && GameSettings.Instance.useV4RaceSystem;
        if (radar != null)
        {
            radar.indicatorList.Clear();

            string[] statKeys;
            if (useV4)
            {
                statKeys = new string[] {
                    "str.ui.char.stat.speed", "str.ui.char.stat.accel",
                    "str.ui.char.stat.stamina", "str.ui.char.stat.power",
                    "str.ui.char.stat.intelligence", "str.ui.char.stat.luck"
                };
            }
            else
            {
                statKeys = new string[] {
                    "str.ui.char.stat.speed", "str.ui.char.stat.power",
                    "str.ui.char.stat.brave", "str.ui.char.stat.calm",
                    "str.ui.char.stat.endurance", "str.ui.char.stat.luck"
                };
            }

            // Phase 5: 트랙 보너스 스탯 하이라이트 (노란색 ★)
            // TrackDB bonus 필드 > 0인 스탯 자동 노란색
            bool[] highlights = new bool[6];
            if (!useV4 && trackInfo != null)
            {
                highlights[1] = trackInfo.powerSpeedBonus > 0f;  // power
                highlights[2] = trackInfo.braveSpeedBonus > 0f;  // brave
            }

            for (int i = 0; i < statKeys.Length; i++)
            {
                string label = Loc.Get(statKeys[i]);
                if (highlights[i])
                    label = "<color=#FFD700>" + label + "★</color>";
                radar.AddIndicator(label, 0, 20);
            }

            // RemoveData() 이후 axisName 스타일 재적용 (초기화 방지)
            radar.axisName.labelStyle.textStyle.fontSize = 28;
            radar.axisName.labelStyle.textStyle.color = Color.black;
        }

        // 4색 배경 시리즈 (구역 표현: 큰 값부터 → 작은 값 위에 겹침)
        Color[] zoneColors = {
            new Color(0f, 0.6f, 0.2f, 0.75f),    // 16~20: 진한 녹색
            new Color(0.55f, 0.65f, 0f, 0.75f),  // 11~15: 진한 연두
            new Color(0.85f, 0.4f, 0f, 0.65f),   // 6~10:  진한 주황
            new Color(0.65f, 0.1f, 0.1f, 0.65f), // 1~5:   진한 빨강
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
                zoneSerie.animation.enable = false; // 배경 즉시 표시
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
            statSerie.areaStyle.color = new Color(0f, 0f, 0f, 0.8f); // 80% 반투명 검은색 채움
            statSerie.lineStyle.show = true;
            statSerie.lineStyle.color = Color.white;
            statSerie.lineStyle.width = 2f;
            statSerie.symbol.show = true;
            statSerie.symbol.type = SymbolType.Circle;
            statSerie.symbol.size = 4;
            statSerie.symbol.color = Color.white;
            statSerie.label.show = false;
            statSerie.animation.enable = false; // 코루틴으로 직접 애니메이션
        }

        List<double> statValues;
        if (useV4)
        {
            var v4 = CharacterDatabaseV4.FindById(data.charId);
            if (v4 != null)
            {
                statValues = new List<double>
                {
                    v4.v4Speed, v4.v4Accel, v4.v4Stamina,
                    v4.v4Power, v4.v4Intelligence, v4.v4Luck
                };
            }
            else
            {
                statValues = new List<double> { 10, 10, 10, 10, 10, 10 };
            }
        }
        else
        {
            statValues = new List<double>
            {
                data.charBaseSpeed, data.charBasePower,
                data.charBaseBrave, data.charBaseCalm,
                data.charBaseEndurance, data.charBaseLuck
            };
        }
        // 스탯 목표값 저장 → 실제 데이터는 0으로 시작 (애니메이션용)
        radarTargetValues = new List<double>(statValues);
        var zeroValues = new List<double>();
        for (int i = 0; i < statValues.Count; i++) zeroValues.Add(0);
        radarChart.AddData(statIdx, zeroValues);
        Debug.Log($"[CharInfoPopup] UpdateRadarChart: stats=[{string.Join(",", statValues)}], series={radarChart.series.Count}");

        radarChart.RefreshChart();
    }

    // ═══ 최근 경기기록 텍스트 ═══

    private const int MAX_DIST_DISPLAY = 10;

    /// <summary>
    /// 최근 경기기록을 거리별(단/중/장)로 분류하여 텍스트 갱신
    /// </summary>
    private void UpdateRecentRecords(CharacterRecord record)
    {
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

        // 거리별 독립 리스트에서 최대 MAX_DIST_DISPLAY개 추출
        List<int> shortRanks = Slice(record != null ? record.recentShortRanks : null);
        List<int> midRanks   = Slice(record != null ? record.recentMidRanks   : null);
        List<int> longRanks  = Slice(record != null ? record.recentLongRanks  : null);

        // 텍스트 갱신
        if (shortDistRanks != null)
            shortDistRanks.text = FormatRankList(shortRanks);
        if (midDistRanks != null)
            midDistRanks.text = FormatRankList(midRanks);
        if (longDistRanks != null)
            longDistRanks.text = FormatRankList(longRanks);
    }

    private List<int> Slice(List<int> src)
    {
        var result = new List<int>();
        if (src == null) return result;
        for (int i = 0; i < src.Count && i < MAX_DIST_DISPLAY; i++)
            result.Add(src[i]);
        return result;
    }

    /// <summary>순위 리스트를 rich text 색상 코딩 문자열로 변환</summary>
    private string FormatRankList(List<int> ranks)
    {
        if (ranks.Count == 0)
            return "<color=#000000>" + Loc.Get("str.ui.char.no_dist_record") + "</color>";

        var parts = new List<string>();
        foreach (int rank in ranks)
            parts.Add(ColoredRank(rank));
        return string.Join(" ", parts.ToArray());
    }

    /// <summary>순위에 따른 rich text 색상 적용</summary>
    private string ColoredRank(int rank)
    {
        string rankText = Loc.GetRank(rank);
        switch (rank)
        {
            case 1:  return "<color=#FFD700>" + rankText + "</color>";
            case 2:  return "<color=#C0C0C0>" + rankText + "</color>";
            case 3:  return "<color=#CD7F32>" + rankText + "</color>";
            default: return "<color=#000000>" + rankText + "</color>";
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
