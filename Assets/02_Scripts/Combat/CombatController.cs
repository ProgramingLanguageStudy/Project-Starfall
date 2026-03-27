using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 상태 On/Off 및 전투중인 적 리스트 관리.
/// Enemy가 Chase/Attack 진입 시 Register, Patrol/Idle/Dead 시 Unregister.
/// 전투 상태의 진실의 원천. AIBrain 등에 주입되어 IsInCombat·EnemiesInCombat 제공.
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

    /// <summary>특정 위치에서 가장 가까운 적을 반환합니다.</summary>
    public Enemy GetNearestEnemy(Vector3 origin)
    {
        if (_enemiesInCombat.Count == 0) return null;

        Enemy nearest = null;
        float minDistanceSq = float.MaxValue;

        // 리스트를 돌며 거리의 제곱을 비교 (Distance보다 sqrMagnitude가 성능에 더 좋습니다)
        for (int i = 0; i < _enemiesInCombat.Count; i++)
        {
            Enemy enemy = _enemiesInCombat[i];

            // 유효성 및 사망 체크 (안전장치)
            if (enemy == null || enemy.Model == null || enemy.Model.IsDead) continue;

            float distSq = (enemy.transform.position - origin).sqrMagnitude;
            if (distSq < minDistanceSq)
            {
                minDistanceSq = distSq;
                nearest = enemy;
            }
        }

        return nearest;
    }
}
