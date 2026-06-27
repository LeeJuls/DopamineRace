using UnityEditor;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// SPEC-049 단계 E — 캐릭터 비디오 트랜스코드 도구.
/// 8개 캐릭터 mp4의 VideoClipImporter 설정을 트랜스코드 ON(H264)·무음으로 바꿔
/// 런타임 "Color primaries 0 ... WindowsMediaFoundation" 경고(raw mp4 직접 디코딩)를 제거한다.
/// 리임포트가 무거우므로 MCP가 아닌 [MenuItem]에서 owner가 직접 실행한다.
/// </summary>
public static class CharacterVideoTranscoder
{
    private const string VideoDir = "Assets/Resources/Icon/Char/video/";

    // §0 측정 대상 8개. ID는 핸드오프 기준(0,1,2,3,5,7,23,26).
    private static readonly int[] CharIds = { 0, 1, 2, 3, 5, 7, 23, 26 };

    [MenuItem("DopamineRace/Transcode Character Videos (SPEC-049)")]
    public static void TranscodeCharacterVideos()
    {
        int total = CharIds.Length;
        int success = 0;
        int skipped = 0;

        // 워밍 패턴: 1개만 먼저 리임포트해 트랜스코더 파이프라인을 깨우고, 성공 시 나머지를 순차 처리.
        for (int i = 0; i < total; i++)
        {
            string path = $"{VideoDir}Character_{CharIds[i]}.mp4";

            if (AssetDatabase.LoadAssetAtPath<VideoClip>(path) == null)
            {
                Debug.LogWarning($"[Transcode] 파일 없음 → skip: {path}");
                continue;
            }

            var imp = AssetImporter.GetAtPath(path) as VideoClipImporter;
            if (imp == null)
            {
                Debug.LogWarning($"[Transcode] VideoClipImporter 취득 실패 → skip: {path}");
                continue;
            }

            var s = imp.defaultTargetSettings;

            // 멱등 가드: 이미 트랜스코드 ON·H264·무음이면 불필요한 리임포트를 건너뛴다.
            if (s.enableTranscoding && s.codec == VideoCodec.H264 && !imp.importAudio)
            {
                Debug.Log($"[Transcode] 이미 적용됨 → skip: Character_{CharIds[i]} ({i + 1}/{total})");
                skipped++;
                success++;   // 최종 상태가 목표와 일치하므로 성공으로 집계
                continue;
            }

            s.enableTranscoding = true;
            s.codec = VideoCodec.H264;
            imp.defaultTargetSettings = s;   // struct 재대입 필수 — 안 하면 미반영
            imp.importAudio = false;         // 무음 영상

            imp.SaveAndReimport();

            success++;
            Debug.Log($"[Transcode] Character_{CharIds[i]} 완료 ({i + 1}/{total})");

            if (i == 0)
                Debug.Log("[Transcode] 워밍(1개) 성공 — 나머지 순차 진행");
        }

        AssetDatabase.Refresh();
        Debug.Log($"[Transcode] 완료: 성공 {success}/{total} (멱등 skip {skipped})");
    }
}
