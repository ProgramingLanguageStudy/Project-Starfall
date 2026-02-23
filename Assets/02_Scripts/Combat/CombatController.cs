using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 상태 On/Off 및 전투중인 적 리스트 관리.
/// Enemy가 Chase/Attack 진입 시 Register, Patrol/Idle/Dead 시 Unregister.
/// CompanionAI는 IsInCombat·EnemiesInCombat로 타겟 선택.
/// </summary>
public class CombatController : MonoBehaviour
{
    private readonly List<Enemy> _enemiesInCombat = new List<Enemy>();

    public bool IsInCombat => _enemiesInCombat.Count > 0;
    public IReadOnlyList<Enemy> EnemiesInCombat => _enemiesInCombat;

    public event Action<bool> OnCombatStateChanged;

    /// <summary>적이 전투 진입(Chase/Attack). 리스트에 추가, 0→1이면 SetCombatOn.</summary>
    public void RegisterInCombat(Enemy enemy)
    {
        if (enemy == null || _enemiesInCombat.Contains(enemy)) return;
        bool wasEmpty = _enemiesInCombat.Count == 0;
        _enemiesInCombat.Add(enemy);
        if (wasEmpty)
        {
            OnCombatStateChanged?.Invoke(true);
        }
    }

    /// <summary>적이 전투 이탈(Patrol/Idle/Dead). 리스트에서 제거, 1→0이면 SetCombatOff.</summary>
    public void UnregisterFromCombat(Enemy enemy)
    {
        if (enemy == null) return;
        bool hadAny = _enemiesInCombat.Count > 0;
        _enemiesInCombat.Remove(enemy);
        if (hadAny && _enemiesInCombat.Count == 0)
        {
            OnCombatStateChanged?.Invoke(false);
        }
    }
}
