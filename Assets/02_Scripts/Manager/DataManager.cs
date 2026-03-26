using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// SO 데이터 로드·캐시. Addressables "Data" 라벨로 일괄 로드.
/// BaseData: 단일 캐시 "Category/Id". DialogueData: npcId별 목록 유지.
/// Handle 미보관, Result(SO)만 캐시. Release 없음. SO는 게임 내내 유지(앱 종료 시 OS 정리).
/// </summary>
public class DataManager : MonoBehaviour
{
    /// <summary>"Category/Id" → SO. BaseData 상속 타입만.</summary>
    private Dictionary<string, ScriptableObject> _cache =
        new Dictionary<string, ScriptableObject>(StringComparer.OrdinalIgnoreCase);

    private Dictionary<string, List<DialogueData>> _dialoguesByNpcId =
        new Dictionary<string, List<DialogueData>>(StringComparer.OrdinalIgnoreCase);

    public bool IsLoaded { get; private set; }

    #region Load

    /// <summary>비동기 로드. 진행률 콜백 지원. 이미 로드됐으면 스킵.</summary>
    public IEnumerator LoadAsync(Action<float, string> onProgress = null)
    {
        if (IsLoaded) yield break;

        onProgress?.Invoke(0f, "DataManager 로드중...");
        var handle = Addressables.LoadAssetsAsync<ScriptableObject>(AddressableConfig.DataLabel, null);

        while (!handle.IsDone)
        {
            onProgress?.Invoke(handle.PercentComplete, "DataManager 로드중...");
            yield return null;
        }

        ClearCaches();
        var list = handle.Result;
        if (list != null)
        {
            Debug.Log($"[DataManager] Loaded {list.Count} ScriptableObjects");
            foreach (var so in list)
            {
                if (so != null)
                {
                    Debug.Log($"[DataManager] Loading: {so.GetType().Name} - {so.name}");
                    CacheByType(so);
                }
            }
        }

        IsLoaded = true;
        onProgress?.Invoke(1f, "Data 로드 완료");
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
        {
            // 하위 타입도 부모 타입의 카테고리로 통일 (QuestData, ItemData 등)
            string category = bd.Category;
            if (so is ItemData) category = "ItemData";
            else if (so is QuestData) category = "QuestData";
            
            var key = $"{category}/{bd.Id}";
            Debug.Log($"[DataManager] Caching {so.GetType().Name}: {key} (Category: {category}, Id: {bd.Id})");
            _cache[key] = so;
        }

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

        // T 타입의 Category를 정적으로 알 수 없으므로, 조회용 딕셔너리나 
        // ItemData와 같은 특수 케이스 처리를 유지하되, 좀 더 깔끔하게 관리할 필요가 있음.
        // 현재는 ItemData 계열만 예외 처리.
        var category = typeof(T).Name;
        if (typeof(ItemData).IsAssignableFrom(typeof(T)))
        {
            category = "ItemData";
        }
        // QuestData 계열도 동일하게 처리 (RecruitmentQuestData 등 하위 타입용)
        else if (typeof(QuestData).IsAssignableFrom(typeof(T)))
        {
            category = "QuestData";
        }

        var key = $"{category}/{id}";
        
        // 디버그 로그 추가
        Debug.Log($"[DataManager] Get<{typeof(T).Name}> with id: {id}");
        Debug.Log($"[DataManager] Looking for key: {key}");
        Debug.Log($"[DataManager] Cache contains {_cache.Count} items");
        if (!_cache.ContainsKey(key))
        {
            Debug.LogWarning($"[DataManager] Key not found in cache: {key}");
            // 캐시에 있는 모든 키 출력
            var keys = string.Join(", ", _cache.Keys);
            Debug.Log($"[DataManager] Available keys: {keys}");
        }
        
        return _cache.TryGetValue(key, out var cached) ? cached as T : null;
    }

    public IReadOnlyList<DialogueData> GetDialoguesForNpc(string npcId)
    {
        if (string.IsNullOrEmpty(npcId)) return Array.Empty<DialogueData>();
        if (!IsLoaded) return Array.Empty<DialogueData>();
        return _dialoguesByNpcId.TryGetValue(npcId, out var list) ? list : Array.Empty<DialogueData>();
    }

    #endregion
}
