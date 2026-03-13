using UnityEngine;
using DG.Tweening;

/// <summary>
/// UI 연출 프리셋 데이터. Scale + Alpha 트윈 파라미터.
/// </summary>
[System.Serializable]
public struct UIPresetData
{
    public float Duration;
    public Ease Ease;
    public Vector3 ScaleFrom;
    public Vector3 ScaleTo;
    public float AlphaFrom;
    public float AlphaTo;

    public static UIPresetData Default => new()
    {
        Duration = 0.25f,
        Ease = Ease.OutQuad,
        ScaleFrom = Vector3.one,
        ScaleTo = Vector3.one,
        AlphaFrom = 1f,
        AlphaTo = 1f
    };
}
