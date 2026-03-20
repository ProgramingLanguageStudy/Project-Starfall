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

    /// <summary>이동 적용. MoveState.Update에서 호출. 플레이어=방향, 동료=AIBrain 타겟 읽음.</summary>
    public void ApplyMovement()
    {
        if (_isPlayer)
        {
            _mover?.Move(_currentMoveDirection);
        }
        else
        {
            var target = _aiBrain?.CurrentTarget;
            if (target != null)
            {
                var stopDist = _aiBrain.CurrentStopDistance;
                _followMover?.MoveToTarget(target.position, stopDist);
            }
        }
    }

    /// <summary>즉시 정지. 공격 진입·사망 등.</summary>
    public void StopMovement()
    {
        if (_isPlayer)
            _mover?.Stop();
        else
            _followMover?.Stop();
    }

    /// <summary>죽음 처리. DeadState.Enter에서 호출.</summary>
    public void Die()
    {
        StopMovement();
        _characterAnimator?.Dead();
        if (_characterController != null)
            _characterController.enabled = false;
    }

    /// <summary>부활 처리. DeadState.Exit에서 호출.</summary>
    public void Revive()
    {
        if (_characterController != null)
            _characterController.enabled = true;
    }

    /// <summary>동료용. 직접 목표 위치로 이동 요청. (레거시/특수용)</summary>
    public void RequestMoveToTarget(Vector3 targetPos)
    {
        _followMover.MoveToTarget(targetPos);
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

        if (_characterController == null)
        {
            transform.position = worldPosition;
            return;
        }

        _characterController.enabled = false;
        transform.position = worldPosition;
        _characterController.enabled = true;
    }

    /// <summary>플레이어 조종 모드로 전환.</summary>
    public void SetAsPlayer()
    {
        _isPlayer = true;
        if (_characterController != null) _characterController.enabled = true;
        if (_navMeshAgent != null) _navMeshAgent.enabled = false;
        if (_interactor != null) _interactor.enabled = true;
        gameObject.layer = LayerMask.NameToLayer(LayerParams.Player);

        if (_aiBrain != null) _aiBrain.enabled = false;
    }

    /// <summary>동료 모드로 전환.</summary>
    public void SetAsCompanion(Transform followTarget)
    {
        _isPlayer = false;
        if (_characterController != null) _characterController.enabled = false;
        if (_navMeshAgent != null) _navMeshAgent.enabled = true;
        if (_interactor != null) _interactor.enabled = false;
        gameObject.layer = LayerMask.NameToLayer(LayerParams.Character);

        if (_aiBrain != null) _aiBrain.enabled = true;
    }

    public void Initialize(CombatController combatController = null, Squad squad = null)
    {
        Squad = squad;

        if (_model == null)
        {
            Debug.LogError($"[Character] CharacterModel is null (Awake/cache). GameObject={gameObject.name}");
            return;
        }

        _aiBrain?.Initialize(this, combatController);

        _model?.Initialize();
        _mover.Initialize(_characterController, _model);
        _followMover.Initialize(_navMeshAgent, _model);
        _characterAnimator?.Initialize(_animator);
        _interactor?.Initialize(this);
        _stateMachine?.Initialize(this);
        _attacker?.Initialize(this, _stateMachine, _model, _characterAnimator);

        if (_animatorEventBridge != null && _attacker != null)
        {
            _animatorEventBridge.OnBeginHitWindow += _attacker.Animation_BeginHitWindow;
            _animatorEventBridge.OnEndHitWindow += _attacker.Animation_EndHitWindow;
            _animatorEventBridge.OnAttackEnded += _attacker.Animation_OnAttackEnded;
            Debug.Log($"[Character] {gameObject.name} AnimatorEventBridge 연결 완료");
        }
        else if (_animatorEventBridge == null)
            Debug.LogWarning($"[Character] {gameObject.name} AnimatorEventBridge 없음 (애니 이벤트 동작 안 함)");
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

    private void Update()
    {
        if (_characterAnimator == null) return;

        bool isMove = StateMachine != null && StateMachine.CurrentState == CharacterState.Move;
        _characterAnimator.SetMoving(isMove);

        float speed = isMove && _model != null ? _model.CurrentMoveSpeed : 0f;
        _characterAnimator.Move(speed);
    }

    private void OnDisable()
    {
        if (_animatorEventBridge != null && _attacker != null)
        {
            _animatorEventBridge.OnBeginHitWindow -= _attacker.Animation_BeginHitWindow;
            _animatorEventBridge.OnEndHitWindow -= _attacker.Animation_EndHitWindow;
            _animatorEventBridge.OnAttackEnded -= _attacker.Animation_OnAttackEnded;
        }
    }
}