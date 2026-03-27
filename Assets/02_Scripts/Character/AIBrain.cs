using UnityEngine;

/// <summary>
/// 동료 AI 제어. FollowTarget 관리·CombatController 기반 Follow/Combat/Attack 판단.
/// Character.Squad.PlayerCharacter로 따라갈 타겟 획득.
/// Character는 CurrentTarget, CurrentStopDistance 프로퍼티로 읽어 ApplyMovement에서 사용.
/// </summary>
[RequireComponent(typeof(Character))]
public class AIBrain : MonoBehaviour
{
    private Character _character;
    private CombatController _combatController;

    [Header("전투 설정")]
    [SerializeField] private float _attackRange = 2f;
    [SerializeField] private float _targetUpdateInterval = 0.5f;
    private Enemy _currentCombatTarget;
    private float _targetUpdateTimer;

    private Transform _currentTarget;
    private float _currentStopDistance;

    /// <summary>Character.Initialize에서 호출. Character·CombatController 주입.</summary>
    public void Initialize(Character character, CombatController combatController)
    {
        _character = character;
        _combatController = combatController;
    }

    /// <summary>Character.ApplyMovement에서 동료 이동 시 읽음.</summary>
    public Transform CurrentTarget => _currentTarget;

    /// <summary>Character.ApplyMovement에서 동료 이동 시 읽음.</summary>
    public float CurrentStopDistance => _currentStopDistance;

    private void Update()
    {
        if (_character == null || _character.Model == null || _character.Model.IsDead) return;
        if (_character.StateMachine != null && _character.StateMachine.CurrentState == CharacterState.Attack) return;

        bool isInCombat = _combatController != null && _combatController.IsInCombat;
        if (isInCombat)
            TickCombat();
        else
            TickFollow();
    }

    private void TickFollow()
    {
        _currentCombatTarget = null;
        Character player = _character?.Squad?.Player;
        _currentTarget = player != null ? player.transform : null;
        _currentStopDistance = _character?.Model != null ? _character.Model.StopDistance : 1.5f;
        if (_currentTarget == null)
        {
            _character?.RequestIdle();
        }
        else if (HasArrived())
        {
            _character?.RequestIdle();
        }
        else
        {
            _character?.RequestMove();
        }
    }

    private void TickCombat()
    {
        CombatController combat = _combatController;
        if (combat == null || !combat.IsInCombat) return;

        _targetUpdateTimer += Time.deltaTime;
        if (_targetUpdateTimer >= _targetUpdateInterval || _currentCombatTarget == null)
        {
            _targetUpdateTimer = 0f;
            _currentCombatTarget = combat.GetNearestEnemy(transform.position);
        }

        if (_currentCombatTarget == null || _currentCombatTarget.Model.IsDead) return;

        _currentTarget = _currentCombatTarget.transform;
        _currentStopDistance = _attackRange;

        float dist = Vector3.Distance(transform.position, _currentCombatTarget.transform.position);
        if (dist > _attackRange)
        {
            if (!HasArrived())
                _character?.RequestMove();
        }
        else
        {
            _character?.RequestIdle(); // 사거리 안: 이동 중단, 공격만
        }

        if (dist <= _attackRange)
        {
            FaceTargetImmediate();
            _character?.RequestAttack();
        }
    }

    private bool HasArrived()
    {
        if (_currentTarget == null) return true;
        float dist = Vector3.Distance(transform.position, _currentTarget.position);
        return dist <= _currentStopDistance + 0.1f;
    }

    private void FaceTargetImmediate()
    {
        Enemy enemy = _combatController?.GetNearestEnemy(transform.position);
        if (enemy == null) return;

        Vector3 dir = (enemy.transform.position - transform.position).normalized;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(dir);
    }
}
