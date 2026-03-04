using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Play 씬의 NPC 일괄 관리. Initialize 시 Find로 등록. TryGet으로 조회.
/// </summary>
public class NpcController : MonoBehaviour
{
    private readonly Dictionary<string, Npc> _npcsById = new Dictionary<string, Npc>(System.StringComparer.OrdinalIgnoreCase);

    /// <summary>PlayScene 등에서 호출. 씬 내 모든 Npc 찾아 등록.</summary>
    public void Initialize()
    {
        _npcsById.Clear();
        var npcs = FindObjectsByType<Npc>(FindObjectsSortMode.None);
        foreach (var npc in npcs)
        {
            if (npc == null || string.IsNullOrEmpty(npc.NpcId)) continue;
            _npcsById[npc.NpcId] = npc;
        }
    }

    /// <summary>npcId에 해당하는 Npc. 없으면 null.</summary>
    public Npc TryGet(string npcId)
    {
        if (string.IsNullOrEmpty(npcId)) return null;
        return _npcsById.TryGetValue(npcId, out var npc) ? npc : null;
    }
}
