using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 캐릭터(스쿼드 멤버) = Model·Mover·Animator·Interactor·Attacker·StateMachine 조합.
/// SquadController가 이 컴포넌트를 PlayerCharacter로 참조하여 입력·카메라 연결.
/// </summary>
[RequireComponent(typeof(CharacterModel)), RequireComponent(typeof(CharacterAnimator)),
 RequireComponent(typeof(CharacterStateMachine)),
 RequireComponent(typeof(CharacterMover)), RequireComponent(typeof(CharacterFollowMover)),
 RequireComponent(typeof(CharacterAttacker)), RequireComponent(typeof(CharacterInteractor))]
public class Character : MonoBehaviour, IInteractReceiver
{
    private CharacterModel _model;
    private CharacterMover _mover;
    private CharacterFollowMover _followMover;
    private CharacterAnimator _characterAnimator;
    private CharacterInteractor _interactor;
    private CharacterAttacker _attacker;
    private CharacterStateMachine _stateMachine;
    private AnimatorEventBridge _animatorEventBridge;
    private CharacterController _characterController;
    private NavMeshAgent _navMeshAgent;
    private Animator _animator;
    private AIBrain _aiBrain;
    // ── 이동 (플레이어=방향, 동료=타겟. 타겟은 AIBrain이 관리) ─
    private Vector3 _currentMoveDirection;

    private bool _isPlayer;

    // ── Public 프로퍼티 ─────────────────────────────────────
    public CharacterModel Model => _model;
    public CharacterMover Mover => _mover;
    public CharacterFollowMover FollowMover => _followMover;
    public CharacterAnimator Animator => _characterAnimator;
    public CharacterInteractor Interactor => _interactor;
    public CharacterAttacker Attacker => _attacker;
    public CharacterStateMachine StateMachine => _stateMachine;

    public bool CanMove => StateMachine != null && StateMachine.CanMove;

    /// <summary>플레이어 조종 여부. SetAsPlayer/SetAsCompanion 시 설정. SquadController 등이 읽음.</summary>
    public bool IsPlayer => _isPlayer;

    /// <summary>소속 분대. 스폰 시 주입. Enemy가 Character→Squad→Player로 타겟 해석.</summary>
    public Squad Squad { get; private set; }

    /// <summary>전투 시스템 참조. AIBrain 등이 타겟 탐색 시 사용.</summary>
    public CombatController CombatController { get; private set; }

    // ── 공개 API (StateMachine·InputHandler·AIBrain 연동) ────

    public void RequestAttack() => _stateMachine?.RequestAttack();

    /// <summary>Move 상태로 전환. 플레이어/AI 공통. 방향·타겟은 호출 전 설정.</summary>
    public void RequestMove()
    {
        _stateMachine?.RequestMove();
    }

    public void RequestIdle() => _stateMachine?.RequestIdle();

    /// <summary>플레이어용. PlayScene에서 입력→월드 방향 설정.</summary>
    public void SetMoveDirection(Vector3 worldDir)
    {
        _currentMoveDirection = worldDir;
    }

    /// <summary>플레이어용. MoveState.IsComplete에서 이동 입력 유무 판단.</summary>
    public bool HasMoveInput => _currentMoveDirection.sqrMagnitude >= 0.01f;

    public void ApplyMovement()
    {
        if (_isPlayer)
        {
            _mover.Move(_currentMoveDirection);
        }
        else if (_aiBrain != null && _aiBrain.CurrentTarget != null)
        {
            _followMover.MoveToTarget(_aiBrain.CurrentTarget.position, _aiBrain.CurrentStopDistance);
        }
    }

    /// <summary>즉시 정지. 공격 진입·사망 등.</summary>
    public void StopMovement()
    {
        if (_isPlayer) _mover.Stop();
        else _followMover.Stop();
    }

    /// <summary>죽음 처리. DeadState.Enter에서 호출.</summary>
    public void Die()
    {
        StopMovement();
        _characterAnimator.Dead();
        SetPhysicsActive(false);
    }

    /// <summary>부활 처리. DeadState.Exit에서 호출.</summary>
    public void Revive()
    {
        SetPhysicsActive(true);
    }

    private void SetPhysicsActive(bool active)
    {
        if (_characterController != null) _characterController.enabled = active;
    }

    /// <summary>NavMeshAgent 경로 초기화. RepositionCompanionsAround 등에서 Warp 전 호출.</summary>
    public void ResetNavMeshPath()
    {
        if (_navMeshAgent != null) _navMeshAgent.ResetPath();
    }

