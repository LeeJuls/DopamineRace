using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SFX 키(SFXKeys) → AudioClip 매핑 1건.
/// clips가 비어있으면 무음(에러 없음) — 사운드 미제작 상태에서도 코드/게임 진행에 지장 없음.
/// </summary>
[System.Serializable]
public class SFXEntry
{
    [Tooltip("SFXKeys 상수값과 동일해야 함 (예: sfx.race.collision.hit)")]
    public string key;

    [Tooltip("재생할 클립 후보. 드래그앤드롭으로 교체. 비워두면 해당 키는 무음(에러 없음)")]
    public AudioClip[] clips;

    [Tooltip("이 효과음만의 개별 볼륨 배율 (GameSettings.sfxVolume에 곱해짐)")]
    [Range(0f, 2f)]
    public float volume = 1f;

    [Tooltip("체크 시 지속 루프 재생, 해제 시 1회만 재생")]
    public bool loop = false;

    [Tooltip("동시 재생 허용 개수 상한 (0=무제한). 루프 재생 시 미적용")]
    [Min(0)]
    public int maxConcurrent = 0;

    [Tooltip("재생 시작 지연 (초). 이벤트 발생 시점과 사운드 타이밍이 안 맞을 때 조절")]
    [Min(0f)]
    public float delay = 0f;

    public AudioClip GetRandomClip()
    {
        if (clips == null || clips.Length == 0) return null;
        if (clips.Length == 1) return clips[0];
        int idx = Random.Range(0, clips.Length);
        return clips[idx] != null ? clips[idx] : clips[0];
    }
}

/// <summary>
/// SFX 키→클립 테이블. Resources.Load&lt;SFXSettings&gt;("SFXSettings")로 로드(GameSettings.Instance 패턴 동일).
/// 사운드 파일 교체는 인스펙터에서만 — 코드 재컴파일 불필요(하드코딩 금지 원칙).
/// </summary>
[CreateAssetMenu(fileName = "SFXSettings", menuName = "DopamineRace/SFXSettings")]
public class SFXSettings : ScriptableObject
{
    public List<SFXEntry> entries = new List<SFXEntry>();

    private Dictionary<string, SFXEntry> _lookup;

    public SFXEntry Find(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        if (_lookup == null || _lookup.Count != entries.Count) RebuildLookup();
        _lookup.TryGetValue(key, out var e);
        return e;
    }

    private void RebuildLookup()
    {
        _lookup = new Dictionary<string, SFXEntry>();
        foreach (var e in entries)
        {
            if (string.IsNullOrEmpty(e.key)) continue;
            if (_lookup.ContainsKey(e.key))
                Debug.LogWarning("[SFXSettings] 중복 key: " + e.key + " — 나중 항목이 적용됨");
            _lookup[e.key] = e;
        }
    }

    private void OnValidate() => _lookup = null;
}
