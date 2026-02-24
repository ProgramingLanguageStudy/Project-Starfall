using System;
using UnityEngine;

[Serializable]
public struct QuestRewardItem
{
    public string itemId;
    public int amount;
}

[CreateAssetMenu(fileName = "NewQuest", menuName = "Quest/QuestData")]
public class QuestData : ScriptableObject
{
    public string QuestId;
    /// <summary>퀘스트 제목 (UI 헤더용).</summary>
    public string Title;
    /// <summary>목표 설명 (UI용, 예: 버섯 3개 수집).</summary>
    [TextArea(1, 3)]
    public string Description;

    public QuestType QuestType;
    /// <summary>목표 ID (아이템/몬스터/장소 등).</summary>
    public string TargetId;
    /// <summary>목표 수치. 방문/도착은 1.</summary>
    public int TargetAmount = 1;

    [Header("완료 보상")]
    public int RewardGold;
    public QuestRewardItem[] RewardItems;
}
