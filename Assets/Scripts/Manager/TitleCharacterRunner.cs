using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 타이틀 씬 캐릭터 달리기 연출
/// - CharacterDatabase에서 12캐릭터 순차 스폰
/// - RacerController 비활성화 + Animator "Run" 트리거
/// - 화면 우측 이탈 시 좌측으로 리셋 (재활용)
/// </summary>
public class TitleCharacterRunner : MonoBehaviour
{
    // ══════════════════════════════════════
    //  설정
    // ══════════════════════════════════════
    private const int MAX_CHARACTERS = 12;
    private const float FIRST_SPAWN_DELAY = 1.0f;
    private const float SPAWN_INTERVAL_MIN = 0.8f;
    private const float SPAWN_INTERVAL_MAX = 1.5f;
    private const float BASE_SPEED = 2.5f;
    private const float SPEED_VARIATION = 1.0f;
    private const float SPAWN_X_OFFSET = -2.0f;    // 화면 왼쪽 밖
    private const float DESPAWN_X_OFFSET = 2.0f;    // 화면 오른쪽 밖
    private const float RUNNER_Y = -4.8f;            // ★ 캐릭터 Y좌표 (이 값만 수정하면 됨)
    private const float RUNNER_SCALE = 1.5f;        // ★ 캐릭터 크기 배율
    private const int SORT_ORDER_MIN = 0;
    private const int SORT_ORDER_MAX = 3;

    // ══════════════════════════════════════
    //  런타임
    // ══════════════════════════════════════
    private Camera cam;
    private float screenLeftX;
    private float screenRightX;

    private List<RunnerInfo> runners = new List<RunnerInfo>();
    private List<RunnerInfo> pool = new List<RunnerInfo>();   // 프리로드된 비활성 인스턴스
    private int spawnedCount = 0;
    private bool spawning = false;

    private class RunnerInfo
    {
        public GameObject go;
        public float speed;
        public int sortOrder;
    }

    // ══════════════════════════════════════
    //  초기화
    // ══════════════════════════════════════
    public void Begin(Camera mainCam)
    {
        cam = mainCam;
        if (cam == null) return;

        // 화면 경계 계산
        float camH = cam.orthographicSize * 2f;
        float camW = camH * cam.aspect;
        screenLeftX = cam.transform.position.x - camW / 2f;
        screenRightX = cam.transform.position.x + camW / 2f;

        StartCoroutine(SpawnSequence());
    }

    private void Update()
    {
        // 캐릭터 이동 + 화면 밖 리셋
        for (int i = 0; i < runners.Count; i++)
        {
            var r = runners[i];
            if (r.go == null) continue;

            r.go.transform.Translate(Vector3.right * r.speed * Time.deltaTime);

            // 화면 우측 이탈 → 좌측으로 리셋
            if (r.go.transform.position.x > screenRightX + DESPAWN_X_OFFSET)
            {
                ResetPosition(r);
            }
        }
    }

