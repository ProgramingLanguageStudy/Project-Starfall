using UnityEngine;
using DG.Tweening;

/// <summary>
/// UI Tween 속성별 컴포넌트의 공통 베이스.
/// duration, ease 등 공통 설정을 담고, PlayForward/PlayReverse 구현을 서브클래스에 위임.
/// </summary>
public abstract class UITweenBase : MonoBehaviour, IUITweenBase
{
    [Header("Tween Settings")]
    [SerializeField] protected float duration = 0.25f;
    [SerializeField] protected Ease ease = Ease.OutQuad;
    [SerializeField] protected float delay;

    public abstract Tween PlayForward();
    public abstract Tween PlayReverse();
}
