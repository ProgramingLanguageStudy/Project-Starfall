using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

/// <summary>
/// 프리팹 로드 담당. Addressables 사용.
/// 경로(prefix 제외)를 키로, Handle을 값으로 캐시. Handle 보관으로 개별/전체 Release 가능.
/// 씬 전환·게임 종료 시 Release() 호출 권장.
/// </summary>
public class ResourceManager : MonoBehaviour
{
    /// <summary>경로(prefix 제외, 확장자 제외) → Handle. 예: "UI/CharacterProfile"</summary>
    private readonly Dictionary<string, AsyncOperationHandle<GameObject>> _cache =
        new Dictionary<string, AsyncOperationHandle<GameObject>>(StringComparer.OrdinalIgnoreCase);

    private bool _isLoaded;

    #region Load

    /// <summary>프리팹 로드 완료 여부. GameManager 부트 후 Intro→Play 시 스킵에 사용.</summary>
    public bool IsLoaded() => _isLoaded;

    /// <summary>라벨로 프리팹 로드(비동기). 진행률 콜백 지원. 이미 로드됐으면 스킵.</summary>
    public IEnumerator LoadAsync(Action<float, string> onProgress = null)
    {
        if (_isLoaded) yield break;

        // 1) 라벨로 위치 목록 조회 (에셋 주소들. 아직 로드 안 됨)
        onProgress?.Invoke(0f, "ResourceManager 위치 조회중...");
        var locHandle = Addressables.LoadResourceLocationsAsync(AddressableConfig.PrefabLabel, typeof(GameObject));
        yield return locHandle;

        var locations = locHandle.Result;
        Addressables.Release(locHandle); // 위치 조회 Handle은 결과만 쓰고 해제

        if (locations == null || locations.Count == 0)
        {
            _isLoaded = true;
            yield break;
        }

        // 2) 각 위치별로 프리팹 로드 → Handle 캐시 (Release 시 해제됨)
        for (int i = 0; i < locations.Count; i++)
        {
            var loc = locations[i];
            var pct = (float)(i + 1) / locations.Count;
            onProgress?.Invoke(pct, $"ResourceManager 로드중... ({i + 1}/{locations.Count})");

            var loadHandle = Addressables.LoadAssetAsync<GameObject>(loc);
            yield return loadHandle;

            if (loadHandle.Result == null) continue;

            // 3) 주소에서 캐시 키 추출 후 저장 (GetPrefab 조회용)
            var address = loc.PrimaryKey as string;
            var key = GetCacheKeyFromAddress(address);
            if (!string.IsNullOrEmpty(key) && !_cache.ContainsKey(key))
                _cache[key] = loadHandle;
        }
        _isLoaded = true;
        onProgress?.Invoke(1f, "Resource 로드 완료");
    }

    /// <summary>주소에서 캐시 키 추출. 예: Assets/00_Prefabs/UI/CharacterProfile.prefab → UI/CharacterProfile</summary>
    private string GetCacheKeyFromAddress(string address)
    {
        if (string.IsNullOrEmpty(address)) return null;

        // 1) 경로 정규화: \ (실제로는 \\가 \임. 규칙임) → / 
        // 끝의 / 제거 (Windows·에디터 혼용 대비)
        var normalized = address.Replace('\\', '/').TrimEnd('/');
        if (!normalized.StartsWith(AddressableConfig.PrefabPrefix))
            return null;

        // 2) Prefix 제거(미리 정해둔 앞부분 경로) → suffix (예: UI/CharacterProfile.prefab)
        var suffix = normalized.Substring(AddressableConfig.PrefabPrefix.Length);
        if (string.IsNullOrEmpty(suffix)) return null;

        // 3) 확장자 제거(.prefab 제거) → 캐시 키 (예: UI/CharacterProfile)
        var dot = suffix.LastIndexOf('.');
        var key = dot >= 0 ? suffix.Substring(0, dot) : suffix;
        return string.IsNullOrEmpty(key) ? null : key;
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
        _isLoaded = false;
    }

    #endregion

    #region Query

    /// <summary>경로(prefix 제외)로 프리팹 반환. 라벨 프리로드 후 캐시에서만 조회. 예: GetPrefab("UI/CharacterProfile")</summary>
    public GameObject GetPrefab(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        // 호출자가 "UI/CharacterProfile.prefab"처럼 확장자까지 넣을 수 있으므로 제거 후 캐시 키로 사용
        var key = path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)
            ? path.Substring(0, path.Length - 7)
            : path;

        if (_cache.TryGetValue(key, out var handle))
            return handle.Result;

        Debug.LogError($"[ResourceManager] 캐시 없음: {key}. 라벨 프리로드에 포함됐는지 확인.");
        return null;
    }

    /// <summary>카테고리+이름으로 프리팹 반환. 캐시에서만 조회.</summary>
    public GameObject GetPrefab(string category, string name)
    {
        if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(name)) return null;

        var key = $"{category}/{name}";
        if (_cache.TryGetValue(key, out var handle))
            return handle.Result;

        Debug.LogError($"[ResourceManager] 캐시 없음: {key}. 라벨 프리로드에 포함됐는지 확인.");
        return null;
    }

    /// <summary>
    /// 비동기 프리팹 획득. 캐시에 없으면 Addressables로 직접 로드하여 캐싱 후 반환.
    /// 대규모 에셋이나 특정 시점에만 필요한 에셋(On-demand) 관리에 적합.
    /// </summary>
    public async Task<GameObject> GetPrefabAsync(string category, string name)
    {
        if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(name)) return null;

        var key = $"{category}/{name}";
        
        // 1. 캐시 확인
        if (_cache.TryGetValue(key, out var handle))
            return handle.Result;

        // 2. 캐시에 없으면 직접 로드 시도 (On-demand Loading)
        string address = $"{AddressableConfig.PrefabPrefix}{key}.prefab";
        var loadHandle = Addressables.LoadAssetAsync<GameObject>(address);
        
        await loadHandle.Task;

        if (loadHandle.Status == AsyncOperationStatus.Succeeded)
        {
            _cache[key] = loadHandle;
            return loadHandle.Result;
        }

        Debug.LogError($"[ResourceManager] 비동기 로드 실패: {address}");
        return null;
    }

    #endregion

    #region Lifecycle

    private void OnDestroy()
    {
        Release();
    }

    #endregion
}
