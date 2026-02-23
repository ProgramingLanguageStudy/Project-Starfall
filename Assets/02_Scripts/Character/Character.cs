using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 캐릭터(스쿼드 멤버) = Model·Mover·Animator·Interactor·Attacker·StateMachine 조합.
/// SquadController가 이 컴포넌트를 PlayerCharacter로 참조하여 입력·카메라 연결.
/// </summary>
[RequireComponent(typeof(CharacterModel)), RequireComponent(typeof(CharacterAnimator)), RequireComponent(typeof(CharacterStateMachine)),
 RequireComponent(typeof(CharacterMover)), RequireComponent(typeof(CharacterFollowMover)), RequireComponent(typeof(CharacterAttacker)), RequireComponent(typeof(CharacterInteractor))]
public class Character : MonoBehaviour, IInteractReceiver
{
    [Header("----- 부품 -----")]
    [SerializeField] private CharacterModel _model;
    [SerializeField] private CharacterMover _mover;
    [SerializeField] private CharacterFollowMover _followMover;
    [SerializeField] private CharacterAnimator _characterAnimator;
    [SerializeField] private CharacterInteractor _interactor;
    [SerializeField] private CharacterAttacker _attacker;
    [SerializeField] private CharacterStateMachine _stateMachine;
    [SerializeField] private AnimatorEventBridge _animatorEventBridge;

    [Header("----- 주입용 참조 -----")]
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private NavMeshAgent _navMeshAgent;
    [SerializeField] private Animator _animator;
    [SerializeField] [Tooltip("동료 전용. 인스펙터에서 CompanionAI 할당")]
    private CompanionAI _companionAI;

    public CharacterModel Model => _model;
    public CharacterMover Mover => _mover;
    public CharacterAnimator Animator => _characterAnimator;
    public CharacterInteractor Interactor => _interactor;
    public CharacterAttacker Attacker => _attacker;
    public CharacterStateMachine StateMachine => _stateMachine;

    public bool CanMove { get; set; } = true;

    public void RequestAttack()
    {
        _stateMachine?.RequestAttack();
    }

    public void Teleport(Vector3 worldPosition)
    {
        if (_characterController == null)
        {
            transform.position = worldPosition;
            return;
        }
        _characterController.enabled = false;
        transform.position = worldPosition;
        _characterController.enabled = true;
    }

    public void Teleport(Transform destination)
    {
        if (destination == null) return;
        Teleport(destination.position);
    }

    public void SetCharacterControllerEnabled(bool enabled)
    {
        if (_characterController != null)
            _characterController.enabled = enabled;
    }

    private void Update()
    {
        if (_characterAnimator == null) return;
        // 동료 모드(NavMeshAgent 사용)일 때는 FollowMover 속도 사용, 플레이어 모드일 때는 Mover 속도 사용
        bool useFollowMover = _navMeshAgent != null && _navMeshAgent.enabled;
        float speed = useFollowMover && _followMover != null
            ? _followMover.GetCurrentSpeed()
            : (_mover != null ? _mover.GetCurrentSpeed() : 0f);
        _characterAnimator.Move(speed);
    }

    /// <summary>AI 따라가기 모드: 따라갈 대상 설정. CharacterFollowMover가 있으면 사용.</summary>
    public void SetFollowTarget(Transform target)
    {
        _followMover?.SetFollowTarget(target);
    }

    /// <summary>전투 모드: 적 등 타겟으로 이동. stopDistance 안이면 정지. 동료 AI용.</summary>
    public void SetCombatTarget(Transform target, float stopDistance)
    {
        _followMover?.SetCombatTarget(target, stopDistance);
    }

    /// <summary>전투 타겟 해제. 다시 follow 대상 따라감.</summary>
    public void ClearCombatTarget()
    {
        _followMover?.ClearCombatTarget();
    }

    /// <summary>플레이어 조종 모드로 전환 (CharacterController+CharacterMover, NavMeshAgent 비활성화, Interactor 활성화).</summary>
    public void SetAsPlayer()
    {
        if (_characterController != null) _characterController.enabled = true;
        if (_navMeshAgent != null) _navMeshAgent.enabled = false;
        if (_interactor != null) _interactor.enabled = true;
        SetFollowTarget(null);
        ClearCombatTarget();
        if (_companionAI != null) _companionAI.enabled = false;
    }

    /// <summary>동료 모드로 전환 (NavMeshAgent+FollowMover로 대상 따라감, Interactor 비활성화).</summary>
    public void SetAsCompanion(Transform followTarget)
    {
        if (_characterController != null) _characterController.enabled = false;
        if (_navMeshAgent != null) _navMeshAgent.enabled = true;
        if (_interactor != null) _interactor.enabled = false;
        SetFollowTarget(followTarget);
        ClearCombatTarget();
        if (_companionAI != null) _companionAI.enabled = true;
    }

    /// <summary>현재 위치·회전·체력을 저장용 데이터로 반환.</summary>
    public PlayerSaveData GetSaveData()
    {
        var d = new PlayerSaveData();
        var id = _model?.Data?.characterId;
        d.playerCharacterId = !string.IsNullOrEmpty(id) ? id : (_model?.Data?.displayName ?? "");
        d.position = transform.position;
        d.rotationY = transform.eulerAngles.y;
        d.currentHp = _model != null ? _model.CurrentHp : 0;
        return d;
    }

    /// <summary>로드한 데이터를 실제 파츠에 적용.</summary>
    public void ApplySaveData(PlayerSaveData data)
    {
        if (data == null) return;
        Teleport(data.position);
        transform.eulerAngles = new Vector3(0f, data.rotationY, 0f);
        _model?.SetCurrentHpForLoad(data.currentHp);
    }

    public void Initialize()
    {
        if (_model == null) _model = GetComponent<CharacterModel>();
        if (_mover == null) _mover = GetComponent<CharacterMover>();
        if (_followMover == null) _followMover = GetComponent<CharacterFollowMover>();
        if (_characterAnimator == null) _characterAnimator = GetComponent<CharacterAnimator>();
        if (_interactor == null) _interactor = GetComponent<CharacterInteractor>();
        if (_attacker == null) _attacker = GetComponent<CharacterAttacker>();
        if (_stateMachine == null) _stateMachine = GetComponent<CharacterStateMachine>();
        if (_animatorEventBridge == null) _animatorEventBridge = GetComponentInChildren<AnimatorEventBridge>();
        if (_characterController == null) _characterController = GetComponent<CharacterController>();
        if (_navMeshAgent == null) _navMeshAgent = GetComponent<NavMeshAgent>();
        if (_animator == null) _animator = GetComponentInChildren<Animator>();

        _model?.Initialize();
        if (_mover != null)
            _mover.Initialize(_characterController, _model);
        if (_followMover != null)
            _followMover.Initialize(_navMeshAgent, _model);
        _characterAnimator?.Initialize(_animator);
        _interactor?.Initialize(this);
        _stateMachine?.Initialize(this);
        _attacker?.Initialize(this, _stateMachine, _model);

        _animatorEventBridge = GetComponentInChildren<AnimatorEventBridge>();
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
