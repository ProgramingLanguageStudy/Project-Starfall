using UnityEngine;
using DG.Tweening;

/// <summary>
/// 타이틀/로고용 기본 연출. 오버스케일 등장 → 1 → Punch.
/// </summary>
public static class TitlePreset
{
    public static readonly UIPresetData Data = new()
    {
        Duration = 0.7f,
        Ease = Ease.OutBack,
        ScaleFrom = new Vector3(1.25f, 1.25f, 1f),
        ScaleTo = Vector3.one,
        AlphaFrom = 0f,
        AlphaTo = 1f
    };

    /// <summary>등장 후 Punch 연출 (강도, 지속시간).</summary>
    public const float PunchStrength = 0.12f;
    public const float PunchDuration = 0.35f;
}
