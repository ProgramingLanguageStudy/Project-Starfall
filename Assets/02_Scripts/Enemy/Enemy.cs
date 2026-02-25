using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Enemy = Model 보유 컨테이너. 전투 판단·상태 전환은 StateMachine. 팀 전투 공유는 OnEnteringCombat.
/// </summary>
[RequireComponent(typeof(EnemyModel)), RequireComponent(typeof(EnemyAnimator)), RequireComponent(typeof(EnemyMover)),
 RequireComponent(typeof(EnemyAggro)), RequireComponent(typeof(EnemyDetector))]
public class Enemy : MonoBehaviour
{
    [Header("----- 부품 (인스펙터에서 연결) -----")]
    [SerializeField] [Tooltip("스탯·체력. Data 기반. 전투/체력바에서 참조")]
    private EnemyModel _model;
    [SerializeField] [Tooltip("이동 제어. 속도·정지·목표 설정")]
    private EnemyMover _mover;
    [SerializeField] [Tooltip("애니메이션 제어. Idle/Patrol/Chase/Attack/Dead 트리거")]
    private EnemyAnimator _enemyAnimator;
    [SerializeField] [Tooltip("공격. HitboxController 히트/데미지. 있으면 Start 시 Model 주입")]
    private EnemyAttacker _attacker;
    [SerializeField] [Tooltip("상태머신. 있으면 Start 시 자기 자신 주입")]
    private EnemyStateMachine _stateMachine;
    [SerializeField] [Tooltip("머리 위 체력바. 자식 등에서 연결, Start 시 Model 주입")]
    private WorldHealthBarView _healthBarView;
    [SerializeField] [Tooltip("탐지. 반경 내 Character 감지, 이벤트 발행")]
    private EnemyDetector _detector;

    /// <summary>어그로 수치가 임계값 초과 시 발행. 팀이 구독해 나머지 멤버에게 전달.</summary>
    public event Action<Transform> OnEnteringCombat;
    /// <summary>소멸 직전 발행. 팀이 구독해 등록 해제.</summary>
    public event Action<Enemy> OnDestroyed;

    [Header("----- 주입용 참조 -----")]
    [SerializeField] [Tooltip("NavMesh 이동. Mover 초기화 시 주입")]
    private NavMeshAgent _agent;
    [SerializeField] [Tooltip("Unity Animator. EnemyAnimator 초기화 시 주입")]
    private Animator _animator;

    public EnemyModel Model => _model;
    public EnemyMover Mover => _mover;
    public EnemyAttacker Attacker => _attacker;
    public EnemyAnimator Animator => _enemyAnimator;
    public EnemyStateMachine StateMachine => _stateMachine;
    public EnemyAggro Aggro => _aggro;

    [SerializeField] [Tooltip("어그로 관리. 감지 이벤트 구독")]
    private EnemyAggro _aggro;

    /// <summary>전투 진입/이탈 알림. StateMachine이 호출. CombatController에 등록/해제.</summary>
    public void NotifyCombatStateChanged(bool inCombat)
    {
        var combat = UnityEngine.Object.FindFirstObjectByType<CombatController>();
        if (inCombat)
            combat?.RegisterInCombat(this);
        else
            combat?.UnregisterFromCombat(this);
    }

    /// <summary>전투 진입 알림. StateMachine 등이 호출. 팀 구독자에게 전달.</summary>
    public void NotifyEnteringCombat(Transform chaseTarget)
    {
        if (OnEnteringCombat != null)
            OnEnteringCombat.Invoke(chaseTarget);
        else
        {
            SetChaseTarget(chaseTarget);
            StateMachine?.RequestChase();
        }
    }

    /// <summary>Spawner·Team 등이 추적 목표 주입 시 호출.</summary>
    public void SetChaseTarget(Transform target)
    {
        _stateMachine?.SetChaseTarget(target);
    }

    private void Awake()
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
        if (_animator == null) _animator = GetComponent<Animator>();
    }

    /// <summary>Spawner가 스폰 시 호출. 풀링 시 재사용 전에도 호출.</summary>
    public void Initialize()
    {
        _model?.Initialize();
        _aggro?.Initialize(_model);
        _detector?.Initialize(_model);
        if (_detector != null && _aggro != null)
            _detector.OnCharacterDetected += _aggro.AddAggroFromDistance;

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

        if (_detector != null && _aggro != null)
            _detector.OnCharacterDetected -= _aggro.AddAggroFromDistance;

        var combat = UnityEngine.Object.FindFirstObjectByType<CombatController>();
        combat?.UnregisterFromCombat(this);
    }

    /// <summary>Model.OnDeath 구독. 죽음 관련 처리(팀 해제 등). 풀링 시에도 사망 시점에 한 번만 호출.</summary>
    private void HandleDeath()
    {
        var enemyId = _model?.Data?.enemyId;
        if (!string.IsNullOrEmpty(enemyId))
            PlaySceneEventHub.OnEnemyKilled?.Invoke(enemyId);

        OnDestroyed?.Invoke(this);
    }
}
