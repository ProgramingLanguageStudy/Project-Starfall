using UnityEngine;

/// <summary>
/// Animator 파라미터 이름·해시 모음. SetFloat / SetTrigger 시 사용.
/// </summary>
public static class AnimatorParams
{
    // Player (이동·공격)
    public const string MoveSpeedName = "MoveSpeed";
    public static readonly int MoveSpeed = Animator.StringToHash(MoveSpeedName);

    public const string IsMovingName = "IsMoving";
    public static readonly int IsMoving = Animator.StringToHash(IsMovingName);

    public const string AttackName = "Attack";
    public static readonly int Attack = Animator.StringToHash(AttackName);

    // 상태 트리거 (Enemy 등. Attack은 위와 동일)
    public const string IdleName = "Idle";
    public static readonly int Idle = Animator.StringToHash(IdleName);

    public const string PatrolName = "Patrol";
    public static readonly int Patrol = Animator.StringToHash(PatrolName);

    public const string ChaseName = "Chase";
    public static readonly int Chase = Animator.StringToHash(ChaseName);

    public const string DeadName = "Dead";
    public static readonly int Dead = Animator.StringToHash(DeadName);
}
