using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

/// <summary>
/// 프리팹 로드 담당. Addressables 사용.
/// 경로(prefix 제외)를 키로, Handle을 값으로 캐시. Release: 씬 전환·게임 종료 시 Release() 호출 권장.
/// </summary>
public class ResourceManager : MonoBehaviour
{
    [Header("경로")]
    [SerializeField] [Tooltip("프리팹 경로 앞부분. 주소 = prefix/category/name.prefab")]
    private string _prefabPathPrefix = "Assets/00_Prefabs";

    [Header("라벨")]
    [SerializeField] [Tooltip("기본 로드용 Addressables 라벨")]
    private string _defaultLoadLabel = "Prefab";

    /// <summary>경로(prefix 제외, 확장자 제외) → Handle. 예: "UI/CharacterProfile"</summary>
    private readonly Dictionary<string, AsyncOperationHandle<GameObject>> _cache =
        new Dictionary<string, AsyncOperationHandle<GameObject>>(StringComparer.OrdinalIgnoreCase);

    private readonly HashSet<string> _loadedLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    #region Load

    /// <summary>해당 라벨 로드 완료 여부. Boot 백그라운드 로드 후 Intro→Play 시 스킵에 사용.</summary>
    public bool IsLoaded(string label) =>
        !string.IsNullOrEmpty(label) && _loadedLabels.Contains(label);

    /// <summary>기본 라벨(_defaultLoadLabel) 로드 완료 여부.</summary>
    public bool IsLoaded() => IsLoaded(_defaultLoadLabel);

    /// <summary>기본 라벨(_defaultLoadLabel)로 프리팹 로드(비동기).</summary>
    public IEnumerator LoadAsync(Action<float, string> onProgress = null) =>
        LoadAsync(_defaultLoadLabel, onProgress);

    /// <summary>라벨로 프리팹 로드(비동기). 진행률 콜백 지원. 이미 로드된 라벨이면 스킵.</summary>
    public IEnumerator LoadAsync(string label, Action<float, string> onProgress = null)
    {
        if (string.IsNullOrEmpty(label)) yield break;
        if (_loadedLabels.Contains(label)) yield break;

        onProgress?.Invoke(0f, "ResourceManager 위치 조회중...");
        // 라벨로 위치 목록 조회 (에셋 본체 아직 로드 안 함)
        var locHandle = Addressables.LoadResourceLocationsAsync(label, typeof(GameObject));
        yield return locHandle;

        var locations = locHandle.Result;
        if (locations == null || locations.Count == 0)
        {
            Addressables.Release(locHandle);
            yield break;
        }

        var prefix = GetPrefix();
        var toLoad = new List<IResourceLocation>();
        // prefix 경로에 해당하는 location만 필터
        foreach (var loc in locations)
        {
            var address = loc.PrimaryKey as string;
            if (string.IsNullOrEmpty(address) || !address.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
            toLoad.Add(loc);
        }
        // locHandle은 위치 목록용. 프리팹 Handle과 별개. 사용 끝났으므로 해제
        Addressables.Release(locHandle);

        if (toLoad.Count == 0) yield break;

        for (int i = 0; i < toLoad.Count; i++)
        {
            var loc = toLoad[i];
            var pct = (float)(i + 1) / toLoad.Count;
            onProgress?.Invoke(pct, $"ResourceManager 로드중... ({i + 1}/{toLoad.Count})");

            // 실제 프리팹 로드
            var loadHandle = Addressables.LoadAssetAsync<GameObject>(loc);
            yield return loadHandle;

            if (loadHandle.Result == null) continue;

            var address = loc.PrimaryKey as string;
            var key = GetCacheKeyFromAddress(address);
            // 캐시에 없으면 Handle 저장 (Release는 나중에 Release()에서)
            if (!string.IsNullOrEmpty(key) && !_cache.ContainsKey(key))
                _cache[key] = loadHandle;
        }
        _loadedLabels.Add(label);
        onProgress?.Invoke(1f, "Resource 로드 완료");
    }

    private string GetPrefix()
    {
        var prefix = string.IsNullOrEmpty(_prefabPathPrefix) ? "Assets/00_Prefabs" : _prefabPathPrefix.TrimEnd('/');
        return prefix + "/";
    }

    /// <summary>전체 주소에서 캐시 키 추출. prefix 제외, 확장자 제외. 예: "UI/CharacterProfile"</summary>
    private string GetCacheKeyFromAddress(string address)
    {
        if (string.IsNullOrEmpty(address)) return null;

        var prefix = GetPrefix();
        if (!address.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return null;

        // prefix 제외한 상대 경로
        var relative = address.Substring(prefix.Length);
        var parts = relative.Split('/');
        if (parts.Length < 2) return null;

        var category = parts[0];
        var nameRaw = parts[parts.Length - 1];
        var dot = nameRaw.LastIndexOf('.');
        var name = dot >= 0 ? nameRaw.Substring(0, dot) : nameRaw;
        if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(name)) return null;

        return $"{category}/{name}";
    }

    #endregion

    #region Release

    /// <summary>로드된 모든 프리팹 해제. 씬 전환·게임 종료 시 호출.</summary>
    public void Release()
    {
        // 캐시된 모든 Handle 해제 (메모리 반환)
        foreach (var kv in _cache)
        {
            if (kv.Value.IsValid())
                Addressables.Release(kv.Value);
        }
        _cache.Clear();
        _loadedLabels.Clear();
    }

    #endregion

    #region Query

    /// <summary>경로(prefix 제외)로 프리팹 반환. 예: GetPrefab("UI/CharacterProfile")</summary>
    public GameObject GetPrefab(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        // .prefab 확장자 제거
        var key = path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)
            ? path.Substring(0, path.Length - 7)
            : path;

        // 캐시 있으면 Handle.Result로 에셋 반환
        if (_cache.TryGetValue(key, out var handle))
            return handle.Result;

        return LoadAndCache(key);
    }

