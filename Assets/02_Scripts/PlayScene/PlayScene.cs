using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// 플레이 씬 조율. 인스펙터 연결 검증 → 코루틴으로 GameManager 부트 대기 → <see cref="Initialize"/> 후 <see cref="IsSceneReady"/>·인스턴스 플래그로 입력·Update 방어.
/// </summary>
public class PlayScene : MonoBehaviour
{
    #region Constants

    /// <summary>GameManager.Instance·BootServicesReady 대기 상한(초). SceneLoadManager와 동일.</summary>
    private const float BootWaitTimeoutSeconds = 120f;

    #endregion

    #region SerializeField

    [SerializeField] [Tooltip("플레이 입력")]
    private InputHandler _inputHandler;
    [SerializeField] [Tooltip("이동 입력→월드 방향 변환에 사용하는 카메라 Transform")]
    private Transform _cameraTransform;
    [SerializeField] [Tooltip("적 스폰 루트 Transform. 자식(및 자기) EnemySpawner 전원에 CombatController 주입")]
    private Transform _enemySpawnRoot;
    [SerializeField] [Tooltip("분대 스폰·따라가기·조종 캐릭터")]
    private SquadController _squadController;
    [SerializeField] [Tooltip("전투 On/Off. 외부에서 SetCombatOn/Off")]
    private CombatController _combatController;
    [SerializeField] [Tooltip("인벤토리 M-V. 플레이어 변경 시 ItemUser 갱신")]
    private InventoryPresenter _inventoryPresenter;
    [SerializeField] [Tooltip("체력바·분대 프로필 등 HUD. 조종 캐릭터 Model.OnHpChanged")]
    private PlaySceneView _playSceneView;
    [SerializeField] [Tooltip("조종 캐릭터 Follow. SquadSwap 시 타겟 갱신")]
    private CinemachineCamera _cinemachineCamera;
    [SerializeField] [Tooltip("세이브/로드 조율·Contributor")]
    private PlaySaveCoordinator _saveCoordinator;
    [SerializeField] [Tooltip("대화 컴포넌트·이벤트 연결")]
    private DialogueController _dialogueController;
    [SerializeField] [Tooltip("퀘스트 UI(M-V). SaveCoordinator·QuestController에 System 주입")]
    private QuestPresenter _questPresenter;
    [SerializeField] [Tooltip("퀘스트 수락·완료. DialogueController 연동")]
    private QuestController _questController;
    [SerializeField] [Tooltip("스토리 플래그 저장·조회")]
    private FlagSystem _flagSystem;
    [SerializeField] [Tooltip("NPC 등록·조회")]
    private NpcController _npcController;
    [SerializeField] [Tooltip("미니맵·맵 토글·스크롤")]
    private MapController _mapController;
    [SerializeField] [Tooltip("포탈. 맵 뷰·플래그 연동")]
    private PortalController _portalController;
    [SerializeField] [Tooltip("설정 패널")]
    private SettingsView _settingsView;
    [SerializeField] [Tooltip("힌트·픽업 로그 등 짧은 오버레이 UI")]
    private PlaySceneOverlayController _overlayController;
    [SerializeField] [Tooltip("레벨업 제단 Presenter")]
    private LevelUpAltarPresenter _levelUpAltarPresenter;

    #endregion

    #region Internal State

    private CharacterModel _hpModelSubscribed; // 체력바용 OnHpChanged 구독 대상
    private SaveData _pendingSaveData; // ApplySaveData 직전 버퍼

    #endregion

    #region Public API

    /// <summary>
    /// <see cref="Initialize"/>가 끝난 뒤 true. <see cref="OnDisable"/>에서 false.
    /// 늦게 켜진 스포너는 <see cref="OnSceneReady"/> 구독 전에 이 값을 보고 즉시 초기화할 수 있음.
    /// </summary>
    public static bool IsSceneReady { get; private set; }

    /// <summary>씬 초기화 완료 시 발생. SceneLoadManager가 로딩 UI 숨김용으로 구독.</summary>
    public static event Action OnSceneReady;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        IsSceneReady = false;

