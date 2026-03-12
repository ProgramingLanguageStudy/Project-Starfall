using UnityEngine;
using DG.Tweening;

/// <summary>
/// CanvasGroup alpha 연출. 페이드 인/아웃에 사용.
/// 대상에 CanvasGroup이 없으면 자동 추가함.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class AlphaTween : UITweenBase
{
    [Header("Alpha")]
    [SerializeField] float fromAlpha;
    [SerializeField] float toAlpha = 1f;

    CanvasGroup _canvasGroup;

    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public override Tween PlayForward()
    {
        _canvasGroup.alpha = fromAlpha;
        return _canvasGroup.DOFade(toAlpha, duration)
            .SetEase(ease)
            .SetDelay(delay);
    }

    public override Tween PlayReverse()
    {
        _canvasGroup.alpha = toAlpha;
        return _canvasGroup.DOFade(fromAlpha, duration)
            .SetEase(ease)
            .SetDelay(delay);
    }
}
