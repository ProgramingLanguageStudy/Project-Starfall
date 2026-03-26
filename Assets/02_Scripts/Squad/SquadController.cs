using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 분대 조율. Squad(상태) + SquadCharacterFactory(생성) 연동.
/// 역할 배정, 입력 위임, 포메이션 등.
/// </summary>
public class SquadController : MonoBehaviour
{
    #region Inspector

    [Header("참조")]
    [SerializeField] [Tooltip("스폰된 분대 부모. 비면 this")]
    private Transform _squadRoot;
    [SerializeField] [Tooltip("세이브 없을 때 기본 스폰 위치")]
    private Transform _spawnPoint;

    [Header("스폰")]
    [SerializeField] private float _spawnRadius = 2f;

    #endregion

    #region Fields

    private Squad _squad = new Squad();
    private SquadCharacterFactory _factory;
    private CombatController _combatController;
    private PlayScene _playScene; // 카메라 순간이동 처리용

    #endregion

    #region Properties

    public Squad Squad => _squad;
    /// <summary>현재 슬롯 배치 (0~3, 빈 슬롯 null). 외부는 읽기 전용으로만 사용.</summary>
    public IReadOnlyList<Character> Characters => _squad.GetSlots();
    public Character PlayerCharacter => _squad.Player;
    public bool CanMove { get; set; } = true;

    public Character DefaultPlayer
    {
        get
        {
            foreach (var c in _squad.GetSlots())
            {
                if (c != null) return c;
            }
            return null;
        }
    }
    public Transform PlayerTransform => PlayerCharacter?.transform;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        _factory = GetComponent<SquadCharacterFactory>();
        if (_factory == null)
        {
            Debug.LogError($"[SquadController] {gameObject.name}: SquadCharacterFactory를 찾을 수 없습니다.");
        }
        
