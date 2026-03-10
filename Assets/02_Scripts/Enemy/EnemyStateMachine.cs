using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemy 상태머신. 상태를 클래스(Enter/Update/Exit)로 두고 전환만 담당.
/// 이동·애니·공격은 Enemy의 Mover/Animator/Attacker 경유.
/// </summary>
public class EnemyStateMachine : MonoBehaviour
{
    public enum EnemyState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Dead
    }

    private Enemy _enemy;
    private Vector3 _patrolCenter;
    private Transform _chaseTarget;

    private Dictionary<EnemyState, EnemyStateBase> _states;
    private EnemyState _currentStateKey;
    private EnemyStateBase _currentState;

    public Enemy Enemy => _enemy;
    public Vector3 PatrolCenter => _patrolCenter;
    public EnemyState CurrentStateKey => _currentStateKey;
    public Transform ChaseTarget => _chaseTarget;

    /// <summary>AI 설정은 Model(Data) 경유. 버프 등은 Model에서 반영.</summary>
    public float PatrolSpeed => _enemy?.Model?.PatrolSpeed ?? 1.5f;
    public float PatrolRadius => _enemy?.Model?.PatrolRadius ?? 5f;
    public float ArriveThreshold => _enemy?.Model?.ArriveThreshold ?? 0.5f;
    public float PatrolWalkDurationMin => _enemy?.Model?.PatrolWalkDurationMin ?? 2f;
    public float PatrolWalkDurationMax => _enemy?.Model?.PatrolWalkDurationMax ?? 3f;
    public float PatrolIdleDuration => _enemy?.Model?.PatrolIdleDuration ?? 1f;
    public float ChaseSpeed => _enemy?.Model?.ChaseSpeed ?? 4f;
    public float DetectionRadius => _enemy?.Model?.DetectionRadius ?? 10f;
    public float AttackRadius => _enemy?.Model?.AttackRadius ?? 2f;
    public float ChaseLoseRadius => _enemy?.Model?.ChaseLoseRadius ?? 15f;
    public float AttackDuration => _enemy?.Model?.AttackDuration ?? 0.6f;

    /// <summary>Spawner 등이 추적 목표 주입 시 호출. Initialize 전/후 모두 가능.</summary>
    public void SetChaseTarget(Transform target)
    {
        _chaseTarget = target;
    }

    /// <summary>Enemy가 자기 자신을 주입. 주입 후 상태 생성 및 Patrol 진입.</summary>
    public void Initialize(Enemy enemy)
    {
        _enemy = enemy;
        _patrolCenter = enemy != null ? enemy.transform.position : transform.position;

        _states = new Dictionary<EnemyState, EnemyStateBase>
        {
            [EnemyState.Idle] = new EnemyIdleState(this),
            [EnemyState.Patrol] = new EnemyPatrolState(this),
            [EnemyState.Chase] = new EnemyChaseState(this),
            [EnemyState.Attack] = new EnemyAttackState(this),
            [EnemyState.Dead] = new EnemyDeadState(this)
        };

        ChangeState(EnemyState.Patrol);

        if (_enemy?.Model != null)
        {
            _enemy.Model.OnDeath += HandleDeath;
            _enemy.Model.OnDamaged += HandleDamaged;
        }
    }

    private void OnDestroy()
    {
        if (_enemy?.Model != null)
        {
            _enemy.Model.OnDeath -= HandleDeath;
            _enemy.Model.OnDamaged -= HandleDamaged;
        }
    }

    private void HandleDamaged(Transform attacker)
    {
        if (attacker == null) return;
        bool isChaseOrAttack = _currentStateKey == EnemyState.Chase || _currentStateKey == EnemyState.Attack;
        if (isChaseOrAttack) return;

        var ch = attacker.GetComponentInParent<Character>();
        if (ch != null)
            _enemy.NotifyEnteringCombat(ch);
        else
        {
            _enemy.SetChaseTarget(attacker);
            RequestChase();
        }
    }

    private void HandleDeath()
    {
        ChangeState(EnemyState.Dead);
    }

    private void Update()
    {
        if (_states == null) return;
        if (_currentStateKey == EnemyState.Dead) return;

        // 전투 판단: 플레이어 도주 시 전투 종료
        bool isChaseOrAttack = _currentStateKey == EnemyState.Chase || _currentStateKey == EnemyState.Attack;
        if (isChaseOrAttack)
        {
            var resetTarget = _enemy?.CombatSquad?.Player?.transform ?? _chaseTarget;
            if (_enemy?.Aggro?.TryResetIfTargetOutOfRange(_enemy.transform.position, resetTarget) == true)
            {
                _enemy.ClearCombatSquad();
                RequestPatrol();
                return;
            }
        }

        _currentState?.Update();
    }

    /// <summary>상태 전환. Exit → 교체 → Enter. Chase/Attack 진입 시 CombatController에 등록.</summary>
    public void ChangeState(EnemyState key)
    {
        if (_states == null) return;
        // 이미 죽었다면 그 어떤 상태로도 전환될 수 없도록 차단 (중요!)
        if (_currentStateKey == EnemyState.Dead) return;

        if (!_states.TryGetValue(key, out EnemyStateBase newState) || newState == null) return;
        if (_currentState == newState) return;

        _currentState?.Exit();
        _currentStateKey = key;
        _currentState = newState;
        _currentState.Enter();

        UpdateCombatRegistration(key);
    }

    private void UpdateCombatRegistration(EnemyState newState)
    {
        if (_enemy == null) return;

        bool inCombat = newState == EnemyState.Chase || newState == EnemyState.Attack;
        _enemy.NotifyCombatStateChanged(inCombat);
    }

    /// <summary>현재 상태 키로 상태 인스턴스 반환.</summary>
    public EnemyStateBase GetState(EnemyState key) => _states != null && _states.TryGetValue(key, out var s) ? s : null;

    public void RequestPatrol()
    {
        _enemy?.ClearCombatSquad();
        ChangeState(EnemyState.Patrol);
    }
    public void RequestIdle() => ChangeState(EnemyState.Idle);
    public void RequestChase() => ChangeState(EnemyState.Chase);
    public void RequestAttack() => ChangeState(EnemyState.Attack);
    public void RequestDead() => ChangeState(EnemyState.Dead);
}
