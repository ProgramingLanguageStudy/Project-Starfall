using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 풀링 기반 Character 생성/삭제. PoolManager 사용.
/// Character 프리팹에 Poolable 컴포넌트 필요.
/// </summary>
public class PooledSquadCharacterFactory : MonoBehaviour, ISquadCharacterFactory
{
    [SerializeField] [Tooltip("풀 관리. 비면 씬에서 찾음")]
    private PoolManager _poolManager;

    private PoolManager PoolManager => _poolManager != null ? _poolManager : FindFirstObjectByType<PoolManager>();

    public Character Create(CharacterData data, Vector3 position, Transform parent, CombatController combatController, Squad squad, float spawnRadius = 2f)
    {
        if (data == null || data.prefab == null) return null;

        var pm = PoolManager;
        if (pm == null)
        {
            Debug.LogWarning("[PooledSquadCharacterFactory] PoolManager 없음. Instantiate로 폴백.");
            return CreateWithInstantiate(data, position, parent, combatController, squad, spawnRadius);
        }

        if (!NavMesh.SamplePosition(position, out var hit, spawnRadius * 2f, NavMesh.AllAreas))
            hit.position = position;

        var go = pm.Pop(data.prefab);
        if (go == null) return null;

        go.transform.SetParent(parent);
        go.transform.position = hit.position;
        go.transform.rotation = Quaternion.identity;

        var character = go.GetComponent<Character>();
        if (character == null)
        {
            pm.Push(data.prefab, go);
            return null;
        }

        var model = character.Model;
        if (model != null && model.Data != data)
            model.Initialize(data);

        character.Initialize(combatController, squad);
        return character;
    }

    public void Destroy(Character character)
    {
        if (character == null) return;

        var poolable = character.GetComponent<Poolable>();
        if (poolable != null)
        {
            var prefab = character.Model?.Data?.prefab;
            if (prefab != null)
            {
                var pm = PoolManager;
                if (pm != null)
                    pm.Push(prefab, character.gameObject);
                else
                    poolable.ReturnToPool();
            }
            else
                poolable.ReturnToPool();
        }
        else
        {
            Destroy(character.gameObject);
        }
    }

    private Character CreateWithInstantiate(CharacterData data, Vector3 position, Transform parent, CombatController combatController, Squad squad, float spawnRadius)
    {
        if (!NavMesh.SamplePosition(position, out var hit, spawnRadius * 2f, NavMesh.AllAreas))
            hit.position = position;

        var go = Object.Instantiate(data.prefab, hit.position, Quaternion.identity, parent);
        var character = go.GetComponent<Character>();
        if (character == null)
        {
            Object.Destroy(go);
            return null;
        }

        var model = character.Model;
        if (model != null && model.Data != data)
            model.Initialize(data);

        character.Initialize(combatController, squad);
        return character;
    }
}
