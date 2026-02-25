using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 분대 디버그·검증. Hierarchy의 Debuggers 등에 붙이고, 인스펙터에서 SquadController 참조 할당.
/// 텔레포트, 체력 회복, 동료 소환/제거 등.
/// </summary>
public class SquadDebugger : MonoBehaviour
{
    [SerializeField] [Tooltip("비어 있으면 씬에서 FindObjectOfType으로 탐색")]
    private SquadController _squadController;
    [SerializeField] [Tooltip("텔레포트 시 도착 위치. 씬에 빈 오브젝트 등을 놓고 지정")]
    private Transform _teleportTarget;
    [SerializeField] [Tooltip("동료 소환용. 인스펙터에서 CharacterData 할당")]
    private List<CharacterData> _spawnableCharacters = new List<CharacterData>();

    /// <summary>디버거용 플레이어 캐릭터. 스탯 표시·체력 회복 등에 사용.</summary>
    public Character PlayerCharacter => _squadController?.PlayerCharacter;
    public SquadController SquadControllerRef => _squadController;
    public IReadOnlyList<CharacterData> SpawnableCharacters => _spawnableCharacters;

    private void OnValidate()
    {
        if (_squadController == null)
            _squadController = FindAnyObjectByType<SquadController>();
    }

    /// <summary>SquadController·Character 부품 구성을 검증하고 누락 항목을 로그.</summary>
    public bool ValidateSetup(out List<string> issues)
    {
        issues = new List<string>();

        if (_squadController == null)
        {
            _squadController = FindAnyObjectByType<SquadController>();
            if (_squadController == null)
            {
                issues.Add("SquadController를 찾을 수 없습니다. 씬에 SquadController가 있는지 확인하세요.");
                return false;
            }
        }

        var c = _squadController.PlayerCharacter;
        if (c == null)
            issues.Add("SquadController에 PlayerCharacter가 없습니다. 분대 스폰이 완료되었는지 확인하세요.");

        if (c == null)
            return false;

        if (c.Model == null) issues.Add("Character: Model 없음");
        if (c.Animator == null) issues.Add("Character: Animator(CharacterAnimator) 없음");
        if (c.StateMachine == null) issues.Add("Character: StateMachine 없음");

        bool hasMover = c.Mover != null;
        bool hasFollowMover = c.GetComponent<CharacterFollowMover>() != null;
        if (!hasMover && !hasFollowMover)
            issues.Add("Character: Mover 또는 FollowMover 중 하나 필요");

        if (c.Mover != null && c.GetComponent<CharacterController>() == null)
            issues.Add("Character: Mover 사용 시 CharacterController 필요");

        if (c.GetComponent<CharacterFollowMover>() != null && c.GetComponent<UnityEngine.AI.NavMeshAgent>() == null)
            issues.Add("Character: FollowMover 사용 시 NavMeshAgent 필요");

        return issues.Count == 0;
    }

    /// <summary>플레이어를 _teleportTarget 위치로 텔레포트.</summary>
    [ContextMenu("텔레포트: 지정 위치로 이동")]
    public void TeleportToTarget()
    {
        if (_teleportTarget == null)
        {
            Debug.LogWarning("[SquadDebugger] Teleport Target이 지정되지 않았습니다.");
            return;
        }
        var sc = GetSquadController();
        if (sc == null) return;
        sc.TeleportPlayer(_teleportTarget);
        Debug.Log("[SquadDebugger] 플레이어를 텔레포트했습니다.");
    }

    /// <summary>동료 소환. characterData를 플레이어 주변에 스폰.</summary>
    public void SpawnCompanion(CharacterData characterData)
    {
        var sc = GetSquadController();
        if (sc == null || characterData == null) return;
        var player = sc.PlayerCharacter;
        if (player == null)
        {
            Debug.LogWarning("[SquadDebugger] PlayerCharacter가 없습니다.");
            return;
        }

        var nearPos = player.transform.position + player.transform.forward * 2f;
        var c = sc.SpawnCharacter(characterData, nearPos, player.transform);
        if (c != null)
            Debug.Log($"[SquadDebugger] 동료 소환: {characterData.displayName}");
        else
            Debug.LogWarning("[SquadDebugger] 동료 소환 실패.");
    }

    /// <summary>지정 캐릭터를 분대에서 제거.</summary>
    public void RemoveCompanion(Character character)
    {
        var sc = GetSquadController();
        if (sc == null || character == null) return;
        if (character == sc.PlayerCharacter)
        {
            Debug.LogWarning("[SquadDebugger] 조종 중인 캐릭터는 제거할 수 없습니다. 먼저 SquadSwap으로 전환하세요.");
            return;
        }
        sc.RemoveCharacter(character);
        Debug.Log($"[SquadDebugger] 동료 제거: {character.name}");
    }

    [ContextMenu("Validate Setup (Log)")]
    private void ValidateAndLog()
    {
        if (ValidateSetup(out var issues))
            Debug.Log("[SquadDebugger] 검증 통과: 부품 구성 정상");
        else
            foreach (var msg in issues)
                Debug.LogWarning($"[SquadDebugger] {msg}");
    }

    private SquadController GetSquadController()
    {
        if (_squadController == null)
            _squadController = FindAnyObjectByType<SquadController>();
        if (_squadController == null)
            Debug.LogWarning("[SquadDebugger] SquadController를 찾을 수 없습니다.");
        return _squadController;
    }
}
