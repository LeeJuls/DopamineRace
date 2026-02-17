#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 스폰 위치별 승률 시뮬레이션
/// Unity 메뉴: Tools > 스폰 위치 승률 테스트
/// </summary>
public class SpawnPositionTest : EditorWindow
{
    private int raceCount = 1000;
    private int racerCount = 8;
    private int totalLaps = 1;
    private float minSpeed = 1f;
    private float maxSpeed = 4f;
    private float speedChangeInterval = 3f;
    private float simTimeStep = 0.1f; // 시뮬 프레임 간격

    private string resultText = "";
    private Vector2 scrollPos;
    private bool isRunning = false;

    [MenuItem("Tools/스폰 위치 승률 테스트")]
    static void ShowWindow()
    {
        GetWindow<SpawnPositionTest>("스폰 승률 테스트");
    }

    private void OnGUI()
    {
        GUILayout.Label("스폰 위치별 승률 시뮬레이션", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        raceCount = EditorGUILayout.IntField("레이스 횟수", raceCount);
        racerCount = EditorGUILayout.IntField("레이서 수", racerCount);
        totalLaps = EditorGUILayout.IntField("바퀴 수", totalLaps);
        minSpeed = EditorGUILayout.FloatField("최소 속도", minSpeed);
        maxSpeed = EditorGUILayout.FloatField("최대 속도", maxSpeed);
        speedChangeInterval = EditorGUILayout.FloatField("속도 변경 간격(초)", speedChangeInterval);

        EditorGUILayout.Space();

        if (!isRunning && GUILayout.Button("시뮬레이션 시작", GUILayout.Height(30)))
        {
            RunSimulation();
        }

        EditorGUILayout.Space();

        if (!string.IsNullOrEmpty(resultText))
        {
            GUILayout.Label("결과:", EditorStyles.boldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(400));
            EditorGUILayout.TextArea(resultText, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }
    }

    private void RunSimulation()
    {
        isRunning = true;

        // spawnPos별 순위 누적: [spawnIdx][rank] = count
        int[,] rankCount = new int[racerCount, racerCount];
        // spawnPos별 1등 횟수
        int[] winCount = new int[racerCount];

        // 스폰 위치 오프셋 계산 (실제 게임과 동일한 로직)
        float[] spawnOffsets = CalcSpawnOffsets(racerCount);

        for (int race = 0; race < raceCount; race++)
        {
            // 스폰 인덱스 셔플 (누가 어디 서는지 랜덤)
            int[] spawnAssign = ShuffleArray(racerCount);

            // 레이스 시뮬레이션
            int[] finishOrder = SimulateRace(spawnAssign, spawnOffsets);

            // 결과 기록 (spawnAssign[racerIdx] = 그 레이서의 스폰 슬롯)
            for (int rank = 0; rank < finishOrder.Length; rank++)
            {
                int racerIdx = finishOrder[rank];
                int spawnIdx = spawnAssign[racerIdx];
                rankCount[spawnIdx, rank]++;
                if (rank == 0) winCount[spawnIdx]++;
            }
        }

        // 결과 출력
        resultText = FormatResults(rankCount, winCount, spawnOffsets);
        isRunning = false;

        Debug.Log("[SpawnTest] " + raceCount + "회 시뮬 완료 (" + racerCount + "명, " + totalLaps + "바퀴)");
        Debug.Log(resultText);
    }

    /// <summary>
    /// 한 레이스 시뮬레이션 → 완주 순서 배열 반환
    /// </summary>
    private int[] SimulateRace(int[] spawnAssign, float[] spawnOffsets)
    {
        int n = racerCount;

        // 각 레이서의 초기 진행도 = 스폰 오프셋 (앞줄일수록 큰 값)
        float[] progress = new float[n];
        float[] speed = new float[n];
        float[] targetSpeed = new float[n];
        float[] speedTimer = new float[n];
        bool[] finished = new bool[n];
        List<int> finishOrder = new List<int>();

        // 1바퀴 = 1.0 진행도, 총 목표 = totalLaps
        float totalDistance = totalLaps;

        for (int i = 0; i < n; i++)
        {
            int spawnIdx = spawnAssign[i];
            progress[i] = spawnOffsets[spawnIdx]; // 스폰 위치에 따른 초기 진행도
            speed[i] = Random.Range(minSpeed, maxSpeed);
            targetSpeed[i] = speed[i];
            speedTimer[i] = Random.Range(0f, speedChangeInterval);
        }

        float maxTime = 300f; // 안전장치
        float time = 0f;

        while (finishOrder.Count < n && time < maxTime)
        {
            time += simTimeStep;

            for (int i = 0; i < n; i++)
            {
                if (finished[i]) continue;

                // 속도 변경 타이머
                speedTimer[i] -= simTimeStep;
                if (speedTimer[i] <= 0f)
                {
                    targetSpeed[i] = Random.Range(minSpeed, maxSpeed);
                    speedTimer[i] = speedChangeInterval;
                }

                // 속도 보간 (실제 게임의 Lerp 시뮬)
                speed[i] = Mathf.Lerp(speed[i], targetSpeed[i], 3f * simTimeStep);

                // 진행도 증가 (트랙 둘레 기준 정규화)
                // 실제 게임에서 speed는 Unity 유닛/초, 트랙 둘레 약 18유닛
                float trackCircumference = 18f;
                progress[i] += (speed[i] * simTimeStep) / trackCircumference;

                // 완주 체크
                if (progress[i] >= totalDistance)
                {
                    finished[i] = true;
                    finishOrder.Add(i);
                }
            }
        }

        // 미완주 처리 (진행도 높은 순)
        if (finishOrder.Count < n)
        {
            var remaining = new List<int>();
            for (int i = 0; i < n; i++)
                if (!finished[i]) remaining.Add(i);
            remaining.Sort((a, b) => progress[b].CompareTo(progress[a]));
            finishOrder.AddRange(remaining);
        }

        return finishOrder.ToArray();
    }

    /// <summary>
    /// 스폰 오프셋 계산 (실제 게임 SpawnPositionData 기반 근사)
    /// 앞줄 = 더 높은 진행도, 뒤 = 낮은 진행도
    /// </summary>
    private float[] CalcSpawnOffsets(int count)
    {
        float[] offsets = new float[count];
        // 실제 게임: 스폰 위치 간격 약 0.02~0.05 바퀴 차이
        // slot 0 = 가장 앞, slot N-1 = 가장 뒤
        float spacing = 0.03f; // 바퀴 기준 간격
        for (int i = 0; i < count; i++)
        {
            offsets[i] = (count - 1 - i) * spacing;
        }
        return offsets;
    }

    private int[] ShuffleArray(int n)
    {
        int[] arr = new int[n];
        for (int i = 0; i < n; i++) arr[i] = i;
        for (int i = n - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int tmp = arr[i]; arr[i] = arr[j]; arr[j] = tmp;
        }
        return arr;
    }

    private string FormatResults(int[,] rankCount, int[] winCount, float[] spawnOffsets)
    {
        string r = "═══════════════════════════════════════════\n";
        r += " 스폰 위치별 승률 (" + raceCount + "회, " + racerCount + "명, " + totalLaps + "바퀴)\n";
        r += "═══════════════════════════════════════════\n\n";

        // 기대 승률
        float expectedWinRate = 100f / racerCount;
        r += "기대 승률 (공정): " + expectedWinRate.ToString("F1") + "%\n\n";

        // 헤더
        r += "스폰  | 오프셋  | 1등 횟수 | 승률     | 편차     | 1등  2등  3등\n";
        r += "──────────────────────────────────────────────────────\n";

        for (int s = 0; s < racerCount; s++)
        {
            float winRate = (float)winCount[s] / raceCount * 100f;
            float deviation = winRate - expectedWinRate;
            string devStr = (deviation >= 0 ? "+" : "") + deviation.ToString("F1") + "%";

            // 상위 3등 비율
            string top3 = "";
            for (int rank = 0; rank < 3 && rank < racerCount; rank++)
            {
                float pct = (float)rankCount[s, rank] / raceCount * 100f;
                top3 += pct.ToString("F1") + "% ";
            }

            r += "슬롯" + s.ToString().PadLeft(2)
                + " | " + spawnOffsets[s].ToString("F3").PadLeft(6)
                + " | " + winCount[s].ToString().PadLeft(7)
                + "  | " + winRate.ToString("F1").PadLeft(5) + "%"
                + "  | " + devStr.PadLeft(7)
                + "  | " + top3 + "\n";
        }

        // 요약
        r += "\n──────────────────────────────────────────────────────\n";
        int maxWinSlot = 0, minWinSlot = 0;
        for (int s = 1; s < racerCount; s++)
        {
            if (winCount[s] > winCount[maxWinSlot]) maxWinSlot = s;
            if (winCount[s] < winCount[minWinSlot]) minWinSlot = s;
        }

        float maxRate = (float)winCount[maxWinSlot] / raceCount * 100f;
        float minRate = (float)winCount[minWinSlot] / raceCount * 100f;
        r += "최고 승률: 슬롯" + maxWinSlot + " (" + maxRate.ToString("F1") + "%)\n";
        r += "최저 승률: 슬롯" + (minWinSlot) + " (" + minRate.ToString("F1") + "%)\n";
        r += "편차 범위: " + (maxRate - minRate).ToString("F1") + "%\n";

        bool needsBalance = (maxRate - minRate) > expectedWinRate * 0.5f;
        r += "\n결론: " + (needsBalance
            ? "⚠️ 스폰 위치에 따른 유의미한 차이 있음 → 보정 필요"
            : "✅ 스폰 위치 영향 적음 → 보정 불필요");

        return r;
    }
}
#endif
