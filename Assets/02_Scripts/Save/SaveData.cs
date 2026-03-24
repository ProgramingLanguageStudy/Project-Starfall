using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 세이브 파일 루트. 섹션별 데이터를 묶어서 한 번에 JSON 직렬화.
/// </summary>
[System.Serializable]
public class SaveData
{
    public SquadSaveData squad = new SquadSaveData();
    public FlagSaveData flags = new FlagSaveData();
    public QuestSaveData quests = new QuestSaveData();
    public InventorySaveData inventory = new InventorySaveData();
    public List<string> openedChestSaveIds = new List<string>();
    /// <summary>계정 귀속 재화. 골드.</summary>
    public int gold;
}
