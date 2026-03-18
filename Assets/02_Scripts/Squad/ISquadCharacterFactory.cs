using UnityEngine;

/// <summary>
/// 분대 Character 생성/삭제. Instantiate·풀링 등 구현 교체용.
/// id 기반: DM.Get + RM.GetPrefab + Model.Initialize(Data).
/// </summary>
public interface ISquadCharacterFactory
{
    /// <summary>Character 생성. DM.Get + RM.GetPrefab + Model.Initialize 호출 후 반환.</summary>
    Character Create(string characterId, Vector3 position, Transform parent, CombatController combatController, Squad squad, float spawnRadius = 2f);

    /// <summary>Character 제거. 풀링 시 반환, 아니면 Destroy.</summary>
    void Destroy(Character character);
}
