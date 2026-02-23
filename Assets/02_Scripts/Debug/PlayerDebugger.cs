using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어·캐릭터 디버그·검증. Hierarchy의 Debuggers 등에 붙이고, 인스펙터에서 SquadController 참조 할당.
/// </summary>
public class PlayerDebugger : MonoBehaviour
{
    [SerializeField] [Tooltip("비어 있으면 씬에서 FindObjectOfType으로 탐색")]
    private SquadController _squadController;
    [SerializeField] [Tooltip("텔레포트 시 도착 위치. 씬에 빈 오브젝트 등을 놓고 지정")]
    private Transform _teleportTarget;

    /// <summary>디버거용 플레이어 캐릭터. 스탯 표시·체력 회복 등에 사용.</summary>
    public Character PlayerCharacter => _squadController?.PlayerCharacter;

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

        // 플레이어 조종 시 필요한 것들 (동료는 일부 없을 수 있음)
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

    /// <summary>플레이어를 _teleportTarget 위치로 텔레포트. 땅에 박혔을 때 등 디버그용.</summary>
    [ContextMenu("텔레포트: 지정 위치로 이동")]
    public void TeleportToTarget()
    {
        if (_teleportTarget == null)
        {
            Debug.LogWarning("[PlayerDebugger] Teleport Target이 지정되지 않았습니다. 인스펙터에서 Transform을 할당하세요.");
            return;
        }
        if (_squadController == null)
        {
            _squadController = FindAnyObjectByType<SquadController>();
            if (_squadController == null)
            {
                Debug.LogWarning("[PlayerDebugger] SquadController를 찾을 수 없습니다.");
                return;
            }
        }
        _squadController.TeleportPlayer(_teleportTarget);
        Debug.Log($"[PlayerDebugger] 플레이어를 {_teleportTarget.position}로 텔레포트했습니다.");
    }

    [ContextMenu("Validate Setup (Log)")]
    private void ValidateAndLog()
    {
        if (ValidateSetup(out var issues))
        {
            Debug.Log("[PlayerDebugger] 검증 통과: 부품 구성 정상");
        }
        else
        {
            foreach (var msg in issues)
                Debug.LogWarning($"[PlayerDebugger] {msg}");
        }
    }
}
