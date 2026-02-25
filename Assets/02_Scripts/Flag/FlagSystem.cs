using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플래그 저장·조회. QuestSystem처럼 PlayScene이 보유하고 주입.
/// </summary>
public class FlagSystem : MonoBehaviour
{
    private Dictionary<string, int> _flags = new Dictionary<string, int>();

    public void SetFlag(string key, int value) => _flags[key] = value;

    /// <summary>현재 값에 value를 더함. 없으면 0으로 간주.</summary>
    public void AddFlag(string key, int value) => SetFlag(key, GetFlag(key) + value);

    public int GetFlag(string key) => _flags.ContainsKey(key) ? _flags[key] : 0;

    /// <summary>세이브용. 현재 플래그 전체를 FlagSaveData로 반환.</summary>
    public FlagSaveData GetAllForSave()
    {
        var result = new FlagSaveData();
        foreach (var kv in _flags)
        {
            result.keys.Add(kv.Key);
            result.values.Add(kv.Value);
        }
        return result;
    }

    /// <summary>로드용. 기존 플래그 초기화 후 저장 데이터로 채움.</summary>
    public void LoadFromSave(FlagSaveData data)
    {
        _flags.Clear();
        if (data?.keys == null || data?.values == null) return;
        var count = Mathf.Min(data.keys.Count, data.values.Count);
        for (var i = 0; i < count; i++)
            _flags[data.keys[i]] = data.values[i];
    }
}
