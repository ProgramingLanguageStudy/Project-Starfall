using UnityEngine;

/// <summary>
/// 플래그 세이브/로드 기여. PlaySaveCoordinator.Initialize에서 주입.
/// </summary>
public class FlagSaveContributor : SaveContributor
{
    public override int SaveOrder => 1;

    private FlagSystem _flagSystem;

    public void Initialize(FlagSystem flagSystem)
    {
        _flagSystem = flagSystem;
    }

    public override void Gather(SaveData data)
    {
        if (data?.flags == null) return;
        if (_flagSystem == null) return;
        var fm = _flagSystem;
        if (fm == null) return;

        data.flags.keys.Clear();
        data.flags.values.Clear();
        var saved = fm.GetAllForSave();
        for (int i = 0; i < saved.keys.Count; i++)
        {
            var key = saved.keys[i];
            if (IsQuestProgressFlag(key)) continue;
            data.flags.keys.Add(key);
            data.flags.values.Add(saved.values[i]);
        }
    }

    /// <summary>QuestAccepted, QuestObjectivesDone 제외. QuestSaveData에서 유도.</summary>
    private static bool IsQuestProgressFlag(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        return key.EndsWith("_accepted") && key.StartsWith("quest_")
            || key.EndsWith("_objectives_done") && key.StartsWith("quest_");
    }

    public override void Apply(SaveData data)
    {
        if (data?.flags == null) return;
        if (_flagSystem == null) return;
        var fm = _flagSystem;
        if (fm == null) return;

        fm.LoadFromSave(data.flags);
    }
}
