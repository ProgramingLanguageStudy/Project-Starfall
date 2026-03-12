using UnityEngine;

/// <summary>아이템 획득 시 왼쪽 중간에 아이콘+x수량 알림. 새 항목이 위에 쌓이고, 2~3초 후 밀려나며 사라짐.</summary>
public class ItemPickupLog : MonoBehaviour
{
    [SerializeField] private Transform _container;
    [SerializeField] private ItemPickupSlot _slotPrefab;

    private void OnEnable()
    {
        GameEvents.OnItemPickedUp += OnItemPickedUp;
        GameEvents.OnGoldAcquired += OnGoldAcquired;
    }

    private void OnDisable()
    {
        GameEvents.OnItemPickedUp -= OnItemPickedUp;
        GameEvents.OnGoldAcquired -= OnGoldAcquired;
    }

    private void OnItemPickedUp(ItemData itemData, int amount)
    {
        if (itemData == null || _slotPrefab == null || _container == null) return;

        var slot = Instantiate(_slotPrefab, _container);
        slot.Show(itemData, amount);
        slot.transform.SetAsFirstSibling();
    }

    private void OnGoldAcquired(int amount)
    {
        if (_slotPrefab == null || _container == null) return;

        var slot = Instantiate(_slotPrefab, _container);
        slot.ShowGold(amount);
        slot.transform.SetAsFirstSibling();
    }
}
