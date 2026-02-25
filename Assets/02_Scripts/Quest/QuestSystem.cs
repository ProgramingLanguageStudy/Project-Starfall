using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 퀘스트 시스템. 다른 시스템을 알지 않음. 조율층이 NotifyProgress(targetId) 호출 시 해당 targetId 퀘스트의 current를 1 올림.
/// </summary>
public class QuestSystem : MonoBehaviour
{
    private readonly List<QuestModel> _activeQuests = new List<QuestModel>();

    public event Action<QuestModel> OnQuestUpdated;
    /// <summary>퀘스트 완료 시 발행. QuestController가 구독 후 QuestCompleted.InvokeAll.</summary>
    public event Action<QuestData> OnQuestCompleted;

    /// <summary>targetId에 해당하는 진행이 일어났을 때 호출. 해당 퀘스트 current를 1 올림 (방문/채집/처치 공통).</summary>
    public void NotifyProgress(string targetId)
    {
        foreach (var quest in _activeQuests)
        {
            if (quest.IsCompleted || quest.TargetId != targetId) continue;
            quest.CurrentAmount = Math.Min(quest.CurrentAmount + 1, quest.TargetAmount);
            if (quest.IsCompleted)
                Debug.Log($"<color=green>{quest.Title}</color> 목표 달성! NPC에게 돌아가세요.");
            OnQuestUpdated?.Invoke(quest);
        }
    }

    /// <summary>수락 시 등, 현재 진행도를 한 번에 설정할 때 (예: 인벤토리 개수 동기화).</summary>
    public void SetTaskProgress(string questId, string targetId, int amount)
    {
        var quest = _activeQuests.FirstOrDefault(q => q.QuestId == questId && q.TargetId == targetId);
        if (quest == null) return;
        quest.CurrentAmount = Math.Min(amount, quest.TargetAmount);
        OnQuestUpdated?.Invoke(quest);
    }

    public void AcceptQuest(QuestData questData)
    {
        var quest = new QuestModel(questData);
        _activeQuests.Add(quest);
        OnQuestUpdated?.Invoke(quest);
    }

    /// <summary>퀘스트 완료: OnQuestCompleted 발행 후 목록에서 제거. 아이템 차감·플래그는 조율에서 처리 후 호출.</summary>
    public bool CompleteQuest(string questId)
    {
        var quest = _activeQuests.FirstOrDefault(q => q.QuestId == questId);
        if (quest == null) return false;
        var data = quest.Data;
        _activeQuests.Remove(quest);
        OnQuestCompleted?.Invoke(data);
        OnQuestUpdated?.Invoke(quest);
        return true;
    }

    public IReadOnlyList<QuestModel> GetActiveQuests() => _activeQuests;

    /// <summary>진행 중인 퀘스트 중 questId와 일치하는 것 반환. 없으면 null.</summary>
    public QuestModel GetQuestById(string questId)
    {
        return _activeQuests.FirstOrDefault(q => q.QuestId == questId);
    }

    /// <summary>해당 퀘스트가 현재 수락되어 진행 중인지.</summary>
    public bool HasQuest(string questId) => GetQuestById(questId) != null;

    /// <summary>디버그용. 퀘스트를 목록에서 제거. OnQuestCompleted 발행 안 함. 플래그 동기화는 호출부에서.</summary>
    public bool RemoveQuest(string questId)
    {
        var quest = _activeQuests.FirstOrDefault(q => q.QuestId == questId);
        if (quest == null) return false;
        _activeQuests.Remove(quest);
        OnQuestUpdated?.Invoke(quest);
        return true;
    }

    /// <summary>디버그용. 진행 중 퀘스트 전체 제거.</summary>
    public void ClearAllQuests()
    {
        var copy = new List<QuestModel>(_activeQuests);
        _activeQuests.Clear();
        foreach (var q in copy)
            OnQuestUpdated?.Invoke(q);
    }
}
