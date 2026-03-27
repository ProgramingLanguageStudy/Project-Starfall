using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 인벤토리 상태·로직. 런타임 데이터(슬롯 배열)를 관리합니다.
/// OnSlotChanged: UI 갱신용(변경된 슬롯 하나). OnItemChangedWithId: 퀘스트 등용(itemId, 총 수량).
/// </summary>
public class Inventory : MonoBehaviour
{
    [SerializeField] private int _inventorySize = 30;
    private IItemUser _itemUser;
    private ItemSlotModel[] _slots;
    
    [Header("재화")]
    [SerializeField] private int _gold;

    /// <summary>아이템 사용 시 효과를 받을 대상. Presenter 등에서 주입.</summary>
    public void SetItemUser(IItemUser itemUser) => _itemUser = itemUser;

    /// <summary>슬롯 하나 변경 시. UI는 해당 슬롯만 갱신하면 됨.</summary>
    public event Action<ItemSlotModel> OnSlotChanged;
    /// <summary>아이템별 총 수량 변경 시 (itemId, 새 총 수량). 퀘스트 등에서 사용.</summary>
    public event Action<string, int> OnItemChangedWithId;
    
    /// <summary>골드 변경 시 (변경 후 값). UI 갱신용.</summary>
    public event Action<int> OnGoldChanged;
    
    /// <summary>현재 골드. UI 표시용.</summary>
    public int Gold => _gold;

    // ── 초기화 ────────────────────────────────────────────────

    /// <summary>슬롯 배열 생성. Presenter가 호출.</summary>
    public void Initialize()
    {
        _slots = new ItemSlotModel[_inventorySize];
        for (int i = 0; i < _inventorySize; i++)
            _slots[i] = new ItemSlotModel(null, 0, i);
    }

    /// <summary>저장 데이터로 슬롯 복원. 슬롯 순서 유지. Initialize 후 호출.</summary>
    public void LoadFromSave(InventorySaveData saveData)
    {
        if (saveData?.slots == null) return;

        foreach (ItemSlotModel slot in _slots)
            slot.Clear();

        foreach (InventorySlotEntry entry in saveData.slots)
        {
            if (entry.index < 0 || entry.index >= _inventorySize) continue;
            if (string.IsNullOrEmpty(entry.id) || entry.count <= 0) continue;

            ItemData itemData = GameManager.Instance?.DataManager?.Get<ItemData>(entry.id);
            if (itemData == null)
            {
                Debug.LogWarning($"[Inventory] LoadFromSave: ItemData not found for '{entry.id}'. Add to Resources/Items.");
                continue;
            }

            _slots[entry.index].Item = new ItemModel(itemData);
            _slots[entry.index].Count = Mathf.Min(entry.count, itemData.MaxStack);
            OnSlotChanged?.Invoke(_slots[entry.index]);
        }

        HashSet<string> notifiedIds = new HashSet<string>();
        foreach (InventorySlotEntry entry in saveData.slots)
        {
            if (!string.IsNullOrEmpty(entry.id) && notifiedIds.Add(entry.id))
                NotifyItemChangedWithId(entry.id);
        }
    }

    // ── Public API ─────────────────────────────────────────────

    public ItemSlotModel[] GetSlots() => _slots;

    public int GetTotalCount(string itemId)
    {
        int total = 0;
        foreach (ItemSlotModel slot in _slots)
        {
            if (slot.Item != null && slot.Item.Id == itemId)
                total += slot.Count;
        }
        return total;
    }

    public void AddItem(ItemData itemData, int amount = 1)
    {
        if (itemData == null) return;
        if (itemData.IsStackable)
        {
            foreach (ItemSlotModel slot in _slots)
            {
                if (slot.Item != null && slot.Item.Id == itemData.Id && slot.Count < itemData.MaxStack)
                {
                    int canAdd = itemData.MaxStack - slot.Count;
                    int amountToAdd = Mathf.Min(amount, canAdd);
                    slot.Count += amountToAdd;
                    amount -= amountToAdd;
                    OnSlotChanged?.Invoke(slot);
                    if (amount <= 0)
                    {
                        NotifyItemChangedWithId(itemData.Id);
                        return;
                    }
                }
            }
        }

        while (amount > 0)
        {
            int emptySlotIndex = FindEmptySlotIndex();
            if (emptySlotIndex == -1)
            {
                Debug.LogWarning("인벤토리가 가득 차서 남은 아이템을 버리거나 무시합니다: " + amount);
                break;
            }
            int amountToPut = Mathf.Min(amount, itemData.MaxStack);
            _slots[emptySlotIndex].Item = new ItemModel(itemData);
            _slots[emptySlotIndex].Count = amountToPut;
            amount -= amountToPut;
            OnSlotChanged?.Invoke(_slots[emptySlotIndex]);
        }
        NotifyItemChangedWithId(itemData.Id);
    }

