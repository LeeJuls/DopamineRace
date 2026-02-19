using UnityEngine;
using System.Collections;

/// <summary>
/// BGM 매니저: 게임 시작 후 딜레이 뒤 BGM 루프 재생
/// BGM 파일 위치: Assets/Resources/Audio/BGM.mp3
/// </summary>
public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }

    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
    }

    private void Start()
    {
        AudioClip bgm = Resources.Load<AudioClip>("Audio/BGM");
        if (bgm == null)
        {
            Debug.LogWarning("[BGMManager] Resources/Audio/BGM 파일을 찾을 수 없습니다.");
            return;
        }

        audioSource.clip = bgm;
        audioSource.volume = GameSettings.Instance.bgmVolume;
        StartCoroutine(PlayAfterDelay(GameSettings.Instance.bgmDelay));
    }

    private IEnumerator PlayAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        audioSource.Play();
    }

    /// <summary>
    /// 볼륨 변경 (런타임)
    /// </summary>
    public void SetVolume(float vol)
    {
        audioSource.volume = Mathf.Clamp01(vol);
    }
}
