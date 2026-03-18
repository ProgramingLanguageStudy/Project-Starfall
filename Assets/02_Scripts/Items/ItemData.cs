using UnityEngine;

/// <summary>아이템 정의 베이스. 타입별 데이터는 MaterialItemData, ConsumableItemData 자식들, EquipmentItemData 사용.</summary>
public abstract class ItemData : BaseData
{
    public override string Id => _id;
    public override string Category => "ItemData";

    [SerializeField] private string _id;
    public string ItemName;
    public Sprite Icon;
    public string Description;
    public bool IsStackable;
    public int MaxStack = 50;

    public abstract ItemType ItemType { get; }
}
