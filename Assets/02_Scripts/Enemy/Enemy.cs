using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Enemy = Model 보유 컨테이너. 전투 판단·상태 전환은 StateMachine. 팀 전투 공유는 OnEnteringCombat.
/// 풀링: 사망 지연 후 <see cref="Poolable.ReturnToPool"/>. 팀 해산은 <see cref="OnReturnedToPool"/>.
/// </summary>
[RequireComponent(typeof(EnemyModel)), RequireComponent(typeof(EnemyAnimator)), RequireComponent(typeof(EnemyMover)),
 RequireComponent(typeof(EnemyAggro)), RequireComponent(typeof(EnemyDetector)),
 RequireComponent(typeof(EnemyStateMachine)), RequireComponent(typeof(EnemyAttacker))]
public class Enemy : MonoBehaviour
{
    private EnemyModel _model;
    private EnemyMover _mover;
    private EnemyAnimator _enemyAnimator;
    private EnemyAttacker _attacker;
    private EnemyStateMachine _stateMachine;
    private EnemyAggro _aggro;
    private EnemyDetector _detector;
    private WorldHealthBarView _healthBarView;
    private NavMeshAgent _agent;
    private Animator _animator;
    private CombatController _combatController;

    private bool _runtimeWired;
    private bool _stateMachineInitialized;

    /// <summary>전투 진입 시 발행. 팀이 구독해 나머지 멤버에게 전달. triggerCharacter→Squad.Player 해석.</summary>
    public event Action<Character> OnEnteringCombat;

    /// <summary>사망 직후(보상·퀘스트 등). 풀 반환 전.</summary>
    public event Action<Enemy> OnDestroyed;

    /// <summary>풀 반환 직전. 팀은 여기서 멤버 제거 후 빈 팀 오브젝트 파괴.</summary>
    public event Action<Enemy> OnReturnedToPool;

    public EnemyModel Model => _model;
    public EnemyMover Mover => _mover;
    public EnemyAttacker Attacker => _attacker;
    public EnemyAnimator Animator => _enemyAnimator;
    public EnemyStateMachine StateMachine => _stateMachine;
    public EnemyAggro Aggro => _aggro;

    /// <summary>전투 중 추적 대상의 Squad. 플레이어 도주 판단·OnPlayerChanged 구독용.</summary>
    public Squad CombatSquad { get; private set; }

    /// <summary>전투 진입/이탈 알림. StateMachine이 호출. CombatController에 등록/해제.</summary>
    public void NotifyCombatStateChanged(bool inCombat)
    {
        if (_combatController == null)
        {
            Debug.LogError("[Enemy] CombatController is null. Call ConfigureFromSpawn after spawn.");
            return;
        }

        if (inCombat)
            _combatController.RegisterInCombat(this);
        else
            _combatController.UnregisterFromCombat(this);
    }

    /// <summary>전투 진입 알림. Character(감지/공격) → Squad.Player 해석 후 타겟 설정. 팀 구독자에게 전달.</summary>
    public void NotifyEnteringCombat(Character triggerCharacter)
    {
        if (triggerCharacter == null) return;

        var target = ResolveChaseTarget(triggerCharacter);
        var squad = triggerCharacter.Squad;
        SetChaseTargetAndSquad(target, squad);

        if (OnEnteringCombat != null)
            OnEnteringCombat.Invoke(triggerCharacter);
        else
            StateMachine?.RequestChase();
    }

    /// <summary>Character→Squad.Player 해석. Squad 없으면 Character 그대로. EnemyTeam에서도 사용.</summary>
    public static Transform ResolveChaseTarget(Character triggerCharacter)
    {
        var player = triggerCharacter.Squad?.Player;
        var target = player != null ? player : triggerCharacter;
        return target != null ? target.transform : null;
    }

    /// <summary>추적 목표 및 Squad 설정. OnPlayerChanged 구독.</summary>
    public void SetChaseTargetAndSquad(Transform target, Squad squad)
    {
        ClearCombatSquad();
        CombatSquad = squad;
        _stateMachine?.SetChaseTarget(target);
        if (squad != null)
            squad.OnPlayerChanged += HandleSquadPlayerChanged;
    }

    private void HandleSquadPlayerChanged(Character newPlayer)
    {
        if (newPlayer == null) return;
        _stateMachine?.SetChaseTarget(newPlayer.transform);
    }

    /// <summary>전투 이탈 시 호출. OnPlayerChanged 구독 해제.</summary>
    public void ClearCombatSquad()
    {
        if (CombatSquad != null)
        {
            CombatSquad.OnPlayerChanged -= HandleSquadPlayerChanged;
            CombatSquad = null;
        }
    }

