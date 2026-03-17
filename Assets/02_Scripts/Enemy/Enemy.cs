using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Enemy = Model 보유 컨테이너. 전투 판단·상태 전환은 StateMachine. 팀 전투 공유는 OnEnteringCombat.
/// Character처럼 RequireComponent + GetComponent 방식.
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

    /// <summary>전투 진입 시 발행. 팀이 구독해 나머지 멤버에게 전달. triggerCharacter→Squad.Player 해석.</summary>
    public event Action<Character> OnEnteringCombat;
    /// <summary>소멸 직전 발행. 팀이 구독해 등록 해제.</summary>
    public event Action<Enemy> OnDestroyed;

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
        var combat = FindFirstObjectByType<CombatController>();
        if (inCombat)
            combat?.RegisterInCombat(this);
        else
            combat?.UnregisterFromCombat(this);
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
        // 모델이 죽어있는데 상태가 Dead가 아니라면 강제로 전환
        if (_model != null && _model.IsDead && _stateMachine != null && _stateMachine.CurrentStateKey != EnemyStateMachine.EnemyState.Dead)
        {
            _stateMachine.ChangeState(EnemyStateMachine.EnemyState.Dead);
            return;
        }
    }

    /// <summary>Spawner가 스폰 시 호출. 풀링 시 재사용 전에도 호출.</summary>
    public void Initialize()
    {
        if (_model == null) _model = GetComponent<EnemyModel>();
        if (_aggro == null) _aggro = GetComponent<EnemyAggro>();
        if (_detector == null) _detector = GetComponent<EnemyDetector>();
        if (_healthBarView == null) _healthBarView = GetComponentInChildren<WorldHealthBarView>(true);
        if (_stateMachine == null) _stateMachine = GetComponent<EnemyStateMachine>();
        if (_agent == null) _agent = GetComponent<NavMeshAgent>();
        if (_mover == null) _mover = GetComponent<EnemyMover>();
        if (_attacker == null) _attacker = GetComponent<EnemyAttacker>();
        if (_enemyAnimator == null) _enemyAnimator = GetComponent<EnemyAnimator>();
        if (_animator == null) _animator = GetComponentInChildren<Animator>();

        _model?.Initialize();
        _aggro?.Initialize(_model);
        _detector?.Initialize(_model);
        if (_detector != null)
            _detector.OnCharacterDetected += HandleCharacterDetected;

        _healthBarView?.Initialize(_model);
        _mover?.Initialize(_agent);
        _attacker?.Initialize(_model);
        _enemyAnimator?.Initialize(_animator);
        _stateMachine?.Initialize(this);

        if (_model != null)
            _model.OnDeath += HandleDeath;
    }

    private void OnDestroy()
    {
        if (_model != null)
            _model.OnDeath -= HandleDeath;

        if (_detector != null)
            _detector.OnCharacterDetected -= HandleCharacterDetected;

        ClearCombatSquad();

        var combat = FindFirstObjectByType<CombatController>();
        combat?.UnregisterFromCombat(this);
    }

    [SerializeField, Tooltip("사망 후 파괴 지연(초)")]
    private float _destroyDelay = 3f;

    /// <summary>Model.OnDeath 구독. 보상(골드·아이템)은 EnemyRewardController가 처리.</summary>
    private void HandleDeath()
    {
        PlaySceneEventHub.OnEnemyKilled?.Invoke(this);

        OnDestroyed?.Invoke(this);

        Invoke(nameof(DestroySelf), _destroyDelay);
    }

    private void DestroySelf()
    {
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
