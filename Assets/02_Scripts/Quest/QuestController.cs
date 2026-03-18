using UnityEngine;

/// <summary>
/// 퀘스트 이벤트 조율 + 수락/완료 요청 API. OnItemChangedWithId→SetTaskProgress(Gather), OnEnemyKilled→NotifyProgress(Kill).
/// OnQuestUpdated(목표달성 플래그), OnQuestCompleted(완료 플래그·아이템 차감).
/// DialogueController가 RequestAcceptQuest/RequestCompleteQuest 호출.
/// </summary>
public class QuestController : MonoBehaviour
{
    private QuestSystem _questSystem;
    private Inventory _inventory;
    private SquadController _squadController;
    private FlagSystem _flagSystem;

    /// <summary>PlayScene 등에서 주입. SquadController는 영입 퀘스트용.</summary>
    public void Initialize(QuestSystem questSystem, Inventory inventory, FlagSystem flagSystem, SquadController squadController = null)
    {
        _questSystem = questSystem;
        if (_inventory == null && inventory != null)
            _inventory = inventory;
        _flagSystem = flagSystem;
        _squadController = squadController;
    }

    private void OnEnable()
    {
        if (_inventory != null)
            _inventory.OnItemChangedWithId += HandleItemChangedWithId;
        PlaySceneEventHub.OnEnemyKilled += HandleEnemyKilled;
        if (_questSystem != null)
        {
            _questSystem.OnQuestUpdated += HandleQuestUpdated;
            _questSystem.OnQuestCompleted += HandleQuestCompleted;
        }
    }

    private void OnDisable()
    {
        if (_inventory != null)
            _inventory.OnItemChangedWithId -= HandleItemChangedWithId;
        PlaySceneEventHub.OnEnemyKilled -= HandleEnemyKilled;
        if (_questSystem != null)
        {
            _questSystem.OnQuestUpdated -= HandleQuestUpdated;
            _questSystem.OnQuestCompleted -= HandleQuestCompleted;
        }
    }

    private void HandleItemChangedWithId(string itemId, int totalCount)
    {
        if (string.IsNullOrEmpty(itemId) || _questSystem == null) return;

        foreach (var quest in _questSystem.GetActiveQuests())
        {
            if (quest.IsCompleted || quest.QuestType != QuestType.Gather) continue;
            if (quest.TargetId != itemId) continue;

            _questSystem.SetTaskProgress(quest.Id, itemId, totalCount);
        }
    }

    private void HandleEnemyKilled(Enemy enemy)
    {
        if (enemy?.Model?.Data == null || _questSystem == null) return;
        var enemyId = enemy.Model.Data.enemyId;
        if (string.IsNullOrEmpty(enemyId)) return;
        _questSystem.NotifyProgress(enemyId);
    }

    private void HandleQuestUpdated(QuestModel quest)
    {
        if (quest == null) return;

        if (quest.QuestType == QuestType.Gather && quest.CurrentAmount == 0 && _inventory != null && !string.IsNullOrEmpty(quest.TargetId))
        {
            var count = _inventory.GetTotalCount(quest.TargetId);
            if (count > 0)
            {
                _questSystem.SetTaskProgress(quest.Id, quest.TargetId, count);
                return;
            }
        }

        if (!quest.IsCompleted) return;

        _flagSystem?.SetFlag(GameStateKeys.QuestObjectivesDone(quest.Id), 1);
    }

    private void HandleQuestCompleted(QuestData data)
    {
        if (data == null) return;

        ApplyQuestCompletedFlag(data);
        ApplyQuestRewards(data);

        switch (data)
        {
            case RecruitmentQuestData recruitment:
                HandleRecruitmentComplete(recruitment);
                break;
            default:
                DeductGatherItems(data);
                break;
        }
    }

    /// <summary>퀘스트 완료 보상(골드, 아이템) 지급.</summary>
    private void ApplyQuestRewards(QuestData data)
    {
        if (data.RewardGold > 0)
            GameManager.Instance?.CurrencyManager?.AddGold(data.RewardGold);

        if (data.RewardItems == null || data.RewardItems.Length == 0) return;
        var dm = GameManager.Instance?.DataManager;
        if (dm == null || _inventory == null) return;

        foreach (var reward in data.RewardItems)
        {
            if (string.IsNullOrEmpty(reward.id) || reward.amount <= 0) continue;

            var itemData = dm.Get<ItemData>(reward.id);
            if (itemData == null) continue;

            _inventory.AddItem(itemData, reward.amount);
        }
    }

    private void HandleRecruitmentComplete(RecruitmentQuestData data)
    {
        var characterId = data.recruitCharacterId;
        if (string.IsNullOrEmpty(characterId))
        {
            Debug.LogWarning("[QuestController] RecruitmentQuestData에 recruitCharacterId가 비어 있습니다.");
            return;
        }

        var dm = GameManager.Instance?.DataManager;
        if (dm == null) return;

        var characterData = dm.Get<CharacterData>(characterId);
        if (characterData == null)
        {
            Debug.LogWarning($"[QuestController] CharacterData 없음: {characterId}. Resources/Characters 확인.");
            return;
        }

        _squadController.AddCompanion(characterId);
    }

    private void ApplyQuestCompletedFlag(QuestData data)
    {
        if (string.IsNullOrEmpty(data.Id)) return;

        _flagSystem?.SetFlag(GameStateKeys.QuestCompleted(data.Id), 1);
    }

    private void DeductGatherItems(QuestData data)
    {
        if (_inventory == null) return;
        if (data.QuestType != QuestType.Gather || string.IsNullOrEmpty(data.TargetId)) return;

        _inventory.RemoveItem(data.TargetId, data.TargetAmount);
    }

    // ── 요청 API (DialogueController 등에서 호출) ─────────────────────

    /// <summary>퀘스트 수락 요청. DataManager에서 QuestData 로드 후 AcceptQuest·QuestAccepted 플래그.</summary>
    public void RequestAcceptQuest(string questId)
    {
        if (string.IsNullOrEmpty(questId) || _questSystem == null) return;

        var dm = GameManager.Instance?.DataManager;
        var questData = dm?.Get<QuestData>(questId);
        if (questData == null)
        {
            Debug.LogWarning($"[QuestController] QuestData not found: {questId}");
            return;
        }

        _questSystem.AcceptQuest(questData);
        _flagSystem?.SetFlag(GameStateKeys.QuestAccepted(questId), 1);
    }

    /// <summary>퀘스트 완료 요청. 목표 달성된 퀘스트만 CompleteQuest 호출.</summary>
    public void RequestCompleteQuest(string questId)
    {
        if (string.IsNullOrEmpty(questId) || _questSystem == null) return;

        var quest = _questSystem.GetQuestById(questId);
        if (quest == null || !quest.IsCompleted) return;

        _questSystem.CompleteQuest(questId);
    }
}
