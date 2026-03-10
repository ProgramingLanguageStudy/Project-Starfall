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
/// Release: 씬 전환·게임 종료 시 ReleasePreloaded 호출 권장.
/// </summary>
public class ResourceManager : MonoBehaviour
{
    [Header("경로")]
    [SerializeField] [Tooltip("프리팹 경로 앞부분. 주소 = prefix/category/name")]
    private string _prefabPathPrefix = "Assets/00_Prefabs";

    /// <summary>category → (name → prefab) 이중 딕셔너리.</summary>
    /// <remarks>StringComparer.OrdinalIgnoreCase: "Celeste"와 "celeste"를 같은 키로 취급.</remarks>
    private readonly Dictionary<string, Dictionary<string, GameObject>> _prefabCache =
        new Dictionary<string, Dictionary<string, GameObject>>(StringComparer.OrdinalIgnoreCase);

    /// <summary>로드한 에셋 핸들. Release 시 사용.</summary>
    private readonly List<AsyncOperationHandle> _handles = new List<AsyncOperationHandle>();

    /// <summary>라벨로 프리팹 전부 선로드. PrimaryKey(주소) 파싱해 category/name으로 캐시.</summary>
    public void PreloadByLabel(string label)
    {
        if (string.IsNullOrEmpty(label)) return;

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

        Addressables.Release(locHandle);
    }

    /// <summary>라벨로 프리팹 선로드(비동기). PercentComplete/GetDownloadStatus로 진행률 보고.</summary>
    public IEnumerator PreloadByLabelAsync(string label, Action<float, string> onProgress = null)
    {
        if (string.IsNullOrEmpty(label)) yield break;

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
        onProgress?.Invoke(1f, "ResourceManager 로드 완료");
    }

    /// <summary>로드된 모든 프리팹 해제. 씬 전환·게임 종료 시 호출.</summary>
    public void ReleasePreloaded()
    {
        foreach (var h in _handles)
        {
            if (h.IsValid())
                Addressables.Release(h);
        }
        _handles.Clear();
        _prefabCache.Clear();
    }

    /// <summary>카테고리+이름으로 프리팹 반환. 캐시 없으면 로드 후 저장.</summary>
    public GameObject GetPrefab(string category, string name)
    {
        if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(name)) return null;

        if (_prefabCache.TryGetValue(category, out var byName) &&
            byName.TryGetValue(name, out var cached))
            return cached;

        var address = BuildAddress(category, name);
        var handle = Addressables.LoadAssetAsync<GameObject>(address);
        var prefab = handle.WaitForCompletion();
        if (prefab != null)
        {
            if (!_prefabCache.ContainsKey(category))
                _prefabCache[category] = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
            _prefabCache[category][name] = prefab;
            _handles.Add(handle);
        }
        return prefab;
    }

    private string BuildAddress(string category, string name)
    {
        var prefix = string.IsNullOrEmpty(_prefabPathPrefix) ? "Assets/00_Prefabs" : _prefabPathPrefix.TrimEnd('/');
        return $"{prefix}/{category}/{name}";
    }

    private void OnDestroy()
    {
        ReleasePreloaded();
    }
}
