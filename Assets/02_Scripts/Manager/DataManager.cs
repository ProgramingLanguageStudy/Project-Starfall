using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// SO 데이터 로드·캐시. Addressables "Data" 라벨로 일괄 로드 후 타입별 분류.
/// </summary>
public class DataManager : MonoBehaviour
{
    [Header("경로")]
    [SerializeField] [Tooltip("SO 경로 앞부분. 주소 = prefix/category/name")]
    private string _soPathPrefix = "Assets/02_Scripts/0_ScriptableObjects";

    [Header("라벨")]
    [SerializeField] [Tooltip("모든 SO에 부여한 라벨")]
    private string _dataLabel = "Data";

    private Dictionary<string, List<DialogueData>> _dialoguesByNpcId = new Dictionary<string, List<DialogueData>>(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, ItemData> _itemsById = new Dictionary<string, ItemData>(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, CharacterData> _charactersById = new Dictionary<string, CharacterData>(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, QuestData> _questsById = new Dictionary<string, QuestData>(StringComparer.OrdinalIgnoreCase);

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
        _dialoguesByNpcId.Clear();
        _itemsById.Clear();
        _charactersById.Clear();
        _questsById.Clear();
    }

    private void CacheByType(ScriptableObject so)
    {
        if (so is CharacterData cd)
        {
            if (!string.IsNullOrEmpty(cd.characterId))
                _charactersById[cd.characterId] = cd;
            return;
        }

        if (so is ItemData id)
        {
            if (!string.IsNullOrEmpty(id.ItemId))
                _itemsById[id.ItemId] = id;
            return;
        }

        if (so is QuestData qd)
        {
            if (!string.IsNullOrEmpty(qd.QuestId))
                _questsById[qd.QuestId] = qd;
            return;
        }

        if (so is DialogueData dd)
        {
            if (string.IsNullOrEmpty(dd.npcId)) return;
            if (!_dialoguesByNpcId.TryGetValue(dd.npcId, out var list))
            {
                list = new List<DialogueData>();
                _dialoguesByNpcId[dd.npcId] = list;
            }
            list.Add(dd);
        }
    }

    private string BuildAddress(string category, string name)
    {
        var prefix = string.IsNullOrEmpty(_soPathPrefix) ? "Assets/02_Scripts/0_ScriptableObjects" : _soPathPrefix.TrimEnd('/');
        return $"{prefix}/{category}/{name}";
    }

    private T LoadAndCache<T>(string category, string name, Dictionary<string, T> cache, Func<T, string> getCacheKey) where T : ScriptableObject
    {
        if (string.IsNullOrEmpty(name) || cache == null) return null;

        try
        {
            var address = BuildAddress(category, name);
            var handle = Addressables.LoadAssetAsync<T>(address);
            var asset = handle.WaitForCompletion();
            if (asset == null) return null;

            var cacheKey = getCacheKey != null ? getCacheKey(asset) : name;
            if (!string.IsNullOrEmpty(cacheKey))
                cache[cacheKey] = asset;
            return asset;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DataManager] LoadAndCache failed ({category}/{name}): {e.Message}");
            return null;
        }
    }

    #endregion

    #region Query

    public CharacterData GetCharacterData(string characterId)
    {
        if (string.IsNullOrEmpty(characterId)) return null;
        if (_charactersById.TryGetValue(characterId, out var cached)) return cached;
        var name = characterId.EndsWith("Data", StringComparison.OrdinalIgnoreCase) ? characterId : characterId + "Data";
        return LoadAndCache("Character", name, _charactersById, a => a.characterId);
    }

    public ItemData GetItemData(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;
        if (_itemsById.TryGetValue(itemId, out var cached)) return cached;
        return LoadAndCache("Items", itemId, _itemsById, a => a.ItemId);
    }

    public QuestData GetQuestData(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return null;
        if (_questsById.TryGetValue(questId, out var cached)) return cached;
        return LoadAndCache("Quests", questId, _questsById, a => a.QuestId);
    }

    public IReadOnlyList<DialogueData> GetDialoguesForNpc(string npcId)
    {
        if (string.IsNullOrEmpty(npcId)) return Array.Empty<DialogueData>();
        if (!IsLoaded) LoadAllByLabel();
        return _dialoguesByNpcId.TryGetValue(npcId, out var list) ? list : Array.Empty<DialogueData>();
    }

    #endregion
}
