using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 분대 제어. Character 관리·플레이어/동료 역할·스폰·따라가기.
/// PlayerCharacter 변경 시 OnPlayerChanged 발행.
/// IPlayerProvider 구현. PlaySceneServices에 등록해 Player 참조 제공.
/// </summary>
public class SquadController : MonoBehaviour, IPlayerProvider
{
    [Header("참조")]
    [SerializeField] [Tooltip("스폰된 분대 부모(비어 있으면 this)")]
    private Transform _squadRoot;
    [SerializeField] [Tooltip("세이브 없을 때만 사용(방어코드). 마을 한가운데 등 유효한 NavMesh 위치에 배치 권장. 비면 this.position")]
    private Transform _spawnPoint;

    [Header("초기 분대")]
    [SerializeField] [Tooltip("전체 분대. [0]=기본 플레이어, [1~]=동료. prefab은 각 Data에 있어야 함")]
    private List<CharacterData> _initialSquad = new List<CharacterData>();

    [Header("스폰")]
    [SerializeField] [Tooltip("플레이어 기준 동료 스폰 반경. NavMesh 샘플링에 사용")]
    private float _spawnRadius = 2f;

    private readonly List<Character> _characters = new List<Character>();
    private Character _playerCharacter;
    private CombatController _combatController;

    public IReadOnlyList<Character> Characters => _characters;
    public Character PlayerCharacter => _playerCharacter;
    public bool CanMove { get; set; } = true;

    /// <summary>플레이어 캐릭터 변경 시 발행. chase target, UI 등 갱신에 사용.</summary>
    public event Action<Character> OnPlayerChanged;

    /// <summary>기본 플레이어(스폰 리스트 첫 번째). 세이브 없을 때 조종 대상.</summary>
    public Character DefaultPlayer => _characters.Count > 0 ? _characters[0] : null;

    Character IPlayerProvider.GetPlayer() => PlayerCharacter;
    Transform IPlayerProvider.GetPlayerTransform() => PlayerCharacter?.transform;

    /// <summary>따라갈 대상 설정. PlayScene 등에서 현재 조종 캐릭터.transform 전달.</summary>
    public void SetFollowTarget(Transform target)
    {
        foreach (var c in _characters)
        {
            if (c == null) continue;
            if (c.transform == target) continue;
            c.SetFollowTarget(target);
        }
    }

    /// <summary>분대 스폰. spawnPositionOverride가 있으면 그 위치(세이브 기준), 없으면 _spawnPoint(방어코드). squadSaveData가 있으면 세이브 멤버 기준 스폰(동료 저장/로드).</summary>
    public void Initialize(Vector3? spawnPositionOverride = null, CombatController combatController = null, SquadSaveData squadSaveData = null)
    {
        _combatController = combatController;
        if (squadSaveData != null && squadSaveData.members != null && squadSaveData.members.Count > 0)
            SpawnFromSaveData(spawnPositionOverride, squadSaveData);
        else
            SpawnInitialSquad(spawnPositionOverride);
    }

    private void SpawnFromSaveData(Vector3? spawnPositionOverride, SquadSaveData squadSaveData)
    {
        _characters.Clear();
        var root = _squadRoot != null ? _squadRoot : transform;
        var basePos = spawnPositionOverride ?? (_spawnPoint != null ? _spawnPoint.position : transform.position);
        var dm = GameManager.Instance?.DataManager;
        if (dm == null)
        {
            Debug.LogWarning("[SquadController] DataManager 없음. _initialSquad로 폴백.");
            SpawnInitialSquad(spawnPositionOverride);
            return;
        }

        Character firstCharacter = null;
        Character targetPlayer = null;
        int index = 0;

        foreach (var m in squadSaveData.members)
        {
            if (string.IsNullOrEmpty(m.characterId)) continue;

            var data = dm.GetCharacterData(m.characterId);
            if (data == null || data.prefab == null)
            {
                Debug.LogWarning($"[SquadController] CharacterData 없음: {m.characterId}. Resources/Characters 확인.");
                continue;
            }

            var offset = GetSpawnOffset(index);
            var character = SpawnCharacterInternal(data, basePos + offset, root);
            if (character == null) continue;

            _characters.Add(character);
            character.Model?.SetCurrentHpForLoad(m.currentHp);

            var companionStateMachine = character.GetComponent<CompanionStateMachine>();
            companionStateMachine?.Initialize(_combatController);

            if (index == 0)
                firstCharacter = character;

            if (!string.IsNullOrEmpty(squadSaveData.currentPlayerId) &&
                (m.characterId == squadSaveData.currentPlayerId || character.Model?.Data?.displayName == squadSaveData.currentPlayerId))
                targetPlayer = character;

            index++;
        }

        _playerCharacter = targetPlayer ?? firstCharacter;
        if (_playerCharacter != null)
        {
            _playerCharacter.SetAsPlayer();
            foreach (var c in _characters)
            {
                if (c != null && c != _playerCharacter)
                    c.SetAsCompanion(_playerCharacter.transform);
            }
        }
        OnPlayerChanged?.Invoke(_playerCharacter);
    }

