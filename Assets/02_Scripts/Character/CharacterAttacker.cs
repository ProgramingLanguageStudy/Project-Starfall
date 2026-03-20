using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 공격 관련 로직. HitboxController + 감지 시 데미지 적용.
/// 애니 이벤트: Animation_BeginHitWindow, Animation_EndHitWindow, Animation_OnAttackEnded.
/// </summary>
public class CharacterAttacker : MonoBehaviour
{
    [SerializeField] private HitboxController _hitboxController;

    private IAttackPowerSource _ownerPowerSource;
    private CharacterStateMachine _stateMachine;
    private CharacterAnimator _characterAnimator;
    private Transform _damageSourceTransform;
    private readonly HashSet<IDamageable> _hitThisAttack = new HashSet<IDamageable>();

    /// <summary>애니메이션 종료 시 true. AttackState.IsComplete가 읽음. Begin()에서 초기화.</summary>
    public bool IsAttackEnded { get; private set; }

    public void Initialize(IAttackPowerSource powerSource, CharacterStateMachine stateMachine, CharacterAnimator characterAnimator, Transform damageSource)
    {
        _ownerPowerSource = powerSource;
        _stateMachine = stateMachine;
        _characterAnimator = characterAnimator;
        _damageSourceTransform = damageSource;

        if (_hitboxController == null)
            _hitboxController = GetComponentInChildren<HitboxController>(true);
        if (_hitboxController == null)
            Debug.LogWarning($"[CharacterAttacker] {gameObject.name}: HitboxController를 찾을 수 없습니다. 자식 계층 어딘가에 있어야 합니다.");
    }

    private void OnEnable()
    {
        if (_hitboxController != null)
            _hitboxController.OnDamageableDetected += ApplyDamage;
    }

    private void OnDisable()
    {
        if (_hitboxController != null)
            _hitboxController.OnDamageableDetected -= ApplyDamage;
    }

    private void ApplyDamage(IDamageable target)
    {
        if (target == null || _ownerPowerSource == null)
        {
            return;
        }

        // 자기 자신 타격 방지 (IAttackPowerSource가 MonoBehaviour라면 비교 가능)
        if (_ownerPowerSource is MonoBehaviour ownerMb && ReferenceEquals(target, ownerMb.GetComponent<IDamageable>()))
        {
            return;
        }

        if (_hitThisAttack.Contains(target))
        {
            return;
        }

        _hitThisAttack.Add(target);
        int damage = _ownerPowerSource.AttackPower;
        
        Debug.Log($"[CharacterAttacker] {gameObject.name} → {target} 데미지 {damage} 적용");
        target.TakeDamage(damage, _damageSourceTransform);
    }

    /// <summary>AttackState.Enter에서 호출. 공격 시작.</summary>
    public void Begin()
    {
        IsAttackEnded = false;
        _characterAnimator?.Attack();
        _hitThisAttack.Clear();
    }

    /// <summary>AttackState.Exit에서 호출. 공격 종료 정리.</summary>
    public void End()
    {
        _hitboxController?.DisableHit();
    }

    public void Animation_BeginHitWindow()
    {
        Debug.Log($"[CharacterAttacker] {gameObject.name} HitWindow Begin (히트박스 활성화)");
        _hitboxController?.EnableHit();
    }

    public void Animation_EndHitWindow()
    {
        Debug.Log($"[CharacterAttacker] {gameObject.name} HitWindow End");
        _hitboxController?.DisableHit();
    }

    public void Animation_OnAttackEnded()
    {
        _hitboxController?.DisableHit();
        IsAttackEnded = true;
    }
}