    public bool RemoveItem(string itemId, int amount)
    {
        if (GetTotalCount(itemId) < amount) return false;

        int remaining = amount;
        foreach (ItemSlotModel slot in _slots)
        {
            if (remaining <= 0) break;
            if (slot.Item == null || slot.Item.Id != itemId) continue;
            int toRemove = Mathf.Min(slot.Count, remaining);
            slot.Count -= toRemove;
            remaining -= toRemove;
            if (slot.Count <= 0) slot.Clear();
            OnSlotChanged?.Invoke(slot);
        }
        NotifyItemChangedWithId(itemId);
        return true;
    }

    /// <summary>해당 슬롯의 아이템 사용 시도. 소모품이면 ApplyTo 적용 후 1개 차감. 성공 시 true.</summary>
    public bool TryUseItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _inventorySize) return false;
        var slot = _slots[slotIndex];
        if (slot.Item == null || slot.Count <= 0) return false;
        if (slot.Item.ItemType != ItemType.Consumable) return false;
        if (_itemUser == null) return false;

        if (slot.Item.Data is ConsumableItemData consumable)
            consumable.ApplyTo(_itemUser);
        RemoveItem(slot.Item.Id, 1);
        return true;
    }

    public void SwapItems(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= _inventorySize || indexB < 0 || indexB >= _inventorySize) return;

        ItemSlotModel slotA = _slots[indexA];
        ItemSlotModel slotB = _slots[indexB];

        if (slotA.Item != null && slotB.Item != null && slotA.Item.Id == slotB.Item.Id && slotA.Item.IsStackable)
        {
            int maxStack = slotB.Item.MaxStack;
            int canAdd = maxStack - slotB.Count;
            if (canAdd > 0)
            {
                int amountToAdd = Mathf.Min(slotA.Count, canAdd);
                slotB.Count += amountToAdd;
                slotA.Count -= amountToAdd;
                if (slotA.Count <= 0) slotA.Clear();
            }
            else
                PerformSwap(slotA, slotB);
        }
        else
            PerformSwap(slotA, slotB);

        OnSlotChanged?.Invoke(_slots[indexA]);
        OnSlotChanged?.Invoke(_slots[indexB]);
        if (_slots[indexA].Item != null)
            NotifyItemChangedWithId(_slots[indexA].Item.Id);
        if (_slots[indexB].Item != null && _slots[indexB].Item != _slots[indexA].Item)
            NotifyItemChangedWithId(_slots[indexB].Item.Id);
    }

    // ── Unity ──────────────────────────────────────────────────

    private void OnEnable()
    {
        GameEvents.OnItemPickedUp += HandleItemPickedUp;
    }

    private void OnDisable()
    {
        GameEvents.OnItemPickedUp -= HandleItemPickedUp;
    }

    // ── Private ────────────────────────────────────────────────

    private void HandleItemPickedUp(ItemData itemData, int amount)
    {
        AddItem(itemData, amount);
    }

    private int FindEmptySlotIndex()
    {
        for (int i = 0; i < _slots.Length; i++)
            if (_slots[i].Item == null) return i;
        return -1;
    }

    private void PerformSwap(ItemSlotModel a, ItemSlotModel b)
    {
        var tempItem = a.Item;
        var tempCount = a.Count;
        a.Item = b.Item;
        a.Count = b.Count;
        b.Item = tempItem;
        b.Count = tempCount;
    }

    private void NotifyItemChangedWithId(string itemId)
    {
        OnItemChangedWithId?.Invoke(itemId, GetTotalCount(itemId));
    }
    
    // ── 재화 관리 ────────────────────────────────────────────────
    
    /// <summary>로드 시 골드 설정. InventorySaveContributor에서 사용.</summary>
    public void SetGold(int gold)
    {
        _gold = Mathf.Max(0, gold);
        OnGoldChanged?.Invoke(_gold);
    }

    /// <summary>골드 추가. amount는 0 이상.</summary>
    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        _gold = Mathf.Max(0, _gold + amount);
        OnGoldChanged?.Invoke(_gold);
    }

    /// <summary>골드 차감 시도. 보유량 이상이면 실패. 성공 시 true.</summary>
    public bool TrySpendGold(int amount)
    {
        if (amount <= 0) return true;
        if (_gold < amount) return false;
        _gold -= amount;
        OnGoldChanged?.Invoke(_gold);
        return true;
    }
}
