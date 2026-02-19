using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 레이스 매니저: 웨이포인트 생성, 레이서 스폰, 레이스 진행, 순위 관리
/// 
/// 의존:
///   - CharacterDatabase: 선발된 캐릭터 목록
///   - TrackPathData: 웨이포인트 JSON
///   - SpawnPositionData: 출발 위치 JSON
///   - GameManager: 상태 전환 이벤트
///   - GameSettings / GameConstants: 설정값
///   
/// 자동 부착:
///   - CollisionSystem (A-3)
///   - RaceDebugOverlay (디버그)
/// </summary>
public class RaceManager : MonoBehaviour
{
    public static RaceManager Instance { get; private set; }

    // ═══ 트랙 중심 (레이서 레인 오프셋 계산용) ═══
    public static Vector3 TrackCenter { get; private set; } = new Vector3(2.5f, 0f, 0f);

    // ═══ 레이서 목록 ═══
    private List<RacerController> racers = new List<RacerController>();
    public List<RacerController> Racers => racers;

    // ═══ 웨이포인트 ═══
    private GameObject waypointParent;
    private List<Transform> waypoints = new List<Transform>();

    // ═══ 레이스 상태 ═══
    private bool raceActive = false;
    public bool RaceActive => raceActive;

    private int currentLaps = 1;
    public int CurrentLaps => currentLaps;

    private int finishCount = 0;
    private List<RankingEntry> finalRankings = new List<RankingEntry>();

    // ═══ 컴포넌트 참조 ═══
    private CollisionSystem collisionSystem;
    private RaceDebugOverlay debugOverlay;

    // ══════════════════════════════════════
    //  순위 엔트리
    // ══════════════════════════════════════

    [System.Serializable]
    public class RankingEntry
    {
        public int rank;
        public string racerName;
        public int racerIndex;
    }

    // ══════════════════════════════════════
    //  초기화
    // ══════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // 충돌 시스템 부착
        collisionSystem = gameObject.AddComponent<CollisionSystem>();

