using UnityEngine;

/// <summary>아이템의 런타임 모델. ItemObject·인벤토리 등에서 사용. 현재는 ItemData를 감싼 임시 구현.</summary>
public class ItemModel
{
    public ItemData Data { get; }

    public string Id => Data != null ? Data.Id : string.Empty;
    public string ItemName => Data != null ? Data.ItemName : string.Empty;
    public Sprite Icon => Data != null ? Data.Icon : null;
    public string Description => Data != null ? Data.Description : string.Empty;
    public ItemType ItemType => Data != null ? Data.ItemType : ItemType.Material;
    public bool IsStackable => Data != null && Data.IsStackable;
    public int MaxStack => Data != null ? Data.MaxStack : 0;

    public ItemModel(ItemData data) => Data = data;
}
