using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 풀 관리. 프리팹별 Pool 보유. Play 씬 또는 GameManager 하위에 배치.
/// 프리팹은 ResourceManager 등에서 로드 후 참조 전달.
/// </summary>
public class PoolManager : MonoBehaviour
{
    private readonly Dictionary<int, Pool> _poolMap = new Dictionary<int, Pool>();

    /// <summary>프리팹에 해당하는 풀 반환. 없으면 새로 생성.</summary>
    public Pool GetPool(GameObject prefab, int initialSize = 10)
    {
        if (prefab == null) return null;

        int key = prefab.GetInstanceID();
        if (!_poolMap.TryGetValue(key, out Pool pool))
        {
            Transform parent = new GameObject($"Pool_{prefab.name}").transform;
            parent.SetParent(transform);
            pool = new Pool(prefab, parent, initialSize);
            _poolMap[key] = pool;
        }
        return pool;
    }

    /// <summary>풀에서 오브젝트 꺼내기.</summary>
    public GameObject Pop(GameObject prefab)
    {
        Pool pool = GetPool(prefab);
        if (pool == null)
        {
            Debug.LogError($"[PoolManager] Pool not found for prefab: {prefab?.name ?? "null"}");
            return null;
        }
        
        GameObject result = pool.Pop();
        if (result == null)
        {
            Debug.LogError($"[PoolManager] Failed to pop object from pool: {prefab.name}");
        }
        return result;
    }

    /// <summary>풀에 반환. Poolable.ReturnToPool() 사용 권장.</summary>
    public void Push(GameObject prefab, GameObject instance)
    {
        if (prefab == null || instance == null) 
        {
            Debug.LogError($"[PoolManager] Push called with null prefab or instance");
            return;
        }
        
        Pool pool = GetPool(prefab);
        if (pool == null)
        {
            Debug.LogError($"[PoolManager] Pool not found for prefab: {prefab.name}");
            Destroy(instance);
            return;
        }
        
        pool.Push(instance);
    }

    /// <summary>모든 풀에서 파괴된 오브젝트 참조 제거. 씬 전환·플레이 종료 시 호출 권장.</summary>
    public void RemoveDestroyedFromAllPools()
    {
        foreach (Pool pool in _poolMap.Values)
            pool.RemoveDestroyed();
    }
}
