using System.Collections.Generic;

/// <summary>
/// 인벤토리 슬롯 저장 데이터.
/// </summary>
[System.Serializable]
public class InventorySaveData
{
    public List<InventorySlotEntry> slots = new List<InventorySlotEntry>();
}

[System.Serializable]
public class InventorySlotEntry
{
    public int index;
    public string id;
    public int count;
}
