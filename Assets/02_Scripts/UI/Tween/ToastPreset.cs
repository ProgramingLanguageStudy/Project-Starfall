using UnityEngine;
using DG.Tweening;

/// <summary>
/// 토스트/에러 알림용 기본 연출. 가볍고 빠른 등장.
/// </summary>
public static class ToastPreset
{
    public static readonly UIPresetData Data = new()
    {
        Duration = 0.18f,
        Ease = Ease.OutQuad,
        ScaleFrom = new Vector3(0.92f, 0.92f, 1f),
        ScaleTo = Vector3.one,
        AlphaFrom = 0f,
        AlphaTo = 1f
    };
}