    // ══════════════════════════════════════
    //  스폰
    // ══════════════════════════════════════
    private IEnumerator SpawnSequence()
    {
        spawning = true;

        // CharacterDatabase 대기
        float waitTime = 0f;
        while (CharacterDatabase.Instance == null && waitTime < 3f)
        {
            yield return null;
            waitTime += Time.deltaTime;
        }

        if (CharacterDatabase.Instance == null)
        {
            Debug.LogWarning("[TitleRunner] CharacterDatabase 없음 — 캐릭터 스폰 스킵");
            yield break;
        }

        var allChars = CharacterDatabase.Instance.AllCharacters;
        if (allChars == null || allChars.Count == 0)
        {
            Debug.LogWarning("[TitleRunner] 캐릭터 데이터 없음");
            yield break;
        }

        // 랜덤 셔플
        List<CharacterData> charPool = new List<CharacterData>(allChars);
        ShuffleList(charPool);

        // ── 프리로드: 대기 시간 동안 전부 미리 생성 (비활성) ──
        int preloadCount = Mathf.Min(MAX_CHARACTERS, charPool.Count);
        for (int i = 0; i < preloadCount; i++)
        {
            var info = PreloadCharacter(charPool[i], i);
            if (info != null) pool.Add(info);
            // 프레임 분산: 한 프레임에 1개씩만 생성
            yield return null;
        }

        // 남은 대기 시간 소화
        yield return new WaitForSeconds(Mathf.Max(0f, FIRST_SPAWN_DELAY - preloadCount * 0.02f));

        // ── 순차 활성화: 이미 생성된 인스턴스를 켜기만 함 ──
        for (int i = 0; i < pool.Count; i++)
        {
            ActivateRunner(pool[i]);
            spawnedCount++;

            if (i < pool.Count - 1)
            {
                float interval = Random.Range(SPAWN_INTERVAL_MIN, SPAWN_INTERVAL_MAX);
                yield return new WaitForSeconds(interval);
            }
        }

        spawning = false;
        Debug.Log($"[TitleRunner] 스폰 완료: {spawnedCount}캐릭터 (프리로드)");
    }

    /// <summary>프리팹 로드 + Instantiate (비활성 상태로 화면 밖에 생성)</summary>
    private RunnerInfo PreloadCharacter(CharacterData charData, int index)
    {
        GameObject prefab = charData.LoadPrefab();
        if (prefab == null)
        {
            Debug.LogWarning($"[TitleRunner] 프리팹 없음: {charData.charName}");
            return null;
        }

        Vector3 hidePos = new Vector3(screenLeftX + SPAWN_X_OFFSET, RUNNER_Y, 0f);
        GameObject instance = Instantiate(prefab, hidePos, Quaternion.identity, transform);
        instance.name = $"TitleRunner_{charData.charName}";

        // RacerController 비활성화
        var rc = instance.GetComponent<RacerController>();
        if (rc != null) rc.enabled = false;

        // 좌우 반전 + 스케일
        Vector3 ls = instance.transform.localScale;
        instance.transform.localScale = new Vector3(
            -Mathf.Abs(ls.x) * RUNNER_SCALE,
            ls.y * RUNNER_SCALE,
            ls.z);

        // SpriteRenderer sortingOrder
        int sortOrder = SORT_ORDER_MIN + (index % (SORT_ORDER_MAX - SORT_ORDER_MIN + 1));
        var spriteRenderers = instance.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in spriteRenderers) sr.sortingOrder = sortOrder;

        // 비활성 상태로 대기
        instance.SetActive(false);

        return new RunnerInfo
        {
            go = instance,
            speed = BASE_SPEED + Random.Range(-SPEED_VARIATION, SPEED_VARIATION),
            sortOrder = sortOrder
        };
    }

    /// <summary>프리로드된 캐릭터를 활성화하고 러너 목록에 추가</summary>
    private void ActivateRunner(RunnerInfo info)
    {
        info.go.SetActive(true);

        // Animator "Run" 트리거 (활성화 후 호출해야 작동)
        var anim = info.go.GetComponentInChildren<Animator>();
        if (anim != null) anim.SetTrigger("Run");

        runners.Add(info);
    }

    // ══════════════════════════════════════
    //  리셋
    // ══════════════════════════════════════
    private void ResetPosition(RunnerInfo r)
    {
        r.go.transform.position = new Vector3(screenLeftX + SPAWN_X_OFFSET, RUNNER_Y, 0f);
        r.speed = BASE_SPEED + Random.Range(-SPEED_VARIATION, SPEED_VARIATION);
    }

    // ══════════════════════════════════════
    //  유틸
    // ══════════════════════════════════════
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    public void StopAll()
    {
        StopAllCoroutines();
        foreach (var r in runners)
        {
            if (r.go != null) Destroy(r.go);
        }
        runners.Clear();

        // 아직 활성화 안 된 프리로드 인스턴스도 정리
        foreach (var p in pool)
        {
            if (p.go != null) Destroy(p.go);
        }
        pool.Clear();
    }
}
