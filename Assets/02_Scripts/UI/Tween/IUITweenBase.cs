using DG.Tweening;

/// <summary>
/// UI Tween 연출 컴포넌트 공통 인터페이스.
/// Facade가 GetComponentsInChildren로 수집해 PlayEnter/PlayExit에서 조합 재생.
/// </summary>
public interface IUITweenBase
{
    /// <summary>등장 방향 재생 (열기).</summary>
    Tween PlayForward();

    /// <summary>퇴장 방향 재생 (닫기).</summary>
    Tween PlayReverse();
}