    public void Teleport(Vector3 worldPosition)
    {
        if (_navMeshAgent != null && _navMeshAgent.enabled)
        {
            _navMeshAgent.Warp(worldPosition);
            return;
        }

        bool wasEnabled = _characterController != null && _characterController.enabled;
        if (wasEnabled) _characterController.enabled = false;
        
        transform.position = worldPosition;
        
        if (wasEnabled) _characterController.enabled = true;
    }

    /// <summary>플레이어 조종 모드로 전환.</summary>
    public void SetAsPlayer()
    {
        SetControlMode(true);
    }

    /// <summary>동료 모드로 전환.</summary>
    public void SetAsCompanion(Transform followTarget)
    {
        SetControlMode(false);
    }

    private void SetControlMode(bool isPlayer)
    {
        _isPlayer = isPlayer;

        // 컴포넌트 상태 스왑
        if (_characterController != null) _characterController.enabled = isPlayer;
        if (_navMeshAgent != null) _navMeshAgent.enabled = !isPlayer;
        if (_interactor != null) _interactor.enabled = isPlayer;
        if (_aiBrain != null) _aiBrain.enabled = !isPlayer;

        // 레이어 설정
        gameObject.layer = LayerMask.NameToLayer(isPlayer ? LayerParams.Player : LayerParams.Character);
    }

    public void Initialize(CombatController combatController = null, Squad squad = null)
    {
        Squad = squad;
        CombatController = combatController;

        // 필수 컴포넌트 초기화 (상위에서 하위로 주입)
        _model.Initialize();
        _mover.Initialize(_characterController, _model);
        _followMover.Initialize(_navMeshAgent, _model);
        _characterAnimator.Initialize(_animator);
        _interactor.Initialize(this);
        _stateMachine.Initialize(this);
        _attacker.Initialize(_model, _stateMachine, _characterAnimator, transform);
        _aiBrain?.Initialize(this, combatController);

        // 이벤트 연결
        BindEvents(true);
    }

    private void BindEvents(bool bind)
    {
        if (_stateMachine != null)
        {
            if (bind) _stateMachine.OnStateChanged += HandleStateChanged;
            else _stateMachine.OnStateChanged -= HandleStateChanged;
        }

        if (_animatorEventBridge != null && _attacker != null)
        {
            if (bind)
            {
                _animatorEventBridge.OnBeginHitWindow += _attacker.Animation_BeginHitWindow;
                _animatorEventBridge.OnEndHitWindow += _attacker.Animation_EndHitWindow;
                _animatorEventBridge.OnAttackEnded += _attacker.Animation_OnAttackEnded;
            }
            else
            {
                _animatorEventBridge.OnBeginHitWindow -= _attacker.Animation_BeginHitWindow;
                _animatorEventBridge.OnEndHitWindow -= _attacker.Animation_EndHitWindow;
                _animatorEventBridge.OnAttackEnded -= _attacker.Animation_OnAttackEnded;
            }
        }
    }

    // ── Unity ───────────────────────────────────────────────

    private void Awake()
    {
        _model = GetComponent<CharacterModel>();
        _mover = GetComponent<CharacterMover>();
        _followMover = GetComponent<CharacterFollowMover>();
        _characterAnimator = GetComponent<CharacterAnimator>();
        _interactor = GetComponent<CharacterInteractor>();
        _attacker = GetComponent<CharacterAttacker>();
        _stateMachine = GetComponent<CharacterStateMachine>();
        _animatorEventBridge = GetComponentInChildren<AnimatorEventBridge>();
        _characterController = GetComponent<CharacterController>();
        _navMeshAgent = GetComponent<NavMeshAgent>();
        _animator = GetComponentInChildren<Animator>();
        _aiBrain = GetComponent<AIBrain>();
    }

    private void HandleStateChanged(CharacterState previous, CharacterState current)
    {
        if (_characterAnimator == null) return;

        bool isMove = current == CharacterState.Move;
        _characterAnimator.SetMoving(isMove);

        // 이동 상태로 진입 시 모델의 이동 속도 전달 (애니메이터 댐핑 사용)
        float speed = isMove && _model != null ? _model.CurrentMoveSpeed : 0f;
        _characterAnimator.Move(speed);
    }

    private void OnDisable()
    {
        BindEvents(false);
    }
}