        // 디버그 오버레이 부착
        if (GameSettings.Instance.enableRaceDebug)
        {
            debugOverlay = gameObject.AddComponent<RaceDebugOverlay>();
        }
    }

    private void Start()
    {
        // GameManager 이벤트 구독
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnRaceStart += OnRaceStart;
            GameManager.Instance.OnStateChanged += OnGameStateChanged;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnRaceStart -= OnRaceStart;
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;
        }

        // ★ 레이서 이벤트 일괄 해제 (비정상 종료 시 누수 방지)
        foreach (var r in racers)
            if (r != null) r.OnFinished -= OnRacerFinished;
    }

    // ══════════════════════════════════════
    //  웨이포인트 생성/재로드
    // ══════════════════════════════════════

    /// <summary>
    /// 현재 트랙에 맞는 웨이포인트 재로드
    /// </summary>
    public void ReloadWaypoints()
    {
        string trackId = null;
        if (TrackDatabase.Instance != null && TrackDatabase.Instance.CurrentTrackInfo != null)
            trackId = TrackDatabase.Instance.CurrentTrackInfo.trackId;

        CreateWaypoints(trackId);

        // 기존 레이서에게 새 웨이포인트 전달
        foreach (var racer in racers)
        {
            racer.Initialize(racer.RacerIndex, waypoints);
        }

        Debug.Log("[RaceManager] 웨이포인트 재로드 완료 (" + waypoints.Count + "개)");
    }

    private void CreateWaypoints(string trackId = null)
    {
        // 기존 웨이포인트 제거
        if (waypointParent != null)
            Destroy(waypointParent);

        waypointParent = new GameObject("Waypoints");
        waypoints.Clear();

        var data = TrackPathData.Load(trackId);

        // 트랙 중심 계산
        float sumX = 0f, sumY = 0f;
        for (int i = 0; i < data.Count; i++)
        {
            Vector2 p = data.GetPoint(i);
            sumX += p.x;
            sumY += p.y;
        }
        if (data.Count > 0)
            TrackCenter = new Vector3(sumX / data.Count, sumY / data.Count, 0f);

        for (int i = 0; i < data.Count; i++)
        {
            Vector2 p = data.GetPoint(i);
            GameObject wp = new GameObject("WP_" + i);
            wp.transform.SetParent(waypointParent.transform);
            wp.transform.position = new Vector3(p.x, p.y, 0f);
            waypoints.Add(wp.transform);
        }
    }

    // ══════════════════════════════════════
    //  레이서 스폰
    // ══════════════════════════════════════

    /// <summary>
    /// 선발된 캐릭터로 레이서 재생성
    /// </summary>
    public void RespawnRacers()
    {
        // 기존 레이서 제거
        foreach (var racer in racers)
        {
            if (racer != null)
                Destroy(racer.gameObject);
        }
        racers.Clear();

        // 웨이포인트 생성 (없으면)
        if (waypoints.Count == 0)
        {
            string trackId = null;
            if (TrackDatabase.Instance != null && TrackDatabase.Instance.CurrentTrackInfo != null)
                trackId = TrackDatabase.Instance.CurrentTrackInfo.trackId;
            CreateWaypoints(trackId);
        }

        // 출발 위치 로드
        var spawnData = SpawnPositionData.Load();
        int racerCount = GameConstants.RACER_COUNT;

        var db = CharacterDatabase.Instance;

        for (int i = 0; i < racerCount; i++)
        {
            // 출발 위치
            Vector3 spawnPos;
            if (i < spawnData.Count)
            {
                Vector2 sp = spawnData.GetPoint(i);
                spawnPos = new Vector3(sp.x, sp.y, 0f);
            }
            else
            {
                spawnPos = new Vector3(6f - i * 0.3f, 1.5f - i * 0.2f, 0f);
            }

            // 캐릭터 프리팹 로드 시도
            GameObject racerObj = null;
            CharacterData charData = null;

            if (db != null && i < db.SelectedCharacters.Count)
            {
                charData = db.SelectedCharacters[i];
                GameObject prefab = charData.LoadPrefab();
                if (prefab != null)
                {
                    racerObj = Instantiate(prefab);
                    racerObj.name = "Racer_" + i + "_" + charData.charName;
                }
            }

            // 프리팹 없으면 기본 오브젝트
            if (racerObj == null)
            {
                racerObj = CreateDefaultRacer(i);
            }

            racerObj.transform.position = spawnPos;

            // RacerController 추가/설정
            RacerController rc = racerObj.GetComponent<RacerController>();
            if (rc == null)
                rc = racerObj.AddComponent<RacerController>();

            rc.Initialize(i, waypoints);

            // 캐릭터 스탯 주입
            if (charData != null)
            {
                rc.SetCharacterData(charData);
                rc.SetupAttackModel();  // ★ 공격 프리팹 미리 로드
            }

            // 레이스 라벨 (번호 표시)
            CreateRaceLabel(racerObj.transform, i);

            rc.ConfirmSpawnPosition();
            racers.Add(rc);
        }

        Debug.Log("[RaceManager] 레이서 " + racerCount + "명 스폰 완료");
    }

    /// <summary>
    /// 프리팹 없을 때 기본 레이서 생성
    /// </summary>
    private GameObject CreateDefaultRacer(int index)
    {
        GameObject obj = new GameObject("Racer_" + index);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        Texture2D tex = new Texture2D(16, 16);
        Color c = GameConstants.RACER_COLORS[index % GameConstants.RACER_COLORS.Length];
        for (int x = 0; x < 16; x++)
            for (int y = 0; y < 16; y++)
                tex.SetPixel(x, y, c);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 32f);
        sr.sortingOrder = 10 + index;

        return obj;
    }

    /// <summary>
    /// 레이서 위에 번호 라벨 생성
    /// </summary>
    private void CreateRaceLabel(Transform parent, int index)
    {
        // 이미 있으면 스킵
        if (parent.Find("RaceLabel") != null) return;

        var gs = GameSettings.Instance;

        GameObject labelObj = new GameObject("RaceLabel");
        labelObj.transform.SetParent(parent);
        labelObj.transform.localPosition = new Vector3(0, gs.labelHeight, 0);

        TextMesh tm = labelObj.AddComponent<TextMesh>();
        tm.text = (index + 1).ToString();
        tm.characterSize = gs.labelSize;
        tm.fontSize = 48;
        tm.fontStyle = FontStyle.Bold;
        tm.color = Color.white;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;

        // 폰트 적용
        FontHelper.ApplyToTextMesh(tm, 40);

        labelObj.SetActive(false);
    }

    // ══════════════════════════════════════
    //  레이스 제어
    // ══════════════════════════════════════

    /// <summary>
    /// 바퀴 수 설정
    /// </summary>
    public void SetLaps(int laps)
    {
        currentLaps = Mathf.Max(1, laps);
    }

    /// <summary>
    /// 레이스 시작 (GameManager.OnRaceStart 이벤트)
    /// </summary>
    private void OnRaceStart()
    {
        raceActive = true;
        finishCount = 0;
        finalRankings.Clear();

        // 디버그 오버레이 라운드 시작
        int round = GameManager.Instance != null ? GameManager.Instance.CurrentRound : 1;
        if (debugOverlay != null)
            debugOverlay.StartRound(round);

        // 충돌 시스템 리셋
        if (collisionSystem != null)
            collisionSystem.ClearAll();

        // 모든 레이서 레이싱 시작
        foreach (var racer in racers)
        {
            racer.OnFinished += OnRacerFinished;
            racer.StartRacing();
        }

        Debug.Log("[RaceManager] 레이스 시작! " + racers.Count + "명, " + currentLaps + "바퀴");
    }

    /// <summary>
    /// 레이서 완주 콜백
    /// </summary>
    private void OnRacerFinished(RacerController racer)
    {
        finishCount++;
        racer.FinishOrder = finishCount;
        racer.OnFinished -= OnRacerFinished;

        string name = racer.CharData != null ? racer.CharData.charName
            : GameConstants.RACER_NAMES[racer.RacerIndex];

        finalRankings.Add(new RankingEntry
        {
            rank = finishCount,
            racerName = name,
            racerIndex = racer.RacerIndex
        });

        Debug.Log("[RaceManager] " + finishCount + "착: " + name);

        // 전원 완주 → 결과 화면
        if (finishCount >= racers.Count)
        {
            raceActive = false;

            // 디버그 리포트 저장
            int round = GameManager.Instance != null ? GameManager.Instance.CurrentRound : 1;
            if (debugOverlay != null)
                debugOverlay.SaveRoundReport(round, finalRankings);

            // 이벤트 정리
            foreach (var r in racers)
                r.OnFinished -= OnRacerFinished;

            GameManager.Instance?.ChangeState(GameManager.GameState.Result);
        }
    }

    /// <summary>
    /// 게임 상태 변경 시 처리
    /// </summary>
    private void OnGameStateChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.Betting)
        {
            // 새 게임 시작 시 디버그 로그 초기화
            if (GameManager.Instance != null && GameManager.Instance.CurrentRound == 1)
            {
                if (debugOverlay != null)
                    debugOverlay.ClearAllLogs();
            }
        }
    }

    // ══════════════════════════════════════
    //  레이스 리셋 (다음 라운드)
    // ══════════════════════════════════════

    /// <summary>
    /// 다음 라운드를 위한 리셋 (레이서 위치 초기화)
    /// </summary>
    public void ResetRace()
    {
        raceActive = false;
        finishCount = 0;
        finalRankings.Clear();

        // 출발 위치 로드
        var spawnData = SpawnPositionData.Load();

        for (int i = 0; i < racers.Count; i++)
        {
            Vector3 spawnPos;
            if (i < spawnData.Count)
            {
                Vector2 sp = spawnData.GetPoint(i);
                spawnPos = new Vector3(sp.x, sp.y, 0f);
            }
            else
            {
                spawnPos = new Vector3(6f - i * 0.3f, 1.5f - i * 0.2f, 0f);
            }

            racers[i].ResetRacer(spawnPos);
            racers[i].OnFinished -= OnRacerFinished;
        }

        // 충돌 시스템 리셋
        if (collisionSystem != null)
            collisionSystem.ClearAll();

        Debug.Log("[RaceManager] 레이스 리셋 완료");
    }

    // ══════════════════════════════════════
    //  순위 조회
    // ══════════════════════════════════════

    /// <summary>
    /// 실시간 순위 (레이싱 중 HUD용)
    /// 진행도 높은 순으로 정렬된 레이서 리스트 반환
    /// </summary>
    public List<RacerController> GetLiveRankings()
    {
        var sorted = new List<RacerController>(racers);
        sorted.Sort((a, b) =>
        {
            // 완주한 레이서 우선 (FinishOrder 순)
            if (a.IsFinished && b.IsFinished)
                return a.FinishOrder.CompareTo(b.FinishOrder);
            if (a.IsFinished) return -1;
            if (b.IsFinished) return 1;

            // 미완주: TotalProgress 높은 순
            return b.TotalProgress.CompareTo(a.TotalProgress);
        });
        return sorted;
    }

    /// <summary>
    /// 최종 순위 (결과 화면용)
    /// </summary>
    public List<RankingEntry> GetFinalRankings()
    {
        // 이미 전원 완주한 경우
        if (finalRankings.Count >= racers.Count)
            return finalRankings;

        // 아직 완주 안 한 레이서도 포함해서 순위 생성
        var result = new List<RankingEntry>(finalRankings);

        // 미완주 레이서를 진행도 순으로 추가
        var unfinished = racers
            .Where(r => !r.IsFinished)
            .OrderByDescending(r => r.TotalProgress)
            .ToList();

        int nextRank = result.Count + 1;
        foreach (var r in unfinished)
        {
            string name = r.CharData != null ? r.CharData.charName
                : GameConstants.RACER_NAMES[r.RacerIndex];

            result.Add(new RankingEntry
            {
                rank = nextRank,
                racerName = name,
                racerIndex = r.RacerIndex
            });
            nextRank++;
        }

        return result;
    }
}