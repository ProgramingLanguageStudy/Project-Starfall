using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// PlayerInput이 참조하는 Input Action Asset을 사용. Cinemachine과 같은 인스턴스 공유 시 UI 모드에서 Look도 함께 꺼짐.
/// GameEvents OnCursorShowRequested/HideRequested로 blocking UI 개수 카운트 후, 1개 이상이면 Player 맵 비활성화(UI 모드).
/// </summary>
public class InputHandler : MonoBehaviour
{
    [Tooltip("PlayerInput 할당 필수. 같은 GameObject에 두거나, 에셋을 공유하는 PlayerInput을 넣으면 됨.")]
    [SerializeField] private PlayerInput _playerInput;

    private InputActionAsset _asset;
    private InputActionMap _playerMap;
    private InputActionMap _uiMap;
    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _uiInventoryAction;
    private InputAction _uiMapAction;
    private InputAction _uiScrollAction;
    private int _blockingUICount;

    private Action<InputAction.CallbackContext> _interactCallback;
    private Action<InputAction.CallbackContext> _attackCallback;
    private Action<InputAction.CallbackContext> _inventoryCallback;
    private Action<InputAction.CallbackContext> _squadSwapCallback;
    private Action<InputAction.CallbackContext> _saveCallback;
    private Action<InputAction.CallbackContext> _mapCallback;
    private Action<InputAction.CallbackContext> _settingsCallback;

    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public Vector2 ScrollInput { get; private set; }

    /// <summary>true면 Player 액션 맵 비활성화(대화/인벤토리 등 UI 모드).</summary>
    public bool IsUIMode => _blockingUICount > 0;

    public event Action OnInteractPerformed;
    public event Action OnAttackPerformed;
    public event Action OnInventoryPerformed;
    public event Action OnSquadSwapPerformed;
    public event Action OnSavePerformed;
    public event Action OnMapPerformed;
    public event Action OnSettingsPerformed;

    private void OnEnable()
    {
        if (_playerInput == null)
        {
            Debug.LogWarning("[InputHandler] PlayerInput이 할당되지 않았습니다. 인스펙터에서 PlayerInput을 할당해 주세요.");
            return;
        }

        _asset = _playerInput.actions;
        if (_asset == null)
        {
            Debug.LogWarning("[InputHandler] PlayerInput.actions가 없습니다.");
            return;
        }

        _playerMap = _asset.FindActionMap("Player");
        if (_playerMap == null)
        {
            Debug.LogWarning("[InputHandler] Player 액션 맵을 찾을 수 없습니다.");
            return;
        }

        _moveAction = _playerMap.FindAction("Move");
        _lookAction = _playerMap.FindAction("Look");

        _interactCallback = _ => OnInteractPerformed?.Invoke();
        _attackCallback = _ => OnAttackPerformed?.Invoke();
        _inventoryCallback = _ => OnInventoryPerformed?.Invoke();
        _squadSwapCallback = _ => OnSquadSwapPerformed?.Invoke();
        _saveCallback = _ => OnSavePerformed?.Invoke();
        _mapCallback = _ => OnMapPerformed?.Invoke();
        _settingsCallback = _ => OnSettingsPerformed?.Invoke();

        var interact = _playerMap.FindAction("Interact");
        var attack = _playerMap.FindAction("Attack");
        var inventory = _playerMap.FindAction("Inventory");
        var squadSwap = _playerMap.FindAction("SquadSwap");
        var save = _playerMap.FindAction("Save");
        var map = _playerMap.FindAction("Map");
        var settings = _playerMap.FindAction("Settings");
        if (interact != null) interact.performed += _interactCallback;
        if (attack != null) attack.performed += _attackCallback;
        if (inventory != null) inventory.performed += _inventoryCallback;
        if (squadSwap != null) squadSwap.performed += _squadSwapCallback;
        if (save != null) save.performed += _saveCallback;
        if (map != null) map.performed += _mapCallback;
        if (settings != null) settings.performed += _settingsCallback;

        _playerMap.Enable();

        _uiMap = _asset.FindActionMap("UI");
        if (_uiMap != null)
        {
            _uiInventoryAction = _uiMap.FindAction("Inventory");
            if (_uiInventoryAction != null)
                _uiInventoryAction.performed += _inventoryCallback;
            _uiMapAction = _uiMap.FindAction("Map");
            if (_uiMapAction != null)
            {
                _uiMapAction.performed += _mapCallback;
            }
            _uiScrollAction = _uiMap.FindAction("ScrollWheel");
        }

        GameEvents.OnCursorShowRequested += OnBlockingUIOpened;
        GameEvents.OnCursorHideRequested += OnBlockingUIClosed;
    }

    private void OnDisable()
    {
        GameEvents.OnCursorShowRequested -= OnBlockingUIOpened;
        GameEvents.OnCursorHideRequested -= OnBlockingUIClosed;

        if (_uiMap != null)
        {
            _uiInventoryAction.performed -= _inventoryCallback;
            _uiMapAction.performed -= _mapCallback;
        }
            

        if (_playerMap != null)
        {
            _playerMap.Disable();
            var interact = _playerMap.FindAction("Interact");
            var attack = _playerMap.FindAction("Attack");
            var inventory = _playerMap.FindAction("Inventory");
            var squadSwap = _playerMap.FindAction("SquadSwap");
            var save = _playerMap.FindAction("Save");
            var map = _playerMap.FindAction("Map");
            var settings = _playerMap.FindAction("Settings");
            if (interact != null && _interactCallback != null) interact.performed -= _interactCallback;
            if (attack != null && _attackCallback != null) attack.performed -= _attackCallback;
            if (inventory != null && _inventoryCallback != null) inventory.performed -= _inventoryCallback;
            if (squadSwap != null && _squadSwapCallback != null) squadSwap.performed -= _squadSwapCallback;
            if (save != null && _saveCallback != null) save.performed -= _saveCallback;
            if (map != null && _mapCallback != null) map.performed -= _mapCallback;
            if (settings  != null && _settingsCallback != null) settings.performed -= _settingsCallback;
        }

        _uiMap?.Disable();
        _asset = null;
        _playerMap = null;
        _uiMap = null;
        _moveAction = null;
        _lookAction = null;
        _uiInventoryAction = null;
        _uiMapAction = null;
    }

    private void OnBlockingUIOpened()
    {
        _blockingUICount++;
        if (_blockingUICount == 1)
            SwitchToUIMode();
    }

    private void OnBlockingUIClosed()
    {
        if (_blockingUICount > 0)
            _blockingUICount--;
        if (_blockingUICount == 0)
            SwitchToPlayMode();
    }

    private void SwitchToUIMode()
    {
        _playerMap?.Disable();
        _uiMap?.Enable();
        MoveInput = Vector2.zero;
        LookInput = Vector2.zero;
    }

    private void SwitchToPlayMode()
    {
        _uiMap?.Disable();
        _playerMap?.Enable();
    }

    private void Update()
    {
        if (_playerMap == null || !_playerMap.enabled)
        {
            MoveInput = Vector2.zero;
            LookInput = Vector2.zero;
        }
        if (_uiMap == null || !_uiMap.enabled)
        {
            ScrollInput = Vector2.zero;
        }

        MoveInput = _moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
        LookInput = _lookAction?.ReadValue<Vector2>() ?? Vector2.zero;
        ScrollInput = _uiScrollAction?.ReadValue<Vector2>() ?? Vector2.zero;
    }
}
