using UnityEngine;
using DG.Tweening;

/// <summary>
/// RectTransform anchoredPosition 연출. 슬라이드 인/아웃에 사용.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class PositionTween : UITweenBase
{
    [Header("Position")]
    [SerializeField] Vector2 fromPosition;
    [SerializeField] Vector2 toPosition;

    RectTransform _rect;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    public override Tween PlayForward()
    {
        _rect.anchoredPosition = fromPosition;
        return _rect.DOAnchorPos(toPosition, duration)
            .SetEase(ease)
            .SetDelay(delay);
    }

    public override Tween PlayReverse()
    {
        _rect.anchoredPosition = toPosition;
        return _rect.DOAnchorPos(fromPosition, duration)
            .SetEase(ease)
            .SetDelay(delay);
    }
}
