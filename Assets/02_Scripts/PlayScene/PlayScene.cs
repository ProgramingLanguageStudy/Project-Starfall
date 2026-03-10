using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// 플레이 씬 조율. InputHandler·SquadController 참조, 입력을 PlayerCharacter에 연결.
/// </summary>
public class PlayScene : MonoBehaviour
{
    [SerializeField] private InputHandler _inputHandler;
    [SerializeField] [Tooltip("입력→월드 방향 변환에 사용. 비면 Camera.main")]
    private Transform _cameraTransform;
    [SerializeField] [Tooltip("비면 주입 안 함. 적 팀 스폰 (CombatController 주입)")]
    private EnemySpawner _enemySpawner;
    [SerializeField] [Tooltip("비면 주입 안 함. 있으면 분대 스폰·따라가기 설정")]
    private SquadController _squadController;
    [SerializeField] [Tooltip("전투 상태 On/Off 관리. 조건은 외부에서 SetCombatOn/Off 호출")]
    private CombatController _combatController;
    [SerializeField] [Tooltip("비면 주입 안 함. 있으면 플레이어 변경 시 ItemUser 갱신")]
    private InventoryPresenter _inventoryPresenter;
    [SerializeField] [Tooltip("비면 주입 안 함. 플레이 화면 UI(체력바 등). 조종 캐릭터 Model.OnHpChanged 구독")]
    private PlaySceneView _playSceneView;
    [SerializeField] [Tooltip("비면 갱신 안 함. 있으면 SquadSwap 시 Follow 타겟을 현재 조종 캐릭터로 변경")]
    private CinemachineCamera _cinemachineCamera;
    [SerializeField] [Tooltip("세이브/로드 조율. 인스펙터에서 Contributor 할당")]
    private PlaySaveCoordinator _saveCoordinator;
    [SerializeField] [Tooltip("대화 관련 컴포넌트·이벤트 연결")]
    private DialogueController _dialogueController;
    [SerializeField] [Tooltip("퀘스트 UI(M-V). SaveCoordinator·QuestController 초기화 시 System 주입용")]
    private QuestPresenter _questPresenter;
    [SerializeField] [Tooltip("퀘스트 조율·수락/완료 API. DialogueController에 주입")]
    private QuestController _questController;
    [SerializeField] [Tooltip("플래그 저장·조회. QuestSystem처럼 Play 씬에서 보유")]
    private FlagSystem _flagSystem;
    [SerializeField] [Tooltip("NPC 등록·조회 관리")]
    private NpcController _npcController;
    [SerializeField] MapController _mapController;
    [SerializeField] PortalController _portalController;
    [SerializeField] SettingsView _settingsView;

    private CharacterModel _hpModelSubscribed;
    private SaveData _pendingSaveData;

    private void Awake()
    {
        if (_inputHandler == null)
        {
            Debug.LogWarning("[PlayScene] InputHandler가 할당되지 않았습니다. 인스펙터에서 할당해 주세요.");
            return;
        }
        if (_squadController == null)
        {
            Debug.LogWarning("[PlayScene] SquadController가 할당되지 않았습니다.");
            return;
        }

        if (_cameraTransform == null && Camera.main != null)
            _cameraTransform = Camera.main.transform;

        var gm = GameManager.Instance;
        _saveCoordinator?.Initialize(
            _squadController,
            _flagSystem,
            _questPresenter,
            _inventoryPresenter?.Model);

        // 세이브 선로드 → 스폰 위치 결정 → 스폰 (세이브 있으면 멤버 목록 기준으로 동료 포함 스폰)
        _pendingSaveData = gm?.SaveManager?.Load();
        var spawnPos = _pendingSaveData?.squad != null ? (Vector3?)_pendingSaveData.squad.playerPosition : null;

        _squadController.Initialize(spawnPos, _combatController, _pendingSaveData?.squad);

        _npcController?.Initialize();

        var player = _squadController.PlayerCharacter;
        _enemySpawner?.Initialize(_combatController);

        if (_inventoryPresenter != null)
            _inventoryPresenter.SetPlayerCharacter(player);

        _dialogueController?.Initialize(_questController, _flagSystem);
        if (_questController != null && _questPresenter != null && _inventoryPresenter?.Model != null)
            _questController.Initialize(_questPresenter.System, _inventoryPresenter.Model, _flagSystem, _squadController);

        if (_mapController != null)
        {
            _mapController.Initialize(_portalController, player, _squadController);
        }

        if (_portalController != null)
        {
            _portalController.Initialize(_mapController.MapView, _flagSystem);
        }

        if (_settingsView != null)
        {
            _settingsView.Initialize();
        }
    }

    private void Start()
    {
        // Apply는 Start에서. Awake 순서 미보장으로 Inventory.Initialize 전에 Apply될 수 있음.
        if (_pendingSaveData != null && GameManager.Instance?.SaveManager != null)
        {
            GameManager.Instance.SaveManager.ApplySaveData(_pendingSaveData);
            _pendingSaveData = null;
        }
    }

