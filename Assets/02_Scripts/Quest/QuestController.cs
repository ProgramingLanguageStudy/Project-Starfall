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
    /// <remarks>OnEnable은 InitAfterLoadRoutine보다 먼저 실행되므로, Initialize에서 구독을 수행.</remarks>
    public void Initialize(QuestSystem questSystem, Inventory inventory, FlagSystem flagSystem, SquadController squadController = null)
    {
        _questSystem = questSystem;
        _inventory = inventory;
        _flagSystem = flagSystem;
        _squadController = squadController;

        Subscribe();
    }

    private void Subscribe()
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

    private void Unsubscribe()
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

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void HandleItemChangedWithId(string itemId, int totalCount)
    {
        if (string.IsNullOrEmpty(itemId)) return;

        Debug.Log($"[QuestController] Item changed: {itemId}, count: {totalCount}");

        foreach (QuestModel quest in _questSystem.GetActiveQuests())
        {
            Debug.Log($"[QuestController] Checking quest: {quest.Id}, type: {quest.QuestType}, target: {quest.TargetId}");
            if (quest.IsCompleted) continue;
            if (quest.TargetId != itemId) continue;

            // Gather 퀘스트: 아이템 수집 진행도 업데이트
            if (quest.QuestType == QuestType.Gather)
            {
                Debug.Log($"[QuestController] Updating Gather quest progress: {quest.Id} -> {totalCount}");
                _questSystem.SetTaskProgress(quest.Id, itemId, totalCount);
            }
            // Recruitment 퀘스트: 아이템 수집 조건 체크 (완료 조건 충족 시 완료 처리)
            else if (quest.QuestType == QuestType.Recruitment)
            {
                if (totalCount >= quest.TargetAmount)
                {
                    Debug.Log($"[QuestController] Recruitment quest item condition met: {quest.Id}");
                    // Recruitment 퀘스트는 아이템 조건 충족 시 자동으로 완료 가능 상태로 변경
                    // 실제 완료는 대화에서 처리하거나, 여기서 자동 완료
                    _questSystem.SetTaskProgress(quest.Id, itemId, totalCount);
                }
            }
        }
    }

    private void HandleEnemyKilled(Enemy enemy)
    {
        if (enemy?.Model?.Data == null) return;
        string enemyId = enemy.Model.Data.enemyId;
        if (string.IsNullOrEmpty(enemyId)) return;
        _questSystem.NotifyProgress(enemyId);
    }

    private void HandleQuestUpdated(QuestModel quest)
    {
        if (quest.QuestType == QuestType.Gather && quest.CurrentAmount == 0 && !string.IsNullOrEmpty(quest.TargetId))
        {
            int count = _inventory.GetTotalCount(quest.TargetId);
            if (count > 0)
            {
                _questSystem.SetTaskProgress(quest.Id, quest.TargetId, count);
                return; // SetTaskProgress 후 바로 return하여 중복 처리 방지
            }
        }

        if (!quest.IsCompleted) return;

        _flagSystem?.SetFlag(GameStateKeys.QuestObjectivesDone(quest.Id), 1);
    }

    private void HandleQuestCompleted(QuestData data)
    {
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

    /// <summary>퀘스트 완료 보상(골드, 아이템) 지급. PickupLogView 연동을 위해 GameEvents 발행.</summary>
    private void ApplyQuestRewards(QuestData data)
    {
        if (data.RewardGold > 0)
        {
            _inventory?.AddGold(data.RewardGold);
            GameEvents.OnGoldAcquired?.Invoke(data.RewardGold);
        }

        if (data.RewardItems == null || data.RewardItems.Length == 0) return;
        var dm = GameManager.Instance?.DataManager;
        if (dm == null || _inventory == null) return;

        foreach (QuestRewardItem reward in data.RewardItems)
        {
            if (string.IsNullOrEmpty(reward.id) || reward.amount <= 0) continue;

            ItemData itemData = dm.Get<ItemData>(reward.id);
            if (itemData == null) continue;

            _inventory.AddItem(itemData, reward.amount);
            GameEvents.OnItemPickedUp?.Invoke(itemData, reward.amount);
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

        DataManager dm = GameManager.Instance?.DataManager;
        if (dm == null) return;

        CharacterData characterData = dm.Get<CharacterData>(characterId);
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
        if (string.IsNullOrEmpty(questId)) return;

        DataManager dm = GameManager.Instance?.DataManager;
        QuestData questData = dm?.Get<QuestData>(questId);
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
        if (string.IsNullOrEmpty(questId)) return;

        QuestModel quest = _questSystem.GetQuestById(questId);
        if (quest == null || !quest.IsCompleted) return;

        _questSystem.CompleteQuest(questId);
    }
}
