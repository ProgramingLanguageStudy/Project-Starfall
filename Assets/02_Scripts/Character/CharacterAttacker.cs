using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 공격 관련 로직. HitboxController + 감지 시 데미지 적용.
/// 애니 이벤트: Animation_BeginHitWindow, Animation_EndHitWindow, Animation_OnAttackEnded.
/// </summary>
public class CharacterAttacker : MonoBehaviour
{
    [SerializeField] private HitboxController _hitboxController;

    private Character _ownerCharacter;
    private CharacterModel _ownerModel;
    private CharacterStateMachine _stateMachine;
    private CharacterAnimator _characterAnimator;
    private readonly HashSet<IDamageable> _hitThisAttack = new HashSet<IDamageable>();

    public void Initialize(Character owner, CharacterStateMachine stateMachine, CharacterModel ownerModel, CharacterAnimator characterAnimator)
    {
        _ownerCharacter = owner;
        _stateMachine = stateMachine;
        _ownerModel = ownerModel;
        _characterAnimator = characterAnimator;

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
        if (target == null || _ownerModel == null)
        {
            if (target == null) Debug.Log($"[CharacterAttacker] {gameObject.name} ApplyDamage 스킵: target==null");
            else if (_ownerModel == null) Debug.Log($"[CharacterAttacker] {gameObject.name} ApplyDamage 스킵: _ownerModel==null");
            return;
        }
        if (ReferenceEquals(target, _ownerModel))
        {
            Debug.Log($"[CharacterAttacker] {gameObject.name} ApplyDamage 스킵: 자기 자신");
            return;
        }
        if (_hitThisAttack.Contains(target))
        {
            Debug.Log($"[CharacterAttacker] {gameObject.name} ApplyDamage 스킵: 이미 타격함 (target={target})");
            return;
        }

        _hitThisAttack.Add(target);
        int damage = _ownerModel.AttackPower;
        var attacker = _ownerCharacter != null ? _ownerCharacter.transform : null;
        Debug.Log($"[CharacterAttacker] {gameObject.name} → {target} 데미지 {damage} 적용");
        target.TakeDamage(damage, attacker);
    }

    /// <summary>AttackState.Enter에서 호출. 공격 시작.</summary>
    public void Begin()
    {
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
        _stateMachine?.RequestIdle();
    }
}
