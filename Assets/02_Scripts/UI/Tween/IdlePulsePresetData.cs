using UnityEngine;
using DG.Tweening;

/// <summary>
/// 가만히 있는 상태로 반복 연출하는 프리셋 데이터. 스케일 펄스(필수) + 아웃라인 알파 펄스(선택).
/// </summary>
[System.Serializable]
public struct IdlePulsePresetData
{
    public float ScaleMin;
    public float ScaleMax;
    public float Duration;
    public Ease Ease;
    [Tooltip("0이면 미사용")]
    public float OutlineAlphaMin;
    [Tooltip("0이면 미사용")]
    public float OutlineAlphaMax;
}