    /// <summary>직접 추적 목표만 설정 (Squad 없을 때).</summary>
    public void SetChaseTarget(Transform target)
    {
        _stateMachine?.SetChaseTarget(target);
    }

    private void Update()
    {
        if (_model != null && _model.IsDead && _stateMachine != null && _stateMachine.CurrentStateKey != EnemyStateMachine.EnemyState.Dead)
        {
            _stateMachine.ChangeState(EnemyStateMachine.EnemyState.Dead);
            return;
        }
    }

    /// <summary>풀에서 꺼낸 뒤 1회 호출. 위치 설정 후 호출할 것.</summary>
    public void ConfigureFromSpawn(CombatController combatController, EnemyData data, Vector3 patrolCenter)
    {
        CancelInvoke(nameof(CompleteRecycleToPool));

        CacheComponentsIfNeeded();

        if (!_runtimeWired)
        {
            if (_detector != null)
                _detector.OnCharacterDetected += HandleCharacterDetected;
            if (_model != null)
                _model.OnDeath += HandleDeath;
            _runtimeWired = true;
        }

        _combatController = combatController;

        if (data == null)
        {
            Debug.LogError("[Enemy] ConfigureFromSpawn: EnemyData is null.", this);
            return;
        }

        _model.ApplyData(data);
        _model.Initialize();
        _aggro?.Initialize(_model);
        _detector?.Initialize(_model);

        if (_agent != null)
            _agent.enabled = true;
        var col = GetComponent<Collider>();
        if (col != null)
            col.enabled = true;

        if (!_stateMachineInitialized)
        {
            _stateMachine.Initialize(this);
            _stateMachineInitialized = true;
        }
        else
            _stateMachine.ResetAfterPoolReturn(patrolCenter);

        _healthBarView?.Initialize(_model);
        _mover?.Initialize(_agent);
        _attacker?.Initialize(_model);
        _enemyAnimator?.Initialize(_animator);
    }

    private void CacheComponentsIfNeeded()
    {
        if (_model != null) return;

        _model = GetComponent<EnemyModel>();
        _aggro = GetComponent<EnemyAggro>();
        _detector = GetComponent<EnemyDetector>();
        _healthBarView = GetComponentInChildren<WorldHealthBarView>(true);
        _stateMachine = GetComponent<EnemyStateMachine>();
        _agent = GetComponent<NavMeshAgent>();
        _mover = GetComponent<EnemyMover>();
        _attacker = GetComponent<EnemyAttacker>();
        _enemyAnimator = GetComponent<EnemyAnimator>();
        _animator = GetComponentInChildren<Animator>();
    }

    private void OnDestroy()
    {
        CancelInvoke(nameof(CompleteRecycleToPool));

        if (_model != null)
            _model.OnDeath -= HandleDeath;

        if (_detector != null)
            _detector.OnCharacterDetected -= HandleCharacterDetected;

        ClearCombatSquad();

        _combatController?.UnregisterFromCombat(this);
    }

    [SerializeField, Tooltip("사망 후 풀 반환 지연(초)")]
    private float _recycleDelay = 3f;

    /// <summary>Model.OnDeath. 보상은 EnemyRewardController·PlaySceneEventHub.</summary>
    private void HandleDeath()
    {
        PlaySceneEventHub.OnEnemyKilled?.Invoke(this);
        OnDestroyed?.Invoke(this);
        Invoke(nameof(CompleteRecycleToPool), _recycleDelay);
    }

    private void CompleteRecycleToPool()
    {
        CancelInvoke(nameof(CompleteRecycleToPool));
        ClearCombatSquad();
        _combatController?.UnregisterFromCombat(this);

        transform.SetParent(null);

        OnReturnedToPool?.Invoke(this);

        var poolable = GetComponent<Poolable>();
        if (poolable != null)
            poolable.ReturnToPool();
        else
            Destroy(gameObject);
    }

    /// <summary>Detect로 Character 감지 시. 반경 내이면 전투 진입. (전투진입 트리거)</summary>
    private void HandleCharacterDetected(Character ch, float distance)
    {
        if (ch == null || ch.Model == null || ch.Model.IsDead) return;
        if (_stateMachine == null) return;

        var key = _stateMachine.CurrentStateKey;
        if (key != EnemyStateMachine.EnemyState.Patrol && key != EnemyStateMachine.EnemyState.Idle) return;

        float detectRadius = _model != null ? _model.DetectionRadius : 10f;
        if (distance > detectRadius) return;

        NotifyEnteringCombat(ch);
    }
}