    private void SpawnInitialSquad(Vector3? spawnPositionOverride)
    {
        _characters.Clear();
        var root = _squadRoot != null ? _squadRoot : transform;
        var basePos = spawnPositionOverride ?? (_spawnPoint != null ? _spawnPoint.position : transform.position);
        Character firstCharacter = null;
        int index = 0;

        foreach (var data in _initialSquad)
        {
            if (data == null || data.prefab == null)
            {
                Debug.LogWarning("[SquadController] CharacterData 또는 prefab이 비어 있습니다. 스킵합니다.");
                continue;
            }

            var offset = GetSpawnOffset(index);
            var character = SpawnCharacterInternal(data, basePos + offset, root);
            if (character == null) continue;

            _characters.Add(character);

            var companionStateMachine = character.GetComponent<CompanionStateMachine>();
            companionStateMachine?.Initialize(_combatController);

            if (index == 0)
            {
                firstCharacter = character;
                character.SetAsPlayer();
            }
            else
            {
                character.SetAsCompanion(firstCharacter != null ? firstCharacter.transform : transform);
            }
            index++;
        }

        _playerCharacter = firstCharacter;
        OnPlayerChanged?.Invoke(_playerCharacter);
    }

    /// <summary>플레이어 캐릭터 설정. 외부(세이브 로드, 스왑 등)에서 호출.</summary>
    public void SetPlayerCharacter(Character character)
    {
        if (character == null || character == _playerCharacter) return;
        if (!_characters.Contains(character)) return;

        _playerCharacter?.SetAsCompanion(character.transform);
        character.SetAsPlayer();
        _playerCharacter = character;
        OnPlayerChanged?.Invoke(_playerCharacter);
    }

    /// <summary>분대원 순환 교체. 다음 캐릭터를 플레이어로. 교체되면 true.</summary>
    public bool SwapSquad()
    {
        if (_playerCharacter == null || _characters.Count == 0) return false;

        int idx = _characters.IndexOf(_playerCharacter);
        if (idx < 0) idx = 0;
        int nextIdx = (idx + 1) % _characters.Count;
        var next = _characters[nextIdx];
        if (next == _playerCharacter) return false;

        SetPlayerCharacter(next);
        return true;
    }

    /// <summary>플레이어를 목표 위치로 텔레포트. 디버거·포탈 등에서 사용.</summary>
    public void TeleportPlayer(Vector3 worldPosition) => _playerCharacter?.Teleport(worldPosition);
    public void TeleportPlayer(Transform destination) => _playerCharacter?.Teleport(destination);

    private Vector3 GetSpawnOffset(int index)
    {
        if (index <= 0) return Vector3.zero;
        float angle = (360f / 4f) * index * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(angle) * _spawnRadius, 0f, Mathf.Sin(angle) * _spawnRadius);
    }

    /// <summary>캐릭터 1명 스폰. NavMesh 유효 위치에 배치. Player/Companion 설정은 호출부에서.</summary>
    private Character SpawnCharacterInternal(CharacterData data, Vector3 nearPosition, Transform parent)
    {
        if (data == null || data.prefab == null) return null;

        if (!NavMesh.SamplePosition(nearPosition, out var hit, _spawnRadius * 2f, NavMesh.AllAreas))
        {
            Debug.LogWarning($"[SquadController] NavMesh 샘플 실패. nearPosition={nearPosition}");
            hit.position = nearPosition;
        }

        var instance = Instantiate(data.prefab, hit.position, Quaternion.identity, parent);
        var character = instance.GetComponent<Character>();

        if (character == null)
        {
            Debug.LogWarning("[SquadController] 프리팹에 Character 컴포넌트가 없습니다.");
            return null;
        }

        var model = character.Model;
        if (model != null && model.Data != data)
            model.Initialize(data);

        character.Initialize();
        return character;
    }

    /// <summary>캐릭터 1명 스폰 (런타임 추가용). followTarget 지정 필요.</summary>
    public Character SpawnCharacter(CharacterData data, Vector3 nearPosition, Transform followTarget, Transform parent = null)
    {
        var root = parent != null ? parent : (_squadRoot != null ? _squadRoot : transform);
        var c = SpawnCharacterInternal(data, nearPosition, root);
        if (c != null)
        {
            c.GetComponent<CompanionStateMachine>()?.Initialize(_combatController);
            c.SetAsCompanion(followTarget);
            _characters.Add(c);
        }
        return c;
    }

    public void RemoveCharacter(Character character)
    {
        if (character == null) return;
        _characters.Remove(character);
        character.SetFollowTarget(null);
        Destroy(character.gameObject);
    }

    /// <summary>동료들을 center 주위에 재배치. center(플레이어)는 제외. NavMesh 샘플링 사용.</summary>
    public void RepositionCompanionsAround(Transform center)
    {
        if (center == null) return;
        var basePos = center.position;
        int companionIndex = 0;
        foreach (var c in _characters)
        {
            if (c == null || c.transform == center) continue;
            companionIndex++;
            var offset = GetSpawnOffset(companionIndex);
            var nearPos = basePos + offset;
            if (NavMesh.SamplePosition(nearPos, out var hit, _spawnRadius * 2f, NavMesh.AllAreas))
                c.Teleport(hit.position);
            else
                c.Teleport(nearPos);
        }
    }
}
