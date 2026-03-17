using UnityEngine;

/// <summary>
/// Enemy Animator 래퍼. 상태 전환 시 AnimatorParams 트리거(Idle, Patrol, Chase, Attack, Dead)로 애니 재생.
/// Animator는 자식(모델링)에 둠. Character와 동일 구조.
/// </summary>
public class EnemyAnimator : MonoBehaviour
{
    private Animator _animator;

    public void Initialize(Animator animator)
    {
        _animator = animator;
    }

    public void Idle()
    {
        if (_animator != null)
            _animator.SetTrigger(AnimatorParams.Idle);
    }

    public void Patrol()
    {
        if (_animator != null)
            _animator.SetTrigger(AnimatorParams.Patrol);
    }

    public void Chase()
    {
        if (_animator != null)
            _animator.SetTrigger(AnimatorParams.Chase);
    }

    public void Attack()
    {
        if (_animator != null)
            _animator.SetTrigger(AnimatorParams.Attack);
    }

    public void Dead()
    {
        if (_animator != null)
            _animator.SetTrigger(AnimatorParams.Dead);
    }
}
