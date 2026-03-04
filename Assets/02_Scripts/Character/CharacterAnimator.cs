using UnityEngine;

/// <summary>
/// 캐릭터 Animator 래퍼. 파라미터는 AnimatorParams 사용.
/// Move(speed)는 호출부(Character)가 매 프레임 전달. Attack/Dead/ResetToIdle은 State·RespawnController 등에서 호출.
/// </summary>
public class CharacterAnimator : MonoBehaviour
{
    private Animator _animator;

    private const float DampTime = 0.1f;

    public void Initialize(Animator animator)
    {
        _animator = animator;
    }

    /// <summary>Idle↔Move 전환. Character.Update에서 StateMachine.CurrentState 기준으로 호출.</summary>
    public void SetMoving(bool value)
    {
        if (_animator != null)
            _animator.SetBool(AnimatorParams.IsMoving, value);
    }

    /// <summary>이동 속도 적용. Move 상태 내 walk/run 블렌드용. Character.Update에서 호출.</summary>
    public void Move(float moveSpeed)
    {
        if (_animator != null)
            _animator.SetFloat(AnimatorParams.MoveSpeed, moveSpeed, DampTime, Time.deltaTime);
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

    public void ResetToIdle()
    {
        if (_animator != null)
            _animator.Play("Idle", 0, 0f);
    }
}
