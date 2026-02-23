using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적(Enemy)의 어그로 관리. Data 기반 수치. 거리별 누적, 이탈 시 리셋.
/// </summary>
public class EnemyAggro : MonoBehaviour
{
    private EnemyModel _model;
    private readonly Dictionary<Character, float> _aggroTable = new Dictionary<Character, float>();

    public void Initialize(EnemyModel model)
    {
        _model = model;
    }

    public float Threshold => _model != null ? _model.AggroThreshold : 100f;
    public float LoseDistance => _model != null ? _model.AggroLoseDistance : 10f;

    /// <summary>거리별 어그로 (1m→aggroAt1m, 3m→aggroAt3m, 그 사이 선형 보간).</summary>
    private float AggroFromDistance(float distance)
    {
        float at1 = _model != null ? _model.AggroAt1m : 50f;
        float at3 = _model != null ? _model.AggroAt3m : 30f;
        if (distance <= 1f) return at1;
        if (distance >= 3f) return at3;
        return Mathf.Lerp(at1, at3, (distance - 1f) / 2f);
    }

    public void AddAggro(Character target, float amount)
    {
        if (target == null) return;
        if (!_aggroTable.ContainsKey(target)) _aggroTable[target] = 0f;
        _aggroTable[target] = Mathf.Max(0f, _aggroTable[target] + amount);
    }

    public void AddAggroFromDistance(Character target, float distance)
    {
        AddAggro(target, AggroFromDistance(distance));
    }

    public void SetAggro(Character target, float value)
    {
        if (target == null) return;
        _aggroTable[target] = Mathf.Max(0f, value);
    }

    /// <summary>가장 높은 어그로 대상. 없으면 null.</summary>
    public Character GetHighestAggroTarget()
    {
        Character best = null;
        float bestVal = 0f;
        foreach (var kv in _aggroTable)
        {
            if (kv.Key == null || kv.Key.Model == null || kv.Key.Model.IsDead) continue;
            if (kv.Value > bestVal)
            {
                bestVal = kv.Value;
                best = kv.Key;
            }
        }
        return best;
    }

    public bool HasAnyAboveThreshold()
    {
        float th = Threshold;
        foreach (var kv in _aggroTable)
        {
            if (kv.Key != null && !kv.Key.Model.IsDead && kv.Value >= th)
                return true;
        }
        return false;
    }

    /// <summary>플레이어가 lose 거리 밖이면 리셋. true면 리셋됨.</summary>
    public bool TryResetIfPlayerOutOfRange(Vector3 myPos, Transform playerTransform)
    {
        if (playerTransform == null) return false;
        float dist = Vector3.Distance(myPos, playerTransform.position);
        float loseDist = _model != null ? _model.AggroLoseDistance : 10f;
        if (dist > loseDist)
        {
            ClearAll();
            return true;
        }
        return false;
    }

    public void ClearAll()
    {
        _aggroTable.Clear();
    }
}
