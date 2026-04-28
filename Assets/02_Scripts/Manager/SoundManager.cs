using UnityEngine;

/// <summary>
/// 브금·효과음 재생. BGM은 단일 AudioSource·루프·2D. 같은 클립이 이미 재생 중이면 중복 호출 무시.
/// </summary>
public class SoundManager : MonoBehaviour
{
    #region Fields / Internal State

    private AudioSource _bgmSource;

    #endregion

    #region Public API

    /// <summary>브금 재생. 이미 같은 클립이 재생 중이면 아무 것도 하지 않음.</summary>
    public void BgmPlay(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogError("[SoundManager] BgmPlay: clip is null.");
            return;
        }

        if (_bgmSource == null)
        {
            Debug.LogError("[SoundManager] BgmPlay: _bgmSource is null.");
            return;
        }

        if (_bgmSource.isPlaying && _bgmSource.clip == clip)
            return;

        _bgmSource.clip = clip;
        _bgmSource.loop = true;
        _bgmSource.spatialBlend = 0f;
        _bgmSource.Play();
    }

    /// <summary>브금 정지. 씬 전환 등에서 호출.</summary>
    public void BgmStop()
    {
        if (_bgmSource == null)
        {
            Debug.LogError("[SoundManager] BgmStop: _bgmSource is null.");
            return;
        }

        _bgmSource.Stop();
        _bgmSource.clip = null;
    }

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        _bgmSource = Util.GetOrAddComponent<AudioSource>(gameObject);
        _bgmSource.playOnAwake = false;
        _bgmSource.loop = true;
        _bgmSource.spatialBlend = 0f;
    }

    #endregion
}