        if (!ValidateBoundReferences())
            return;
    }

    private void Start()
    {
        StartCoroutine(WaitForBootThenInitializeRoutine());
    }

    private void OnEnable()
    {
        if (_inputHandler == null || _squadController == null) return;

        // Initialize 전 콜백은 OnSquad*에서 IsSceneReady로 무시
        _squadController.OnPlayerChanged += OnSquadPlayerChanged;
        _squadController.OnMembersChanged += OnSquadMembersChanged;

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
        IsSceneReady = false;

        GameManager.Instance?.PoolManager?.RemoveDestroyedFromAllPools();
        PlaySceneEventHub.Clear();
        PlaySceneRegistry.Clear();

        if (_hpModelSubscribed != null)
        {
            _hpModelSubscribed.OnHpChanged -= OnHpChanged;
            _hpModelSubscribed = null;
        }
        if (_squadController != null)
        {
            _squadController.OnPlayerChanged -= OnSquadPlayerChanged;
            _squadController.OnMembersChanged -= OnSquadMembersChanged;
        }
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
        if (!IsSceneReady) return;

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

        _mapController?.RequestScrollMap(_inputHandler.ScrollInput);
    }

    #endregion

    #region Private Helpers

    /// <summary>루트 아래(포함) 모든 <see cref="EnemySpawner"/>에 전투 컨트롤러 주입·스폰. 참조는 <see cref="ValidateBoundReferences"/> 이후에만 호출.</summary>
    private static void InitializeEnemySpawnersUnderRoot(Transform root, CombatController combatController)
    {
        var spawners = root.GetComponentsInChildren<EnemySpawner>(true);
        for (int i = 0; i < spawners.Length; i++)
            spawners[i].Initialize(combatController);
    }

    /// <summary>인스펙터 연결 참조가 하나라도 없으면 에러 로그 후 false.</summary>
    private bool ValidateBoundReferences()
    {
        bool ok = true;
        ok = this.CheckComponent(_inputHandler) && ok;
        ok = this.CheckComponent(_cameraTransform) && ok;
        ok = this.CheckComponent(_enemySpawnRoot) && ok;
        ok = this.CheckComponent(_squadController) && ok;
        ok = this.CheckComponent(_combatController) && ok;
        ok = this.CheckComponent(_inventoryPresenter) && ok;
        ok = this.CheckComponent(_playSceneView) && ok;
        ok = this.CheckComponent(_cinemachineCamera) && ok;
        ok = this.CheckComponent(_saveCoordinator) && ok;
        ok = this.CheckComponent(_dialogueController) && ok;
        ok = this.CheckComponent(_questPresenter) && ok;
        ok = this.CheckComponent(_questController) && ok;
        ok = this.CheckComponent(_flagSystem) && ok;
        ok = this.CheckComponent(_npcController) && ok;
        ok = this.CheckComponent(_mapController) && ok;
        ok = this.CheckComponent(_portalController) && ok;
        ok = this.CheckComponent(_settingsView) && ok;
        ok = this.CheckComponent(_overlayController) && ok;
        ok = this.CheckComponent(_levelUpAltarPresenter) && ok;
        return ok;
    }

    /// <summary>GM 존재·부트 완료까지 대기 후 <see cref="Initialize"/>.</summary>
    private IEnumerator WaitForBootThenInitializeRoutine()
    {
        float waitStart = Time.realtimeSinceStartup;

        while (GameManager.Instance == null || !GameManager.Instance.BootServicesReady)
        {
            if (Time.realtimeSinceStartup - waitStart > BootWaitTimeoutSeconds)
            {
                Debug.LogError("[PlayScene] GameManager 부트 대기 시간 초과.");
                yield break;
            }

            yield return null;
        }

        Initialize();
    }

    /// <summary>세이브·분대·UI·맵 등 플레이 씬 일괄 초기화. 끝에서 <see cref="IsSceneReady"/> 및 <see cref="OnSceneReady"/>.</summary>
    private void Initialize()
    {
        var gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("[PlayScene] Initialize: GameManager is null.");
            return;
        }

        // 세이브 스냅샷 (Apply는 분대 주입 이후)
        _pendingSaveData = gm.SaveManager != null ? gm.SaveManager.LoadedSaveData : null;

        // 세이브 코디네이터·플레이 UI
        _saveCoordinator?.Initialize(
            _squadController,
            _flagSystem,
            _questPresenter,
            _inventoryPresenter?.Model);


        if (_settingsView != null)
            _settingsView.Initialize();
        _playSceneView?.Initialize();
        _overlayController?.Initialize();
        _levelUpAltarPresenter?.Initialize(_inventoryPresenter?.Model);

        // 시네: 초기화 중 렌더 끄고, 세이브 반영 후 다시 켬
        if (_cinemachineCamera != null)
            _cinemachineCamera.gameObject.SetActive(false);

        // 분대·전투·NPC·인벤
        _squadController.Initialize(_combatController);
        _npcController?.Initialize();

        var player = _squadController.PlayerCharacter;
        InitializeEnemySpawnersUnderRoot(_enemySpawnRoot, _combatController);

        if (_inventoryPresenter != null)
            _inventoryPresenter.SetPlayerCharacter(player);

        // 대화·퀘스트
        _dialogueController?.Initialize(_questController, _flagSystem);
        if (_questController != null && _questPresenter != null && _inventoryPresenter?.Model != null)
            _questController.Initialize(_questPresenter.System, _inventoryPresenter.Model, _flagSystem, _squadController);

        // 맵·포탈
        if (_mapController != null)
            _mapController.Initialize(_portalController, player, _squadController);

        if (_portalController != null)
            _portalController.Initialize(_mapController.MapView, _flagSystem);

        // 세이브 적용 → 분대 슬롯 갱신
        if (_pendingSaveData != null && GameManager.Instance?.SaveManager != null)
        {
            Debug.Log($"[PlayScene] Save data squad members: {_pendingSaveData.squad?.members?.Count ?? 0}");
            GameManager.Instance.SaveManager.ApplySaveData(_pendingSaveData);
            _pendingSaveData = null;
        }
        else
        {
            Debug.LogWarning("[PlayScene] No save data to apply - creating default squad");
            // 신규 게임일 때 기본 캐릭터 생성 로직 필요
        }

        // ApplySaveData 후 프로필·체력바·카메라 팔로우 (SetSlots는 OnMembersChanged 미호출)
        RefreshSquadProfileView(_squadController.Characters);
        HandlePlayerChanged(_squadController.PlayerCharacter);

        if (_cinemachineCamera != null)
            _cinemachineCamera.gameObject.SetActive(true);

        IsSceneReady = true;
        OnSceneReady?.Invoke();
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

    private void OnSquadPlayerChanged(Character newPlayer)
    {
        if (!IsSceneReady) return;
        HandlePlayerChanged(newPlayer);
    }

    private void OnSquadMembersChanged(IReadOnlyList<Character> slots)
    {
        if (!IsSceneReady) return;
        RefreshSquadProfileView(slots);
    }

    private void HandleInteract()
    {
        if (!IsSceneReady) return;
        _squadController?.RequestInteract();
    }

    private void HandleInventoryKey()
    {
        if (!IsSceneReady) return;
        _inventoryPresenter?.RequestToggleInventory();
    }

    private void HandleAttack()
    {
        if (!IsSceneReady) return;
        _squadController?.RequestAttack();
    }

    private void HandleSquadSwap()
    {
        if (!IsSceneReady) return;
        _squadController?.RequestSquadSwap();
    }

    private void HandleSave()
    {
        if (!IsSceneReady) return;
        _saveCoordinator?.RequestSave();
    }

    private void HandleMap()
    {
        if (!IsSceneReady) return;
        _mapController?.RequestToggleMap();
    }

    private void HandleSettings()
    {
        if (!IsSceneReady) return;
        _settingsView?.RequestToggle();
    }

    private void HandleEscapeRequested()
    {
        if (!IsSceneReady) return;
        _squadController?.TeleportToDefaultPoint();
    }

    /// <summary>플레이어 변경 시 chase/follow/인벤토리/체력바/카메라 등 갱신.</summary>
    private void HandlePlayerChanged(Character newPlayer)
    {
        if (newPlayer != null && _cinemachineCamera != null)
            _cinemachineCamera.Follow = newPlayer.transform;
        _inventoryPresenter?.SetPlayerCharacter(newPlayer);

        if (_hpModelSubscribed != null)
            _hpModelSubscribed.OnHpChanged -= OnHpChanged;

        _hpModelSubscribed = newPlayer?.Model;
        if (_hpModelSubscribed != null)
        {
            _hpModelSubscribed.OnHpChanged += OnHpChanged;
            _playSceneView?.RefreshHealth(_hpModelSubscribed.CurrentHp, _hpModelSubscribed.MaxHp);
        }

        // 분대 프로필 선택 강조 (슬롯 인덱스 기준)
        if (_squadController != null && _playSceneView != null)
        {
            int slot = newPlayer != null ? _squadController.Squad.GetSlotOf(newPlayer) : -1;
            _playSceneView.SetSelectedProfileIndex(slot);
        }
    }

    private void OnHpChanged(int currentHp, int maxHp)
    {
        if (!IsSceneReady) return;
        _playSceneView?.RefreshHealth(currentHp, maxHp);
    }

    /// <summary>분대원 추가/제거 시 프로필 슬롯 다시 바인딩.</summary>
    private void RefreshSquadProfileView(IReadOnlyList<Character> slots)
    {
        if (_playSceneView == null) return;
        _playSceneView.RefreshSquadProfiles(slots);
        int slot = _squadController.PlayerCharacter != null
            ? _squadController.Squad.GetSlotOf(_squadController.PlayerCharacter)
            : -1;
        _playSceneView.SetSelectedProfileIndex(slot);
    }

    #endregion
}
