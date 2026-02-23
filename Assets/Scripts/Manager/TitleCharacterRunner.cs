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
    private const float Y_MIN = -4.0f;
    private const float Y_MAX = -2.5f;
    private const int SORT_ORDER_MIN = 0;
    private const int SORT_ORDER_MAX = 3;

    // ══════════════════════════════════════
    //  런타임
    // ══════════════════════════════════════
    private Camera cam;
    private float screenLeftX;
    private float screenRightX;

    private List<RunnerInfo> runners = new List<RunnerInfo>();
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
        List<CharacterData> pool = new List<CharacterData>(allChars);
        ShuffleList(pool);

        yield return new WaitForSeconds(FIRST_SPAWN_DELAY);

        for (int i = 0; i < MAX_CHARACTERS && i < pool.Count; i++)
        {
            SpawnCharacter(pool[i], i);
            spawnedCount++;

            if (i < MAX_CHARACTERS - 1)
            {
                float interval = Random.Range(SPAWN_INTERVAL_MIN, SPAWN_INTERVAL_MAX);
                yield return new WaitForSeconds(interval);
            }
        }

        spawning = false;
        Debug.Log($"[TitleRunner] 스폰 완료: {spawnedCount}캐릭터");
    }

    private void SpawnCharacter(CharacterData charData, int index)
    {
        GameObject prefab = charData.LoadPrefab();
        if (prefab == null)
        {
            Debug.LogWarning($"[TitleRunner] 프리팹 없음: {charData.charName}");
            return;
        }

        // Y 위치: 인덱스 기반 + 약간의 랜덤
        float yPos = Mathf.Lerp(Y_MIN, Y_MAX, (float)index / MAX_CHARACTERS)
                     + Random.Range(-0.15f, 0.15f);
        Vector3 spawnPos = new Vector3(screenLeftX + SPAWN_X_OFFSET, yPos, 0f);

        GameObject instance = Instantiate(prefab, spawnPos, Quaternion.identity, transform);
        instance.name = $"TitleRunner_{charData.charName}";

        // RacerController 비활성화
        var rc = instance.GetComponent<RacerController>();
        if (rc != null) rc.enabled = false;

        // Animator "Run" 트리거
        var anim = instance.GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.SetTrigger("Run");
        }

        // SpriteRenderer sortingOrder 설정
        int sortOrder = SORT_ORDER_MIN + (index % (SORT_ORDER_MAX - SORT_ORDER_MIN + 1));
        var spriteRenderers = instance.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in spriteRenderers)
        {
            sr.sortingOrder = sortOrder;
        }

        float speed = BASE_SPEED + Random.Range(-SPEED_VARIATION, SPEED_VARIATION);

        var info = new RunnerInfo
        {
            go = instance,
            speed = speed,
            sortOrder = sortOrder
        };
        runners.Add(info);
    }

    // ══════════════════════════════════════
    //  리셋
    // ══════════════════════════════════════
    private void ResetPosition(RunnerInfo r)
    {
        float yPos = Random.Range(Y_MIN, Y_MAX);
        r.go.transform.position = new Vector3(screenLeftX + SPAWN_X_OFFSET, yPos, 0f);
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
    }
}
