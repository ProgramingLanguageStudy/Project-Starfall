using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 풀링 기반 Character 생성/삭제. PoolManager 사용.
/// Character 프리팹에 Poolable 컴포넌트 필요. 경로: Character/{id}.prefab
/// </summary>
public class PooledSquadCharacterFactory : MonoBehaviour, ISquadCharacterFactory
{
    private const string PrefabCategory = "Character";

    [SerializeField] [Tooltip("풀 관리. 비면 씬에서 찾음")]
    private PoolManager _poolManager;

    private PoolManager PoolManager => _poolManager != null ? _poolManager : FindFirstObjectByType<PoolManager>();

    public Character Create(string characterId, Vector3 position, Transform parent, CombatController combatController, Squad squad, float spawnRadius = 2f)
    {
        if (string.IsNullOrEmpty(characterId)) return null;

        var dm = GameManager.Instance?.DataManager;
        var rm = GameManager.Instance?.ResourceManager;
        if (dm == null || rm == null)
        {
            Debug.LogWarning("[PooledSquadCharacterFactory] DataManager 또는 ResourceManager 없음.");
            return null;
        }

        var data = dm.Get<CharacterData>(characterId);
        if (data == null)
        {
            Debug.LogWarning($"[PooledSquadCharacterFactory] CharacterData 없음: {characterId}");
            return null;
        }

        var prefab = rm.GetPrefab(PrefabCategory, data.Id);
        if (prefab == null)
        {
            Debug.LogWarning($"[PooledSquadCharacterFactory] 프리팹 없음: {PrefabCategory}/{data.Id}. Addressables 재빌드·파일명 확인.");
            return null;
        }

        var pm = PoolManager;
        if (pm == null)
        {
            Debug.LogWarning("[PooledSquadCharacterFactory] PoolManager 없음. Instantiate로 폴백.");
            return CreateWithInstantiate(prefab, data, position, parent, combatController, squad, spawnRadius);
        }

        if (!NavMesh.SamplePosition(position, out var hit, spawnRadius * 2f, NavMesh.AllAreas))
            hit.position = position;

        var go = pm.Pop(prefab);
        if (go == null) return null;

        go.transform.SetParent(parent);
        go.transform.position = hit.position;
        go.transform.rotation = Quaternion.identity;

        var character = go.GetComponent<Character>();
        if (character == null)
        {
            pm.Push(prefab, go);
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
            var data = character.Model?.Data;
            var prefab = data != null ? GameManager.Instance?.ResourceManager?.GetPrefab(PrefabCategory, data.Id) : null;
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
            Object.Destroy(character.gameObject);
        }
    }

    private Character CreateWithInstantiate(GameObject prefab, CharacterData data, Vector3 position, Transform parent, CombatController combatController, Squad squad, float spawnRadius)
    {
        if (!NavMesh.SamplePosition(position, out var hit, spawnRadius * 2f, NavMesh.AllAreas))
            hit.position = position;

        var go = Object.Instantiate(prefab, hit.position, Quaternion.identity, parent);
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