        // PlayScene 참조 설정
        _playScene = FindFirstObjectByType<PlayScene>();
    }

    #endregion

    #region Events

    public event Action<Character> OnPlayerChanged;
    /// <summary>동료 추가/제거 시 발생. 인자로 슬롯 기준 길이 4 (빈 슬롯 null).</summary>
    public event Action<IReadOnlyList<Character>> OnMembersChanged;

    #endregion

    #region Public API

    /// <summary>분대 컨트롤러 초기화. 팩토리·이벤트·의존성 주입. PlayScene에서 호출.</summary>
    public void Initialize(CombatController combatController = null)
    {
        _combatController = combatController;

        _squad.OnPlayerChanged += c => OnPlayerChanged?.Invoke(c);
        _squad.OnMembersChanged += slots => OnMembersChanged?.Invoke(slots);
    }

    /// <summary>세이브 데이터 기반으로 분대 슬롯·플레이어·상태를 복원.</summary>
    public void ApplySaveData(SquadSaveData squadSaveData)
    {
        var dm = GameManager.Instance?.DataManager;
        if (dm == null || squadSaveData == null) return;

        var root = _squadRoot != null ? _squadRoot : transform;

        // 세이브된 플레이어 위치가 있으면 우선 사용, 아니면 스폰 포인트/자기 위치.
        var basePos = squadSaveData.playerPosition != Vector3.zero
            ? squadSaveData.playerPosition
            : (_spawnPoint != null ? _spawnPoint.position : transform.position);

        var slots = new Character[Squad.SlotCount];
        Character targetPlayer = null;
        foreach (var m in squadSaveData.members)
        {
            if (string.IsNullOrEmpty(m.id)) continue;
            if (m.slotIndex < 0 || m.slotIndex >= Squad.SlotCount) continue;

            var data = dm.Get<CharacterData>(m.id);
            if (data == null) continue;

            var pos = basePos + GetSpawnOffset(m.slotIndex);
            var c = CreateCharacter(m.id, pos, root);
            if (c == null) continue;

            c.Model?.SetLevelForLoad(m.level);
            c.Model?.SetCurrentHpForLoad(m.currentHp);
            slots[m.slotIndex] = c;

            if (!string.IsNullOrEmpty(squadSaveData.currentPlayerId) &&
                (m.id == squadSaveData.currentPlayerId || c.Model?.Data?.displayName == squadSaveData.currentPlayerId))
                targetPlayer = c;
        }

        _squad.SetSlots(slots, targetPlayer);
        // 슬롯·플레이어가 정해진 뒤, Squad가 플레이어 교체 및 역할 재배치를 담당
        _squad.SetPlayerCharacter(_squad.Player);

        // 플레이어 회전·동료 재배치
        var playerChar = _squad.Player ?? DefaultPlayer;
        if (playerChar != null)
        {
            playerChar.transform.eulerAngles = new Vector3(0f, squadSaveData.playerRotationY, 0f);
            RepositionCompanionsAround(playerChar.transform);
        }
    }

    private Character CreateCharacter(string characterId, Vector3 position, Transform parent)
    {
        if (_factory == null)
        {
            Debug.LogError($"[SquadController] Factory is null! characterId={characterId}");
            return null;
        }
        
        return _factory.Create(characterId, position, parent, _combatController, _squad, _spawnRadius);
    }

    public void SetPlayerCharacter(Character character)
    {
        if (character == null || character == _squad.Player) return;
        if (_squad.GetSlotOf(character) < 0) return;

        // 실제 플레이어 필드·이벤트·역할 전환은 Squad가 전담
        _squad.SetPlayerCharacter(character);
    }

    public void RequestInteract() => PlayerCharacter?.Interactor?.TryInteract();
    public void RequestAttack() => PlayerCharacter?.RequestAttack();

    /// <summary>
    /// 스쿼드 스왑. 현재 플레이어가 있는 슬롯 기준으로,
    /// 다음 슬롯(시계 방향)에서 첫 번째 non-null 캐릭터를 찾아 플레이어로 전환한다.
    /// </summary>
    public bool RequestSquadSwap()
    {
        var current = PlayerCharacter;
        if (current == null) return false;

        var slots = _squad.GetSlots();
        int currentSlot = _squad.GetSlotOf(current);
        if (currentSlot < 0) return false;

        for (int i = 1; i < Squad.SlotCount; i++)
        {
            int s = (currentSlot + i) % Squad.SlotCount;
            var candidate = slots[s];
            if (candidate != null && candidate != current)
            {
                SetPlayerCharacter(candidate);
                return true;
            }
        }

        return false;
    }

    public void TeleportToDefaultPoint()
    {
        var pos = _spawnPoint != null ? _spawnPoint.position : transform.position;
        TeleportPlayer(pos);
    }

    public void TeleportPlayer(Vector3 worldPosition)
    {
        if (PlayerCharacter == null) return;
        
        // 카메라 즉시 이동 처리
        _playScene?.HandlePlayerTeleport(worldPosition);
        
        PlayerCharacter.Teleport(worldPosition);
        RepositionCompanionsAround(PlayerCharacter.transform);
    }

    public Character AddCompanion(string characterId)
    {
        if (string.IsNullOrEmpty(characterId)) return null;
        if (_factory == null) return null;

        var followTarget = PlayerCharacter != null ? PlayerCharacter.transform : transform;
        var root = _squadRoot != null ? _squadRoot : transform;
        // 현재 분대원 수(빈 슬롯 제외)를 기준으로 스폰 위치 계산
        int memberCount = 0;
        foreach (var member in _squad.GetSlots())
        {
            if (member != null) memberCount++;
        }
        var pos = followTarget.position + GetSpawnOffset(memberCount);

        var created = _factory.Create(characterId, pos, root, _combatController, _squad, _spawnRadius);
        if (created == null) return null;

        _squad.AddMember(created);
        created.SetAsCompanion(followTarget);
        return created;
    }

    public void RemoveCharacter(Character character)
    {
        if (character == null) return;
        _squad.RemoveMember(character);
        _factory?.Destroy(character);
    }

    public void RepositionCompanionsAround(Transform center)
    {
        if (center == null) return;
        var basePos = center.position;
        int companionIndex = 0;

        foreach (var c in _squad.GetSlots())
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

    #endregion

    #region Internal Helpers

    private Vector3 GetSpawnOffset(int index)
    {
        if (index <= 0) return Vector3.zero;
        float angle = (360f / 4f) * index * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(angle) * _spawnRadius, 0f, Mathf.Sin(angle) * _spawnRadius);
    }

    #endregion
}
