using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 캐릭터 런타임 데이터. CharacterData → CharacterBaseStats, 보정(StatModifier) 적용 후 최종 스탯 노출.
/// Mover/Attacker 등이 스탯 참조 시 사용. IItemUser로 아이템·버프 호환.
/// </summary>
public class CharacterModel : MonoBehaviour, IDamageable, IAttackPowerSource, IItemUser
{
    private CharacterData _data;

    private CharacterBaseStats _baseStats;
    private StatModifier _modifier;
    private StatModifier _levelModifier;
    private int _level = 1;
    private int _currentHp;
    [SerializeField] private float _currentMoveSpeed;

    public CharacterData Data => _data;

    public CharacterBaseStats BaseStats => _baseStats;
    public StatModifier Modifier => _modifier;
    public int Level => _level;

    public int CurrentHp => _currentHp;
    public float CurrentMoveSpeed => _currentMoveSpeed;
    public void SetCurrentMoveSpeed(float speed)
    {
        _currentMoveSpeed = speed;
    }
    public int MaxHp => Mathf.Max(1, _baseStats.maxHp + _modifier.maxHp);
    public bool IsDead => _currentHp <= 0;

    public float MoveSpeed => Mathf.Max(0f, _baseStats.moveSpeed + _modifier.moveSpeed);
    public int AttackPower => Mathf.Max(0, _baseStats.attackPower + _modifier.attackPower);
    public float AttackSpeed => Mathf.Max(0f, _baseStats.attackSpeed + _modifier.attackSpeed);
    public int Defense => Mathf.Max(0, _baseStats.defense + _modifier.defense);
    
    public bool IsMaxLevel => _data != null && (_data.maxLevel <= 0 || _level >= _data.maxLevel);

    /// <summary>AI 따라가기: 목표 거리</summary>
    public float FollowDistance => _data != null ? _data.followDistance : 3f;
    public float StopDistance => _data != null ? _data.stopDistance : 1.5f;
    public float CatchUpSpeed => _data != null ? _data.catchUpSpeed : 1.2f;

    public event Action<int, int> OnHpChanged;
    /// <summary>사망 시 (HP 0 이하로 떨어질 때) 한 번 발행.</summary>
    public event Action OnDeath;

    public void Initialize()
    {
        _baseStats = CharacterBaseStats.From(_data);
        _modifier = default;
        _level = 1;
        _levelModifier = ComputeLevelModifier(_level);
        _modifier = _modifier.Add(_levelModifier);
        _currentHp = MaxHp;
        OnHpChanged?.Invoke(_currentHp, MaxHp);
    }

    public void Initialize(CharacterData data)
    {
        _data = data;
        _baseStats = CharacterBaseStats.From(_data);
        _modifier = default;
        _level = 1;
        _levelModifier = ComputeLevelModifier(_level);
        _modifier = _modifier.Add(_levelModifier);
        _currentHp = MaxHp;
        OnHpChanged?.Invoke(_currentHp, MaxHp);
    }

    public void AddModifier(StatModifier delta)
    {
        _modifier = _modifier.Add(delta);
        _currentHp = Mathf.Clamp(_currentHp, 0, MaxHp);
        OnHpChanged?.Invoke(_currentHp, MaxHp);
    }

    public void RemoveModifier(StatModifier delta)
    {
        _modifier = _modifier.Subtract(delta);
        _currentHp = Mathf.Clamp(_currentHp, 0, MaxHp);
        OnHpChanged?.Invoke(_currentHp, MaxHp);
    }

    public void TakeDamage(int amount, Transform attacker = null)
    {
        if (amount <= 0) return;
        bool wasAlive = _currentHp > 0;
        int reduced = Mathf.Max(0, amount - Defense);
        _currentHp = Mathf.Max(0, _currentHp - reduced);
        OnHpChanged?.Invoke(_currentHp, MaxHp);
        if (reduced > 0)
        {
            Vector3 hitPos = transform.position + Vector3.up * 1.5f;
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

    public void ApplyBuff(StatModifier modifier, float durationSeconds)
    {
        if (durationSeconds <= 0f) return;
        
        // GameManager를 통한 중앙 집중식 버프 관리
        if (GameManager.Instance != null && GameManager.Instance.BuffManager != null)
        {
            GameManager.Instance.BuffManager.AddBuff(this, modifier, durationSeconds);
        }
        else
        {
            // 폴백: GameManager가 없을 경우 (테스트 환경 등)를 대비해 직접 적용 로직을 두거나 로그를 남깁니다.
            Debug.LogWarning("[CharacterModel] BuffManager not found via GameManager. Buff not applied.");
        }
    }

    public void SetCurrentHpForLoad(int value)
    {
        _currentHp = Mathf.Clamp(value, 0, MaxHp);
        OnHpChanged?.Invoke(_currentHp, MaxHp);
    }

    public void SetLevelForLoad(int value)
    {
        SetLevelInternal(value);
        _currentHp = Mathf.Clamp(_currentHp, 0, MaxHp);
        OnHpChanged?.Invoke(_currentHp, MaxHp);
    }

    public bool TryLevelUp()
    {
        int maxLevel = _data != null && _data.maxLevel > 0 ? _data.maxLevel : int.MaxValue;
        if (_level >= maxLevel) return false;
        SetLevelInternal(_level + 1);
        _currentHp = Mathf.Clamp(_currentHp, 0, MaxHp);
        OnHpChanged?.Invoke(_currentHp, MaxHp);
        return true;
    }

    private void SetLevelInternal(int value)
    {
        int maxLevel = _data != null && _data.maxLevel > 0 ? _data.maxLevel : int.MaxValue;
        int clamped = Mathf.Clamp(value, 1, maxLevel);
        if (clamped == _level) return;

        _modifier = _modifier.Subtract(_levelModifier);
        _level = clamped;
        _levelModifier = ComputeLevelModifier(_level);
        _modifier = _modifier.Add(_levelModifier);
    }

    private StatModifier ComputeLevelModifier(int level)
    {
        if (_data == null) return default;
        int steps = Mathf.Max(0, level - 1);
        return new StatModifier
        {
            maxHp = _data.maxHpPerLevel * steps,
            attackPower = _data.attackPowerPerLevel * steps
        };
    }
}
