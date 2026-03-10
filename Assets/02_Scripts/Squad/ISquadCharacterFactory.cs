using UnityEngine;

/// <summary>
/// 분대 Character 생성/삭제. Instantiate·풀링 등 구현 교체용.
/// </summary>
public interface ISquadCharacterFactory
{
    /// <summary>Character 생성. Model.Initialize, Character.Initialize 호출 후 반환.</summary>
    Character Create(CharacterData data, Vector3 position, Transform parent, CombatController combatController, Squad squad, float spawnRadius = 2f);

    /// <summary>Character 제거. 풀링 시 반환, 아니면 Destroy.</summary>
    void Destroy(Character character);
}
