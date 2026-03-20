using System;
using UnityEngine;

/// <summary>
/// Enemy 런타임 데이터. EnemyData 기반으로 현재 HP 등 상태 보관.
/// 전투 시 IDamageable/IAttackPowerSource로 데미지 주고받기에 사용.
/// </summary>
public class EnemyModel : MonoBehaviour, IDamageable, IAttackPowerSource
{
    [SerializeField] private EnemyData _data;

    private int _currentHp;

    public EnemyData Data => _data;
    public int CurrentHp => _currentHp;
    public int MaxHp => _data != null ? _data.maxHp : 50;
    public int AttackPower => _data != null ? _data.attackPower : 5;
    public int Defense => _data != null ? _data.defense : 0;

    public event Action<int, int> OnHpChanged;
    /// <summary>사망 시 (HP 0 이하로 떨어질 때) 한 번 발행.</summary>
    public event Action OnDeath;
    /// <summary>데미지 입었을 때. (공격자 Transform, null 가능). 구독자가 전투 진입 등 판단.</summary>
    public event Action<Transform> OnDamaged;

    /// <summary>런타임에 DM에서 가져온 데이터 적용 (풀 재사용 시).</summary>
    public void ApplyData(EnemyData data)
    {
        _data = data;
    }

    public void Initialize()
    {
        _currentHp = MaxHp;
        OnHpChanged?.Invoke(_currentHp, MaxHp);
    }

    public void TakeDamage(int amount, Transform attacker = null)
    {
        if (amount <= 0) return;
        bool wasAlive = _currentHp > 0;
        int reduced = Mathf.Max(0, amount - Defense);
        _currentHp = Mathf.Max(0, _currentHp - reduced);
        Debug.Log($"[EnemyModel] {gameObject.name} TakeDamage {amount} (방어후 {reduced}) → HP {_currentHp}/{MaxHp}");
        OnHpChanged?.Invoke(_currentHp, MaxHp);
        OnDamaged?.Invoke(attacker);
        if (reduced > 0)
        {
            var hitPos = transform.position + Vector3.up * 1.5f;
            PlaySceneEventHub.OnDamageDealt?.Invoke(reduced, this, hitPos, attacker);
        }
        if (wasAlive && _currentHp <= 0)
            OnDeath?.Invoke();
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        _currentHp = Mathf.Min(MaxHp, _currentHp + amount);
        OnHpChanged?.Invoke(_currentHp, MaxHp);
    }

    /// <summary>사망 여부. 체력바 숨김 등에 사용.</summary>
    public bool IsDead => _currentHp <= 0;

    // --- AI 유효값 (Data 기반. 버프 등은 여기서 적용 예정) ---
    public float PatrolSpeed => _data != null ? _data.patrolSpeed : 1.5f;
    public float PatrolRadius => _data != null ? _data.patrolRadius : 5f;
    public float ArriveThreshold => _data != null ? _data.arriveThreshold : 0.5f;
    public float PatrolWalkDurationMin => _data != null ? _data.patrolWalkDurationMin : 2f;
    public float PatrolWalkDurationMax => _data != null ? _data.patrolWalkDurationMax : 3f;
    public float PatrolIdleDuration => _data != null ? _data.patrolIdleDuration : 1f;
    public float ChaseSpeed => _data != null ? _data.chaseSpeed : 4f;
    public float DetectionRadius => _data != null ? _data.detectionRadius : 10f;
    public float AggroThreshold => _data != null ? _data.aggroThreshold : 100f;
    public float AggroLoseDistance => _data != null ? _data.aggroLoseDistance : 10f;
    public float AggroAt1m => _data != null ? _data.aggroAt1m : 50f;
    public float AggroAt3m => _data != null ? _data.aggroAt3m : 30f;
    public float DetectInterval => _data != null ? _data.detectInterval : 0.5f;
    public float TargetReevalInterval => _data != null ? _data.targetReevalInterval : 1.5f;
    public float AttackRadius => _data != null ? _data.attackRadius : 2f;
    public float ChaseLoseRadius => _data != null ? _data.chaseLoseRadius : 15f;
    public float AttackDuration => _data != null ? _data.attackDuration : 0.6f;
}