    /// <summary>카테고리+이름으로 프리팹 반환. 캐시 없으면 로드 후 저장.</summary>
    public GameObject GetPrefab(string category, string name)
    {
        if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(name)) return null;

        var key = $"{category}/{name}";
        if (_cache.TryGetValue(key, out var handle))
            return handle.Result;

        return LoadAndCache(key, category, name);
    }

    private GameObject LoadAndCache(string key, string category = null, string name = null)
    {
        // category/name 있으면 BuildAddress, 없으면 key로 주소 생성
        var address = !string.IsNullOrEmpty(category) && !string.IsNullOrEmpty(name)
            ? BuildAddress(category, name)
            : BuildAddressFromKey(key);

        try
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(address);
            var prefab = handle.WaitForCompletion();
            if (prefab != null)
            {
                // 캐시에 Handle 저장 (Release 시 해제됨)
                _cache[key] = handle;
                return prefab;
            }
        }
        catch (InvalidKeyException ex)
        {
            Debug.LogError($"[ResourceManager] 주소 없음: {address}. {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ResourceManager] 로드 실패: {address}. {ex.Message}");
        }
        return null;
    }

    private string BuildAddress(string category, string name)
    {
        var prefix = string.IsNullOrEmpty(_prefabPathPrefix) ? "Assets/00_Prefabs" : _prefabPathPrefix.TrimEnd('/');
        var nameWithExt = name.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) ? name : name + ".prefab";
        return $"{prefix}/{category}/{nameWithExt}";
    }

    private string BuildAddressFromKey(string key)
    {
        var prefix = string.IsNullOrEmpty(_prefabPathPrefix) ? "Assets/00_Prefabs" : _prefabPathPrefix.TrimEnd('/');
        return $"{prefix}/{key}.prefab";
    }

    #endregion

    #region Lifecycle

    private void OnDestroy()
    {
        Release();
    }

    #endregion
}
