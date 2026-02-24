using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 대화 선택만 담당. 후보 중 플래그 조건 만족하는 첫 대화 반환. Controller가 보유.
/// </summary>
public class DialogueSelector : MonoBehaviour
{
    /// <summary>npcId로 DataManager·FlagManager 사용해 선택.</summary>
    public DialogueData Select(string npcId)
    {
        var gm = GameManager.Instance;
        var dm = gm?.DataManager;
        var fm = gm?.FlagManager;

        if (dm == null || !dm.IsLoaded || fm == null || string.IsNullOrEmpty(npcId))
            return null;

        var candidates = dm.GetDialoguesForNpc(npcId);
        return SelectFirstMatch(candidates, key => fm.GetFlag(key));
    }

    private DialogueData SelectFirstMatch(IEnumerable<DialogueData> candidates, Func<string, int> getFlag)
    {
        if (candidates == null || getFlag == null) return null;
        var sorted = candidates.Where(d => d != null).OrderBy(d => d.priority).ToList();
        foreach (var d in sorted)
            if (d.MatchesFlags(getFlag)) return d;
        return null;
    }
}
