using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 원격 리더보드 서비스 — 표시(GET /top) + 저장(POST /submit). 코루틴 + UnityWebRequest.
/// DontDestroyOnLoad: Finish→TitleScene 전환에도 제출 코루틴 생존(fire-and-forget).
/// 백엔드 독립: REST 세부는 ===ADAPTER=== region 한 곳. 교체 시 여기만 수정.
/// RemoteEnabled=false 면 모든 호출이 즉시 실패 콜백(네트워크 0) → 호출자가 로컬 폴백.
/// </summary>
public class LeaderboardService : MonoBehaviour
{
    public static LeaderboardService Instance { get; private set; }

    private bool _fetchInFlight;

    /// <summary>원격 호출 가능 여부 — 설정 토글 + URL 존재.</summary>
    public bool RemoteEnabled => LeaderboardConfig.Instance.RemoteEnabled;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ═══════════════════════════════════════════════════════════
    //  공개 API
    // ═══════════════════════════════════════════════════════════

    /// <summary>Top N 조회. 성공 시 onResult(점수내림차순 ≤count), 실패 시 onError(메시지).</summary>
    public void FetchTop(int count, Action<List<LeaderboardEntry>> onResult, Action<string> onError)
    {
        if (!RemoteEnabled) { onError?.Invoke("remote-disabled"); return; }
        StartCoroutine(CoFetch(count, onResult, onError));
    }

    /// <summary>점수 제출(fire-and-forget). nonce로 서버 멱등. onDone(성공여부).</summary>
    public void SubmitScore(LeaderboardEntry entry, string nonce, Action<bool> onDone)
    {
        if (!RemoteEnabled || entry == null) { onDone?.Invoke(false); return; }
        if (string.IsNullOrEmpty(LeaderboardConfig.Instance.writeToken)) { onDone?.Invoke(false); return; }
        StartCoroutine(CoSubmit(entry, nonce, onDone));
    }

    /// <summary>캐시 워밍(best-effort) — 자격판정 로컬 캐시 신선도 확보. 중복 워밍 가드.</summary>
    public void WarmFetch()
    {
        if (!RemoteEnabled || _fetchInFlight) return;
        FetchTop(LeaderboardConfig.Instance.fetchCount, _ => { }, _ => { });
    }

    // ═══════════════════════════════════════════════════════════
    //  코루틴
    // ═══════════════════════════════════════════════════════════

    private IEnumerator CoFetch(int count, Action<List<LeaderboardEntry>> onResult, Action<string> onError)
    {
        _fetchInFlight = true;
        var cfg = LeaderboardConfig.Instance;
        string body = null, err = null;
        bool ok = false;

        using (UnityWebRequest req = Adapter_BuildFetch(cfg, count))
        {
            req.timeout = cfg.timeoutSeconds;
            yield return req.SendWebRequest();
            ok = req.result == UnityWebRequest.Result.Success;
            if (ok) body = req.downloadHandler.text;          // Dispose 전 값 복사
            else err = string.IsNullOrEmpty(req.error) ? "network-error" : req.error;
        }

        _fetchInFlight = false;

        if (!ok) { Debug.LogWarning("[Leaderboard] fetch 실패: " + err); onError?.Invoke(err); yield break; }

        List<LeaderboardEntry> list;
        if (!Adapter_TryParse(body, out list)) { onError?.Invoke("parse-error"); yield break; }

        Normalize(list, count);
        LeaderboardData.WriteThrough(list);                   // 성공 시 로컬 캐시 갱신(빈 응답은 no-op)
        onResult?.Invoke(list);
    }

    private IEnumerator CoSubmit(LeaderboardEntry entry, string nonce, Action<bool> onDone)
    {
        var cfg = LeaderboardConfig.Instance;
        bool ok = false;

        using (UnityWebRequest req = Adapter_BuildSubmit(cfg, entry, nonce))
        {
            req.timeout = cfg.timeoutSeconds;
            yield return req.SendWebRequest();
            ok = req.result == UnityWebRequest.Result.Success;
            if (!ok) Debug.LogWarning("[Leaderboard] submit 실패: " +
                (string.IsNullOrEmpty(req.error) ? "?" : req.error));
        }

        onDone?.Invoke(ok);
    }

    // ═══════════════════════════════════════════════════════════
    //  ===ADAPTER=== (백엔드 교체 시 이 region만 수정)
    //  계약: GET {base}/top?limit=N -> {"entries":[{score,rounds,date,name,summary}]}
    //        POST {base}/submit (X-Write-Token) {"entry":{...},"clientNonce":".."} -> {ok,rank}
    // ═══════════════════════════════════════════════════════════

    [Serializable] private class FetchResponse { public List<LeaderboardEntry> entries = new List<LeaderboardEntry>(); }
    [Serializable] private class SubmitBody { public LeaderboardEntry entry; public string clientNonce; }

    private UnityWebRequest Adapter_BuildFetch(LeaderboardConfig cfg, int count)
    {
        int limit = Mathf.Clamp(count, 1, 100);
        string url = cfg.apiBaseUrl.TrimEnd('/') + "/top?limit=" + limit;
        return UnityWebRequest.Get(url);
    }

    private UnityWebRequest Adapter_BuildSubmit(LeaderboardConfig cfg, LeaderboardEntry entry, string nonce)
    {
        string url = cfg.apiBaseUrl.TrimEnd('/') + "/submit";
        string json = JsonUtility.ToJson(new SubmitBody { entry = entry, clientNonce = nonce });
        var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("X-Write-Token", cfg.writeToken);
        return req;
    }

    private bool Adapter_TryParse(string body, out List<LeaderboardEntry> list)
    {
        list = null;
        if (string.IsNullOrEmpty(body)) return false;
        try
        {
            var resp = JsonUtility.FromJson<FetchResponse>(body);   // 계약이 {entries:[...]} → 최상위 배열 문제 없음
            list = (resp != null && resp.entries != null) ? resp.entries : new List<LeaderboardEntry>();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Leaderboard] 파싱 실패: " + e.Message);
            return false;
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  유틸
    // ═══════════════════════════════════════════════════════════

    /// <summary>서버를 신뢰하되 방어: null 제거 + 점수 내림차순 + count 컷.</summary>
    private void Normalize(List<LeaderboardEntry> list, int count)
    {
        if (list == null) return;
        list.RemoveAll(e => e == null);
        list.Sort((a, b) => b.score.CompareTo(a.score));
        if (count > 0 && list.Count > count) list.RemoveRange(count, list.Count - count);
    }
}
