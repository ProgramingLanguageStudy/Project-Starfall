using UnityEngine;

/// <summary>
/// 인벤토리 세이브/로드. PlaySaveCoordinator.Initialize에서 주입. Resources/Items에 ItemData 에셋 필요.
/// </summary>
public class InventorySaveContributor : SaveContributor
{
    public override int SaveOrder => 2;

    private Inventory _inventory;

    public void Initialize(Inventory inventory)
    {
        _inventory = inventory;
    }

    public override void Gather(SaveData data)
    {
        if (data?.inventory == null) return;
        if (_inventory == null) return;

        data.inventory.slots.Clear();
        ItemSlotModel[] slots = _inventory.GetSlots();
        if (slots == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            ItemSlotModel slot = slots[i];
            if (slot?.Item == null || slot.Count <= 0) continue;

            data.inventory.slots.Add(new InventorySlotEntry
            {
                index = i,
                id = slot.Item.Id,
                count = slot.Count
            });
        }
        
        // 골드 저장
        data.inventory.gold = _inventory.Gold;
    }

    public override void Apply(SaveData data)
    {
        if (data?.inventory == null) return;
        if (_inventory == null) return;

        _inventory.LoadFromSave(data.inventory);
        
        // 골드 로드
        _inventory.SetGold(data.inventory.gold);
    }
}
