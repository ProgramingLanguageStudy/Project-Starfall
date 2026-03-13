using UnityEngine;
using DG.Tweening;

/// <summary>
/// 패널용 기본 연출. 설정, 맵, 인벤토리 등에 사용.
/// </summary>
public static class PanelPreset
{
    public static readonly UIPresetData Data = new()
    {
        Duration = 0.28f,
        Ease = Ease.OutQuad,
        ScaleFrom = new Vector3(0.9f, 0.9f, 1f),
        ScaleTo = Vector3.one,
        AlphaFrom = 0f,
        AlphaTo = 1f
    };
}
