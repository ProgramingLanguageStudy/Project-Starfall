using UnityEngine;

/// <summary>
/// 퀘스트 관련 플래그·인벤토리 처리. OnItemPickedUp→NotifyProgress, OnQuestUpdated(목표달성 플래그), OnQuestCompleted(완료 플래그·아이템 차감).
/// 대화→퀘스트 수락/완료는 DialogueController가 QuestPresenter.RequestXxx 직접 호출.
/// PlayScene 컴포넌트. QuestPresenter를 보유.
/// </summary>
public class QuestController : MonoBehaviour
{
    [SerializeField] private QuestPresenter _presenter;

    /// <summary>PlayScene 등에서 DialogueController에 주입용.</summary>
    public QuestPresenter Presenter => _presenter;
    private Inventory _inventory;

    private QuestSystem QuestSystem => _presenter != null ? _presenter.System : null;

    private FlagSystem _flagSystem;

    /// <summary>PlayScene 등에서 주입.</summary>
    public void Initialize(Inventory inventory, FlagSystem flagSystem)
    {
        if (_inventory == null && inventory != null)
            _inventory = inventory;
        _flagSystem = flagSystem;
        _presenter?.Initialize(flagSystem);
    }

    private void OnEnable()
    {
        GameEvents.OnItemPickedUp += HandleItemPickedUp;
        PlaySceneEventHub.OnEnemyKilled += HandleEnemyKilled;
        if (QuestSystem != null)
        {
            QuestSystem.OnQuestUpdated += HandleQuestUpdated;
            QuestSystem.OnQuestCompleted += HandleQuestCompleted;
        }
    }

    private void OnDisable()
    {
        GameEvents.OnItemPickedUp -= HandleItemPickedUp;
        PlaySceneEventHub.OnEnemyKilled -= HandleEnemyKilled;
        if (QuestSystem != null)
        {
            QuestSystem.OnQuestUpdated -= HandleQuestUpdated;
            QuestSystem.OnQuestCompleted -= HandleQuestCompleted;
        }
    }

    private void HandleItemPickedUp(ItemData itemData, int amount)
    {
        if (itemData == null || string.IsNullOrEmpty(itemData.ItemId) || QuestSystem == null) return;

        for (int i = 0; i < amount; i++)
            QuestSystem.NotifyProgress(itemData.ItemId);
    }

    private void HandleEnemyKilled(string enemyId)
    {
        if (string.IsNullOrEmpty(enemyId) || QuestSystem == null) return;
        QuestSystem.NotifyProgress(enemyId);
    }

    private void HandleQuestUpdated(QuestModel quest)
    {
        if (quest == null) return;

        if (quest.QuestType == QuestType.Gather && quest.CurrentAmount == 0 && _inventory != null && !string.IsNullOrEmpty(quest.TargetId))
        {
            var count = _inventory.GetTotalCount(quest.TargetId);
            if (count > 0)
            {
                QuestSystem.SetTaskProgress(quest.QuestId, quest.TargetId, count);
                return;
            }
        }

        if (!quest.IsCompleted) return;

        _flagSystem?.SetFlag(GameStateKeys.QuestObjectivesDone(quest.QuestId), 1);
    }

    private void HandleQuestCompleted(QuestData data)
    {
        if (data == null) return;

        ApplyQuestCompletedFlag(data);
        DeductGatherItems(data);
    }

    private void ApplyQuestCompletedFlag(QuestData data)
    {
        if (string.IsNullOrEmpty(data.QuestId)) return;

        _flagSystem?.SetFlag(GameStateKeys.QuestCompleted(data.QuestId), 1);
    }

    private void DeductGatherItems(QuestData data)
    {
        if (_inventory == null) return;
        if (data.QuestType != QuestType.Gather || string.IsNullOrEmpty(data.TargetId)) return;

        _inventory.RemoveItem(data.TargetId, data.TargetAmount);
    }
}
