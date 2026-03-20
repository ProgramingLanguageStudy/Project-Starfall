using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Instantiate 기반 Character 생성/삭제. 풀링 미사용.
/// 분대원 생성 빈도가 낮아 풀 불필요. 경로: Character/{id}.prefab
/// </summary>
public class InstantiateSquadCharacterFactory : MonoBehaviour, ISquadCharacterFactory
{
    private const string PrefabCategory = "Character";

    public Character Create(string characterId, Vector3 position, Transform parent, CombatController combatController, Squad squad, float spawnRadius = 2f)
    {
        if (string.IsNullOrEmpty(characterId)) return null;

        var dm = GameManager.Instance?.DataManager;
        var rm = GameManager.Instance?.ResourceManager;
        if (dm == null || rm == null)
        {
            Debug.LogWarning("[InstantiateSquadCharacterFactory] DataManager 또는 ResourceManager 없음.");
            return null;
        }

        var data = dm.Get<CharacterData>(characterId);
        if (data == null)
        {
            Debug.LogWarning($"[InstantiateSquadCharacterFactory] CharacterData 없음: {characterId}");
            return null;
        }

        var prefab = rm.GetPrefab(PrefabCategory, data.Id);
        if (prefab == null)
        {
            Debug.LogWarning($"[InstantiateSquadCharacterFactory] 프리팹 없음: {PrefabCategory}/{data.Id}. Addressables 재빌드·파일명 확인.");
            return null;
        }

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
        if (model != null)
            model.Initialize(data);

        character.Initialize(combatController, squad);
        return character;
    }

    public void Destroy(Character character)
    {
        if (character == null) return;
        Object.Destroy(character.gameObject);
    }
}
