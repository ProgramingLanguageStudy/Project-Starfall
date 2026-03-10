using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 분대 조율. Squad(상태) + ISquadCharacterFactory(생성) 연동.
/// 역할 배정, 입력 위임, 포메이션 등.
/// </summary>
public class SquadController : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] [Tooltip("Character 생성/삭제. 풀링 등")]
    private PooledSquadCharacterFactory _factory;
    [SerializeField] [Tooltip("스폰된 분대 부모. 비면 this")]
    private Transform _squadRoot;
    [SerializeField] [Tooltip("세이브 없을 때 기본 스폰 위치")]
    private Transform _spawnPoint;

    [Header("스폰")]
    [SerializeField] private float _spawnRadius = 2f;

    private Squad _squad = new Squad();
    private ISquadCharacterFactory _factoryInterface;
    private CombatController _combatController;

    public Squad Squad => _squad;
    public IReadOnlyList<Character> Characters => _squad.Members;
    public Character PlayerCharacter => _squad.Player;
    public bool CanMove { get; set; } = true;

    public event Action<Character> OnPlayerChanged;
    public Character DefaultPlayer => _squad.Members.Count > 0 ? _squad.Members[0] : null;
    public Transform PlayerTransform => PlayerCharacter?.transform;

    private void Awake()
    {
        _factoryInterface = _factory != null ? _factory : GetComponent<PooledSquadCharacterFactory>();
        _squad.OnPlayerChanged += c => OnPlayerChanged?.Invoke(c);
    }

    /// <summary>분대 스폰.</summary>
    public void Initialize(Vector3? spawnPositionOverride = null, CombatController combatController = null, SquadSaveData squadSaveData = null)
    {
        _combatController = combatController;

        var root = _squadRoot != null ? _squadRoot : transform;
        var basePos = spawnPositionOverride ?? (_spawnPoint != null ? _spawnPoint.position : transform.position);

        if (squadSaveData != null && squadSaveData.members != null && squadSaveData.members.Count > 0)
            SpawnFromSaveData(basePos, root, squadSaveData);
        else
            SpawnInitialSquad(basePos, root);
    }

    private void SpawnFromSaveData(Vector3 basePos, Transform root, SquadSaveData squadSaveData)
    {
        var dm = GameManager.Instance?.DataManager;
        if (dm == null)
        {
            SpawnInitialSquad(basePos, root);
            return;
        }

        var members = new List<Character>();
        Character firstCharacter = null;
        Character targetPlayer = null;
        int index = 0;

        foreach (var m in squadSaveData.members)
        {
            if (string.IsNullOrEmpty(m.characterId)) continue;

            var data = dm.GetCharacterData(m.characterId);
            if (data == null || data.prefab == null) continue;

            var pos = basePos + GetSpawnOffset(index);
            var c = CreateCharacter(data, pos, root);
            if (c == null) continue;

            c.Model?.SetCurrentHpForLoad(m.currentHp);
            members.Add(c);
            if (index == 0) firstCharacter = c;

            if (!string.IsNullOrEmpty(squadSaveData.currentPlayerId) &&
                (m.characterId == squadSaveData.currentPlayerId || c.Model?.Data?.displayName == squadSaveData.currentPlayerId))
                targetPlayer = c;

            index++;
        }

        _squad.SetMembersAndPlayer(members, targetPlayer ?? firstCharacter);
        ApplyRoles(_squad.Player);
        OnPlayerChanged?.Invoke(_squad.Player);
    }

    private void SpawnInitialSquad(Vector3 basePos, Transform root)
    {
        var dm = GameManager.Instance?.DataManager;
        var data = dm?.GetCharacterData("Celeste");
        if (data == null || data.prefab == null)
        {
            _squad.SetMembersAndPlayer(new List<Character>(), null);
            return;
        }

        var c = CreateCharacter(data, basePos, root);
        if (c == null)
        {
            _squad.SetMembersAndPlayer(new List<Character>(), null);
            return;
        }

        _squad.SetMembersAndPlayer(new List<Character> { c }, c);
        ApplyRoles(c);
        OnPlayerChanged?.Invoke(c);
    }

    private Character CreateCharacter(CharacterData data, Vector3 position, Transform parent)
    {
        if (_factoryInterface == null) return null;
        return _factoryInterface.Create(data, position, parent, _combatController, _squad, _spawnRadius);
    }

    private void ApplyRoles(Character player)
    {
        if (player == null) return;

        player.SetAsPlayer();
        foreach (var c in _squad.Members)
        {
            if (c != null && c != player)
                c.SetAsCompanion(player.transform);
        }
    }

    public void SetPlayerCharacter(Character character)
    {
        if (character == null || character == _squad.Player) return;
        if (!_squad.Members.Contains(character)) return;

        _squad.Player?.SetAsCompanion(character.transform);
        character.SetAsPlayer();
        _squad.SetPlayer(character);
        ApplyRoles(character);
    }

    public void RequestInteract() => PlayerCharacter?.Interactor?.TryInteract();
    public void RequestAttack() => PlayerCharacter?.RequestAttack();

    public bool RequestSquadSwap()
    {
        if (PlayerCharacter == null || _squad.Members.Count == 0) return false;

        int idx = _squad.Members.IndexOf(PlayerCharacter);
        if (idx < 0) idx = 0;
        int nextIdx = (idx + 1) % _squad.Members.Count;
        var next = _squad.Members[nextIdx];
        if (next == PlayerCharacter) return false;

        SetPlayerCharacter(next);
        return true;
    }

    public void TeleportToDefaultPoint()
    {
        var pos = _spawnPoint != null ? _spawnPoint.position : transform.position;
        TeleportPlayer(pos);
    }

    public void TeleportPlayer(Vector3 worldPosition)
    {
        if (PlayerCharacter == null) return;
        PlayerCharacter.Teleport(worldPosition);
        RepositionCompanionsAround(PlayerCharacter.transform);
    }

    private Vector3 GetSpawnOffset(int index)
    {
        if (index <= 0) return Vector3.zero;
        float angle = (360f / 4f) * index * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(angle) * _spawnRadius, 0f, Mathf.Sin(angle) * _spawnRadius);
    }

    public Character AddCompanion(CharacterData data)
    {
        if (data == null || data.prefab == null) return null;
        if (_factoryInterface == null) return null;

        var followTarget = PlayerCharacter != null ? PlayerCharacter.transform : transform;
        var root = _squadRoot != null ? _squadRoot : transform;
        var pos = followTarget.position + GetSpawnOffset(_squad.Members.Count);

        var c = CreateCharacter(data, pos, root);
        if (c == null) return null;

        _squad.AddMember(c);
        c.SetAsCompanion(followTarget);
        return c;
    }

    public void RemoveCharacter(Character character)
    {
        if (character == null) return;
        _squad.RemoveMember(character);
        _factoryInterface?.Destroy(character);
    }

    public void RepositionCompanionsAround(Transform center)
    {
        if (center == null) return;
        var basePos = center.position;
        int companionIndex = 0;

        foreach (var c in _squad.Members)
        {
            if (c == null || c.transform == center) continue;
            companionIndex++;

            var nearPos = basePos + GetSpawnOffset(companionIndex);
            if (NavMesh.SamplePosition(nearPos, out var hit, _spawnRadius * 2f, NavMesh.AllAreas))
            {
                c.ResetNavMeshPath();
                c.Teleport(hit.position);
            }
        }
    }
}
