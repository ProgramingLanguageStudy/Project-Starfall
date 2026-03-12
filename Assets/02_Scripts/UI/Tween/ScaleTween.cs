using UnityEngine;
using DG.Tweening;

/// <summary>
/// RectTransform scale 연출. 팝업 등장(0→1), 호버 확대 등에 사용.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ScaleTween : UITweenBase
{
    [Header("Scale")]
    [SerializeField] Vector3 fromScale = Vector3.zero;
    [SerializeField] Vector3 toScale = Vector3.one;

    RectTransform _rect;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    public override Tween PlayForward()
    {
        _rect.localScale = fromScale;
        return _rect.DOScale(toScale, duration)
            .SetEase(ease)
            .SetDelay(delay);
    }

    public override Tween PlayReverse()
    {
        _rect.localScale = toScale;
        return _rect.DOScale(fromScale, duration)
            .SetEase(ease)
            .SetDelay(delay);
    }
}
