using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 팀. 멤버의 OnEnteringCombat 구독 → 전원 Chase. 풀 반환 시 <see cref="Enemy.OnReturnedToPool"/>로 멤버 제거 후 빈 팀 오브젝트 파괴.
/// </summary>
public class EnemyTeam : MonoBehaviour
{
    private CombatController _combatController;
    private readonly List<Enemy> _members = new List<Enemy>();

    public IReadOnlyList<Enemy> Members => _members;

    public void Initialize(CombatController combatController)
    {
        _combatController = combatController;
    }

    public void AddMember(Enemy enemy)
    {
        if (enemy == null || _members.Contains(enemy)) return;
        _members.Add(enemy);
        enemy.OnEnteringCombat += HandleMemberEnteringCombat;
        enemy.OnReturnedToPool += HandleMemberReturnedToPool;
    }

    private void HandleMemberReturnedToPool(Enemy enemy)
    {
        if (enemy == null) return;
        enemy.OnEnteringCombat -= HandleMemberEnteringCombat;
        enemy.OnReturnedToPool -= HandleMemberReturnedToPool;
        _members.Remove(enemy);

        if (_members.Count == 0)
            Destroy(gameObject);
    }

    private void HandleMemberEnteringCombat(Character triggerCharacter)
    {
        if (triggerCharacter == null) return;

        Transform target = Enemy.ResolveChaseTarget(triggerCharacter);
        Squad squad = triggerCharacter.Squad;

        foreach (Enemy m in _members)
        {
            if (m == null || m.Model == null || m.Model.IsDead) continue;
            m.SetChaseTargetAndSquad(target, squad);
            m.StateMachine.RequestChase();
        }
    }
}
