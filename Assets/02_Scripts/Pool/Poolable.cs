using UnityEngine;

/// <summary>
/// 풀링된 오브젝트. ReturnToPool() 호출 시 풀에 반환.
/// </summary>
public class Poolable : MonoBehaviour
{
    private Pool _pool;

    public void SetPool(Pool pool) => _pool = pool;

    public void ReturnToPool()
    {
        if (_pool != null)
        {
            _pool.Push(gameObject);
        }
        else
        {
            Debug.LogError($"[Poolable] Pool is null for object: {gameObject.name}");
            Destroy(gameObject);
        }
    }
}
