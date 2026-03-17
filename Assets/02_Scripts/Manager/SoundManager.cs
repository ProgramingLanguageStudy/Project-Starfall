using UnityEngine;

/// <summary>
/// 사운드 재생. RM 또는 Resources에서 클립 로드.
/// 호출처: 전투(히트, 공격), UI 등. API는 Play(SoundType, ...) 오버로드.
/// </summary>
public class SoundManager : MonoBehaviour
{
    /// <summary>사운드 재생. 2D 또는 3D(위치 기반).</summary>
    public void Play(SoundType type)
    {
        // TODO: 클립 로드, 재생
    }

    /// <summary>3D 사운드. 위치에 따라 볼륨/패닝.</summary>
    public void Play(SoundType type, Vector3 position)
    {
        // TODO: 3D AudioSource, 위치 설정
    }
}
