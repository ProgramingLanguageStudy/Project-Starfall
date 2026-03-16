using UnityEngine;
using DG.Tweening;

/// <summary>
/// 버튼 등 "누르고 싶게" 만드는 아이들 스케일 펄스용 기본 프리셋.
/// </summary>
public static class IdlePulsePreset
{
    public static readonly IdlePulsePresetData Data = new()
    {
        ScaleMin = 1f,
        ScaleMax = 1.06f,
        Duration = 0.9f,
        Ease = Ease.InOutSine,
        OutlineAlphaMin = 0f,
        OutlineAlphaMax = 0f
    };

    /// <summary>아웃라인(글로우) 펄스까지 쓰는 버튼용.</summary>
    public static readonly IdlePulsePresetData WithGlow = new()
    {
        ScaleMin = 1f,
        ScaleMax = 1.06f,
        Duration = 0.9f,
        Ease = Ease.InOutSine,
        OutlineAlphaMin = 0.4f,
        OutlineAlphaMax = 1f
    };
}
