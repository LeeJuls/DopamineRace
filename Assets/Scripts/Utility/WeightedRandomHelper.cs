using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 가중치 기반 랜덤 선택 유틸 (CharacterData 비의존 — 단위 테스트 용이).
/// CharacterDatabase.PickWeighted(룰렛휠)와 동일 알고리즘의 범용 버전.
/// 가중치 0 이하인 항목은 후보에서 제외한다.
/// </summary>
public static class WeightedRandomHelper
{
    /// <summary>가중치 배열에서 누적확률(룰렛휠)로 인덱스 1개 선택. 유효 후보 없으면 -1.</summary>
    public static int PickIndex(float[] weights)
    {
        if (weights == null || weights.Length == 0) return -1;

        float total = 0f;
        foreach (float w in weights)
            if (w > 0f) total += w;
        if (total <= 0f) return -1;

        float rnd = Random.Range(0f, total);
        float sum = 0f;
        for (int i = 0; i < weights.Length; i++)
        {
            if (weights[i] <= 0f) continue;
            sum += weights[i];
            if (rnd < sum) return i;
        }
        // 부동소수 잔차 보정: 마지막 양수 가중 인덱스
        for (int i = weights.Length - 1; i >= 0; i--)
            if (weights[i] > 0f) return i;
        return -1;
    }

    /// <summary>
    /// 가중치 배열에서 비복원으로 서로 다른 n개 인덱스 선택.
    /// 가중치 0 이하 제외. 유효 후보 &lt; n 이면 가능한 만큼만. 합계 0 이면 빈 리스트.
    /// </summary>
    public static List<int> PickDistinct(float[] weights, int n)
    {
        var result = new List<int>();
        if (weights == null || n <= 0) return result;

        // 가중치 > 0 후보 풀
        var pool = new List<int>();
        for (int i = 0; i < weights.Length; i++)
            if (weights[i] > 0f) pool.Add(i);

        int pick = Mathf.Min(n, pool.Count);
        for (int k = 0; k < pick; k++)
        {
            float total = 0f;
            foreach (int idx in pool) total += weights[idx];
            if (total <= 0f) break;

            float rnd = Random.Range(0f, total);
            float sum = 0f;
            int chosen = pool[pool.Count - 1]; // 잔차 폴백
            foreach (int idx in pool)
            {
                sum += weights[idx];
                if (rnd < sum) { chosen = idx; break; }
            }
            result.Add(chosen);
            pool.Remove(chosen);
        }
        return result;
    }
}
