using UnityEngine;

/// <summary>
/// Inventory(Model)와 InventoryView(View)를 연결. Model·View는 인스펙터에서 할당.
/// PlayScene이 SetPlayerCharacter로 주입하고, 플레이어 변경 시 SetItemUser 갱신.
/// </summary>
public class InventoryPresenter : MonoBehaviour
{
    [SerializeField] private Inventory _model;
    [SerializeField] private InventoryView _view;

    private Character _playerCharacter;
    private float _lastToggleTime = float.MinValue;
    private const float ToggleCooldown = 0.15f;

    /// <summary>QuestController 등에서 Gather 퀘스트 완료 시 아이템 차감용.</summary>
    public Inventory Model => _model;

    /// <summary>인벤토리 토글. PlayScene 입력에서 Request. 연속 호출 방지용 쿨다운 적용.</summary>
    public void RequestToggleInventory()
    {
        if (Time.unscaledTime - _lastToggleTime < ToggleCooldown) return;
        _lastToggleTime = Time.unscaledTime;
        _view?.ToggleInventory();
    }

    /// <summary>PlayScene에서 주입. 플레이어 변경 시 호출.</summary>
    public void SetPlayerCharacter(Character character)
    {
        _playerCharacter = character;
        RefreshItemUser();
    }

    /// <summary>소모품 효과 적용 대상을 현재 플레이어로 갱신.</summary>
    private void RefreshItemUser()
    {
        if (_model != null)
            _model.SetItemUser(_playerCharacter?.Model);
    }

    private void Awake()
    {
        if (_model == null)
            Debug.LogWarning($"[InventoryPresenter] {gameObject.name}: Model(Inventory)이 할당되지 않았습니다. 인스펙터에서 할당해 주세요.");
        if (_view == null)
            Debug.LogWarning($"[InventoryPresenter] {gameObject.name}: View(InventoryView)가 할당되지 않았습니다. 인스펙터에서 할당해 주세요.");
        _model?.Initialize();
        _view?.Initialize();
    }

    private void OnEnable()
    {
        if (_model != null)
            _model.OnSlotChanged += OnSlotChanged;
        if (_view != null)
        {
            _view.OnDropEnded += HandleDropEnded;
            _view.OnUseItemRequested += HandleUseItemRequested;
            _view.OnRefreshRequested += RefreshView;
        }
        RefreshView();
    }

    private void OnDisable()
    {
        if (_model != null)
            _model.OnSlotChanged -= OnSlotChanged;
        if (_view != null)
        {
            _view.OnDropEnded -= HandleDropEnded;
            _view.OnUseItemRequested -= HandleUseItemRequested;
            _view.OnRefreshRequested -= RefreshView;
        }
    }

    private void HandleUseItemRequested(int slotIndex)
    {
        if (_model != null)
            _model.TryUseItem(slotIndex);
    }

    private void OnSlotChanged(ItemSlotModel slot)
    {
        if (_view != null)
            _view.RefreshSlot(slot);
    }

    private void RefreshView()
    {
        if (_view == null || _model == null) return;
        var slots = _model.GetSlots();
        for (int i = 0; i < slots.Length; i++)
            _view.RefreshSlot(slots[i]);
    }

    private void HandleDropEnded(int fromIndex, Vector2 screenPosition)
    {
        if (_view == null || _model == null) return;
        int toIndex = _view.GetSlotIndexAtPosition(screenPosition);
        if (toIndex >= 0 && toIndex != fromIndex)
            _model.SwapItems(fromIndex, toIndex);
    }
}
