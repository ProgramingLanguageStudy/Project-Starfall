using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

/// <summary>
/// 프리팹 로드 담당. Addressables 사용.
/// 경로 앞부분(prefix)은 Inspector에서 수정. 카테고리+이름으로 딕셔너리 캐시.
/// Release: 씬 전환·게임 종료 시 Release() 호출 권장.
/// </summary>
public class ResourceManager : MonoBehaviour
{
    [Header("경로")]
    [SerializeField] [Tooltip("프리팹 경로 앞부분. 주소 = prefix/category/name")]
    private string _prefabPathPrefix = "Assets/00_Prefabs";

    [Header("라벨")]
    [SerializeField] [Tooltip("기본 로드용 Addressables 라벨")]
    private string _defaultLoadLabel = "Prefab";

    /// <summary>category → (name → prefab) 이중 딕셔너리.</summary>
    /// <remarks>StringComparer.OrdinalIgnoreCase: "Celeste"와 "celeste"를 같은 키로 취급.</remarks>
    private readonly Dictionary<string, Dictionary<string, GameObject>> _prefabCache =
        new Dictionary<string, Dictionary<string, GameObject>>(StringComparer.OrdinalIgnoreCase);

    /// <summary>로드한 에셋 핸들. Release 시 사용.</summary>
    private readonly List<AsyncOperationHandle> _handles = new List<AsyncOperationHandle>();

    private readonly HashSet<string> _loadedLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    #region Load

    /// <summary>해당 라벨 로드 완료 여부. Boot 백그라운드 로드 후 Intro→Play 시 스킵에 사용.</summary>
    public bool IsLoaded(string label) =>
        !string.IsNullOrEmpty(label) && _loadedLabels.Contains(label);

    /// <summary>기본 라벨(_defaultLoadLabel) 로드 완료 여부.</summary>
    public bool IsLoaded() => IsLoaded(_defaultLoadLabel);

    /// <summary>라벨로 프리팹 전부 로드(동기).</summary>
    public void Load(string label)
    {
        if (string.IsNullOrEmpty(label)) return;
        if (_loadedLabels.Contains(label)) return;

        var locHandle = Addressables.LoadResourceLocationsAsync(label, typeof(GameObject));
        var locations = locHandle.WaitForCompletion();
        if (locations == null || locations.Count == 0) return;

        var prefix = (string.IsNullOrEmpty(_prefabPathPrefix) ? "Assets/00_Prefabs" : _prefabPathPrefix.TrimEnd('/')) + "/";

        foreach (var loc in locations)
        {
            var address = loc.PrimaryKey as string;
            if (string.IsNullOrEmpty(address) || !address.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;

            var relative = address.Substring(prefix.Length);
            var parts = relative.Split('/');
            if (parts.Length < 2) continue;

            var category = parts[0];
            var nameRaw = parts[parts.Length - 1];
            var dot = nameRaw.LastIndexOf('.');
            var name = dot >= 0 ? nameRaw.Substring(0, dot) : nameRaw;
            if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(name)) continue;

            if (_prefabCache.TryGetValue(category, out var byName) && byName.ContainsKey(name)) continue;

            var handle = Addressables.LoadAssetAsync<GameObject>(loc);
            var prefab = handle.WaitForCompletion();
            if (prefab == null) continue;

            if (!_prefabCache.ContainsKey(category))
                _prefabCache[category] = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
            _prefabCache[category][name] = prefab;
            _handles.Add(handle);
        }

        _loadedLabels.Add(label);
        Addressables.Release(locHandle);
    }

    /// <summary>기본 라벨(_defaultLoadLabel)로 프리팹 로드(비동기).</summary>
    public IEnumerator LoadAsync(Action<float, string> onProgress = null) =>
        LoadAsync(_defaultLoadLabel, onProgress);

    /// <summary>라벨로 프리팹 로드(비동기). 진행률 콜백 지원. 이미 로드된 라벨이면 스킵.</summary>
    public IEnumerator LoadAsync(string label, Action<float, string> onProgress = null)
    {
        if (string.IsNullOrEmpty(label)) yield break;
        if (_loadedLabels.Contains(label)) yield break;

        onProgress?.Invoke(0f, "ResourceManager 위치 조회중...");
        var locHandle = Addressables.LoadResourceLocationsAsync(label, typeof(GameObject));
        yield return locHandle;

        var locations = locHandle.Result;
        if (locations == null || locations.Count == 0)
        {
            Addressables.Release(locHandle);
            yield break;
        }

        var prefix = (string.IsNullOrEmpty(_prefabPathPrefix) ? "Assets/00_Prefabs" : _prefabPathPrefix.TrimEnd('/')) + "/";
        var toLoad = new List<IResourceLocation>();
        foreach (var loc in locations)
        {
            var address = loc.PrimaryKey as string;
            if (string.IsNullOrEmpty(address) || !address.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
            toLoad.Add(loc);
        }
        Addressables.Release(locHandle);

        if (toLoad.Count == 0) yield break;

        for (int i = 0; i < toLoad.Count; i++)
        {
            var loc = toLoad[i];
            var address = loc.PrimaryKey as string;
            var pct = (float)(i + 1) / toLoad.Count;
            onProgress?.Invoke(pct, $"ResourceManager 로드중... ({i + 1}/{toLoad.Count})");

            var loadHandle = Addressables.LoadAssetAsync<GameObject>(loc);
            yield return loadHandle;

            var prefab = loadHandle.Result;
            if (prefab == null) continue;

            var relative = address.Substring(prefix.Length);
            var parts = relative.Split('/');
            if (parts.Length < 2) continue;

            var category = parts[0];
            var nameRaw = parts[parts.Length - 1];
            var dot = nameRaw.LastIndexOf('.');
            var name = dot >= 0 ? nameRaw.Substring(0, dot) : nameRaw;
            if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(name)) continue;

            if (!_prefabCache.ContainsKey(category))
                _prefabCache[category] = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
            _prefabCache[category][name] = prefab;
            _handles.Add(loadHandle);
        }
        _loadedLabels.Add(label);
        onProgress?.Invoke(1f, "Resource 로드 완료");
    }

    #endregion

    #region Release

    /// <summary>로드된 모든 프리팹 해제. 씬 전환·게임 종료 시 호출.</summary>
    public void Release()
    {
        foreach (var h in _handles)
        {
            if (h.IsValid())
                Addressables.Release(h);
        }
        _handles.Clear();
        _loadedLabels.Clear();
        _prefabCache.Clear();
    }

    #endregion

    #region Query

    /// <summary>카테고리+이름으로 프리팹 반환. 캐시 없으면 로드 후 저장.</summary>
    public GameObject GetPrefab(string category, string name)
    {
        if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(name)) return null;

        if (_prefabCache.TryGetValue(category, out var byName) &&
            byName.TryGetValue(name, out var cached))
            return cached;

        var address = BuildAddress(category, name);
        try
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(address);
            var prefab = handle.WaitForCompletion();
            if (prefab != null)
            {
                if (!_prefabCache.ContainsKey(category))
                    _prefabCache[category] = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
                _prefabCache[category][name] = prefab;
                _handles.Add(handle);
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

    #endregion

    #region Lifecycle

    private void OnDestroy()
    {
        Release();
    }

    #endregion
}
