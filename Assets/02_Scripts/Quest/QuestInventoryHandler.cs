using UnityEngine;

/// <summary>
/// 퀘스트 완료 시 Gather 타입이면 인벤토리에서 목표 아이템 차감. IQuestCompletedHandler 구현, QuestCompletedRegistry에 등록.
/// </summary>
public class QuestInventoryHandler : MonoBehaviour, IQuestCompletedHandler
{
    [SerializeField] private Inventory _inventory;

    private void Awake()
    {
        PlaySceneServices.QuestCompleted.Register(this);
    }

    private void OnDestroy()
    {
        PlaySceneServices.QuestCompleted.Unregister(this);
    }

    public void OnQuestCompleted(QuestData data)
    {
        if (data == null || _inventory == null) return;
        if (data.QuestType != QuestType.Gather || string.IsNullOrEmpty(data.TargetId)) return;

        _inventory.RemoveItem(data.TargetId, data.TargetAmount);
    }
}
