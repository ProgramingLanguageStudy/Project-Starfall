using System.Collections.Generic;

/// <summary>
/// 진행 중인 퀘스트 저장 데이터. 완료된 퀘스트는 QuestCompleted 플래그로만 저장.
/// </summary>
[System.Serializable]
public class QuestSaveData
{
    public List<QuestProgressEntry> entries = new List<QuestProgressEntry>();
}

[System.Serializable]
public class QuestProgressEntry
{
    public string questId;
    public string targetId;
    public int currentAmount;
}
