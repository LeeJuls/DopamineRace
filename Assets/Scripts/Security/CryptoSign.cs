using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>
/// HMAC-SHA256 헬퍼 — 저장 무결성 체크섬용 (SPEC-044 Phase B / 저장 HMAC).
/// 의존성 0 (System.Security.Cryptography, .NET Standard 2.1). MonoBehaviour 아님.
/// </summary>
public static class CryptoSign
{
    private static string _localKey;

    /// <summary>
    /// 로컬 저장 무결성 키 — 기기 고유값 파생(서버 비공유, 빌드/기기별 상이). 저장 변조 탐지 전용.
    /// deviceId는 평문 노출 회피 위해 한 단계 HMAC. ※ 기기 변경 시 키가 바뀌어 타 기기 저장은 레거시 취급(grace).
    /// </summary>
    public static string LocalKey
    {
        get
        {
            if (string.IsNullOrEmpty(_localKey))
            {
                string seed = SystemInfo.deviceUniqueIdentifier + "|DopamineRace|save-v1";
                _localKey = HmacHex(Encoding.UTF8.GetBytes(seed), Encoding.UTF8.GetBytes("DRZ-local-salt"));
            }
            return _localKey;
        }
    }

    /// <summary>data를 key로 HMAC-SHA256 서명 → 소문자 hex. key 비면 빈 문자열.</summary>
    public static string HmacHex(byte[] data, byte[] key)
    {
        if (data == null) data = Array.Empty<byte>();
        if (key == null || key.Length == 0) return string.Empty;
        using (var h = new HMACSHA256(key))
        {
            byte[] mac = h.ComputeHash(data);
            var sb = new StringBuilder(mac.Length * 2);
            for (int i = 0; i < mac.Length; i++) sb.Append(mac[i].ToString("x2"));
            return sb.ToString();
        }
    }

    /// <summary>UTF-8 문자열 편의 오버로드.</summary>
    public static string HmacHex(string data, string key)
    {
        return HmacHex(
            Encoding.UTF8.GetBytes(data ?? string.Empty),
            Encoding.UTF8.GetBytes(key ?? string.Empty));
    }

    /// <summary>상수시간 hex 비교 (타이밍 누출 완화). 길이 다르면 즉시 false.</summary>
    public static bool ConstantTimeEquals(string a, string b)
    {
        if (a == null || b == null) return false;
        if (a.Length != b.Length) return false;
        int diff = 0;
        for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
        return diff == 0;
    }
}