    private void OnEnable()
    {
        if (_inputHandler == null || _squadController == null) return;

        _squadController.OnPlayerChanged += HandlePlayerChanged;
        HandlePlayerChanged(_squadController.PlayerCharacter);

        _inputHandler.OnInteractPerformed += HandleInteract;
        _inputHandler.OnInventoryPerformed += HandleInventoryKey;
        _inputHandler.OnAttackPerformed += HandleAttack;
        _inputHandler.OnSquadSwapPerformed += HandleSquadSwap;
        _inputHandler.OnSavePerformed += HandleSave;
        _inputHandler.OnMapPerformed += HandleMap;
        _inputHandler.OnSettingsPerformed += HandleSettings;

        if (_settingsView != null)
            _settingsView.OnEscapeRequested += HandleEscapeRequested;
    }

    private void OnDisable()
    {
        PlaySceneEventHub.Clear();

        if (_hpModelSubscribed != null)
        {
            _hpModelSubscribed.OnHpChanged -= OnHpChanged;
            _hpModelSubscribed = null;
        }
        if (_squadController != null)
            _squadController.OnPlayerChanged -= HandlePlayerChanged;
        if (_inputHandler == null) return;

        _inputHandler.OnInteractPerformed -= HandleInteract;
        _inputHandler.OnInventoryPerformed -= HandleInventoryKey;
        _inputHandler.OnAttackPerformed -= HandleAttack;
        _inputHandler.OnSquadSwapPerformed -= HandleSquadSwap;
        _inputHandler.OnSavePerformed -= HandleSave;
        _inputHandler.OnMapPerformed -= HandleMap;
        _inputHandler.OnSettingsPerformed -= HandleSettings;

        if (_settingsView != null)
            _settingsView.OnEscapeRequested -= HandleEscapeRequested;
    }

    private void Update()
    {
        var player = _squadController?.PlayerCharacter;
        if (_inputHandler == null || player == null) return;
        if (!_squadController.CanMove) return;

        Vector2 input = _inputHandler.MoveInput;
        Vector3 worldDir = InputToWorldDirection(input);
        bool hasInput = worldDir.sqrMagnitude >= 0.01f;

        // Idle↔Move: 입력 있음→Move, 없음→MoveState.IsComplete로 Idle 전환. RequestIdle 호출 안 함.
        player.SetMoveDirection(hasInput ? worldDir : Vector3.zero);
        if (hasInput)
            player.RequestMove();

        _mapController.RequestScrollMap(_inputHandler.ScrollInput);
    }

    /// <summary>입력 + 카메라 → 월드 기준 이동 방향.</summary>
    private Vector3 InputToWorldDirection(Vector2 input)
    {
        if (input.sqrMagnitude < 0.01f) return Vector3.zero;

        Transform cam = _cameraTransform != null ? _cameraTransform : Camera.main?.transform;
        if (cam == null) return Vector3.zero;

        Vector3 forward = cam.forward;
        Vector3 right = cam.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        return (forward * input.y + right * input.x).normalized;
    }

    private void HandleInteract()
    {
        _squadController?.RequestInteract();
    }

    private void HandleInventoryKey()
    {
        _inventoryPresenter?.RequestToggleInventory();
    }

    private void HandleAttack()
    {
        _squadController?.RequestAttack();
    }

    private void HandleSquadSwap()
    {
        _squadController?.RequestSquadSwap();
    }

    private void HandleSave()
    {
        _saveCoordinator?.RequestSave();
    }

    private void HandleMap()
    {
        _mapController?.RequestToggleMap();
    }

    private void HandleSettings()
    {
        _settingsView?.RequestToggle();
    }

    private void HandleEscapeRequested()
    {
        _squadController?.TeleportToDefaultPoint();
    }

    /// <summary>플레이어 변경 시 chase/follow/인벤토리/체력바/카메라 등 갱신.</summary>
    private void HandlePlayerChanged(Character newPlayer)
    {
        var chaseTarget = newPlayer != null ? newPlayer.transform : _squadController?.transform;
        if (chaseTarget != null && _cinemachineCamera != null)
            _cinemachineCamera.Follow = chaseTarget;
        _inventoryPresenter?.SetPlayerCharacter(newPlayer);

        if (_hpModelSubscribed != null)
            _hpModelSubscribed.OnHpChanged -= OnHpChanged;

        _hpModelSubscribed = newPlayer?.Model;
        if (_hpModelSubscribed != null)
        {
            _hpModelSubscribed.OnHpChanged += OnHpChanged;
            _playSceneView?.RefreshHealth(_hpModelSubscribed.CurrentHp, _hpModelSubscribed.MaxHp);
        }
    }

    private void OnHpChanged(int currentHp, int maxHp)
    {
        _playSceneView?.RefreshHealth(currentHp, maxHp);
    }
}
