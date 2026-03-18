using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// SO 데이터 로드·캐시. Addressables "Data" 라벨로 일괄 로드.
/// BaseData: 단일 캐시 "Category/Id". DialogueData: npcId별 목록 유지.
/// </summary>
public class DataManager : MonoBehaviour
{
    [Header("라벨")]
    [SerializeField] [Tooltip("모든 SO에 부여한 라벨")]
    private string _dataLabel = "Data";

    /// <summary>"Category/Id" → SO. BaseData 상속 타입만.</summary>
    private Dictionary<string, ScriptableObject> _cache =
        new Dictionary<string, ScriptableObject>(StringComparer.OrdinalIgnoreCase);

    private Dictionary<string, List<DialogueData>> _dialoguesByNpcId =
        new Dictionary<string, List<DialogueData>>(StringComparer.OrdinalIgnoreCase);

    public bool IsLoaded { get; private set; }

    #region Load

    /// <summary>동기 로드. Data 라벨로 일괄 로드 후 타입별 분류.</summary>
    public void Load()
    {
        if (IsLoaded) return;
        LoadAllByLabel();
        IsLoaded = true;
    }

    /// <summary>비동기 로드. 진행률 콜백 지원. 이미 로드됐으면 스킵.</summary>
    public IEnumerator LoadAsync(Action<float, string> onProgress = null)
    {
        if (IsLoaded) yield break;

        onProgress?.Invoke(0f, "DataManager 로드중...");
        var handle = Addressables.LoadAssetsAsync<ScriptableObject>(_dataLabel, null);

        while (!handle.IsDone)
        {
            onProgress?.Invoke(handle.PercentComplete, "DataManager 로드중...");
            yield return null;
        }

        ClearCaches();
        var list = handle.Result;
        if (list != null)
        {
            foreach (var so in list)
            {
                if (so != null)
                    CacheByType(so);
            }
        }

        IsLoaded = true;
        onProgress?.Invoke(1f, "Data 로드 완료");
    }

    private void LoadAllByLabel()
    {
        if (IsLoaded) return;

        var handle = Addressables.LoadAssetsAsync<ScriptableObject>(_dataLabel, null);
        var list = handle.WaitForCompletion();

        ClearCaches();
        if (list != null)
        {
            foreach (var so in list)
            {
                if (so != null)
                    CacheByType(so);
            }
        }

        IsLoaded = true;
    }

    #endregion

    #region Cache (내부)

    private void ClearCaches()
    {
        _cache.Clear();
        _dialoguesByNpcId.Clear();
    }

    private void CacheByType(ScriptableObject so)
    {
        // BaseData: 단일 캐시
        if (so is BaseData bd && !string.IsNullOrEmpty(bd.Id))
            _cache[$"{bd.Category}/{bd.Id}"] = so;

        // DialogueData: npcId별 목록 (1:N)
        if (so is DialogueData dd && !string.IsNullOrEmpty(dd.npcId))
        {
            if (!_dialoguesByNpcId.TryGetValue(dd.npcId, out var list))
            {
                list = new List<DialogueData>();
                _dialoguesByNpcId[dd.npcId] = list;
            }
            list.Add(dd);
        }
    }

    #endregion

    #region Query

    /// <summary>제네릭 조회. Load 완료 후 캐시에서 반환.</summary>
    public T Get<T>(string id) where T : BaseData
    {
        if (string.IsNullOrEmpty(id)) return null;

        var key = $"{typeof(T).Name}/{id}";
        if (typeof(T) == typeof(ItemData) || typeof(T).IsSubclassOf(typeof(ItemData)))
            key = $"ItemData/{id}";

        return _cache.TryGetValue(key, out var cached) ? cached as T : null;
    }

    public IReadOnlyList<DialogueData> GetDialoguesForNpc(string npcId)
    {
        if (string.IsNullOrEmpty(npcId)) return Array.Empty<DialogueData>();
        if (!IsLoaded) LoadAllByLabel();
        return _dialoguesByNpcId.TryGetValue(npcId, out var list) ? list : Array.Empty<DialogueData>();
    }

    #endregion
}
