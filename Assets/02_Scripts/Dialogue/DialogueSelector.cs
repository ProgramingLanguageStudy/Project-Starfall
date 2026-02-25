using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 대화 선택. SelectMain=재생할 대화 1개, GetAvailableQuests=버튼용 퀘스트 목록.
/// PlayScene에서 Initialize(FlagSystem) 주입.
/// </summary>
public class DialogueSelector : MonoBehaviour
{
    private FlagSystem _flagSystem;

    public void Initialize(FlagSystem flagSystem)
    {
        _flagSystem = flagSystem;
    }

    /// <summary>재생할 대화 1개 선택. 없으면 null.</summary>
    public DialogueData SelectMain(string npcId)
    {
        var (main, _) = SelectInternal(npcId);
        return main;
    }

    /// <summary>버튼 생성용 퀘스트 대화 목록. Casual 모드일 때만 사용.</summary>
    public IReadOnlyList<DialogueData> GetAvailableQuests(string npcId)
    {
        var (_, questList) = SelectInternal(npcId);
        return questList ?? Array.Empty<DialogueData>();
    }

    private (DialogueData main, IReadOnlyList<DialogueData> questList) SelectInternal(string npcId)
    {
        var dm = GameManager.Instance?.DataManager;
        var fm = _flagSystem;

        if (dm == null || !dm.IsLoaded || fm == null || string.IsNullOrEmpty(npcId))
            return (null, null);

        var candidates = dm.GetDialoguesForNpc(npcId);
        if (candidates == null || candidates.Count == 0) return (null, null);

        Func<string, int> getFlag = key => fm.GetFlag(key);
        var matching = candidates.Where(d => d != null && d.MatchesFlags(getFlag)).OrderBy(d => d.priority).ToList();

        var firstTalk = matching.FirstOrDefault(d => d.category == DialogueCategory.FirstTalk);
        if (firstTalk != null) return (firstTalk, null);

        var casual = matching.FirstOrDefault(d => d.category == DialogueCategory.Casual);
        var questList = matching.Where(d => d.category == DialogueCategory.Quest).ToList();

        if (casual != null || questList.Count > 0)
            return (casual ?? questList[0], questList.Count > 0 ? questList : null);

        return (null, null);
    }
}
