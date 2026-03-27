using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>캐릭터 상태 식별자. 외부/내부에서 CurrentState 비교 시 사용.</summary>
public enum CharacterState
{
    Idle,
    Move,
    Attack,
    Dead,
}

/// <summary>
/// 캐릭터 상태머신. Idle ↔ Move ↔ Attack ↔ Dead.
/// 전환 규칙:
/// - Idle↔Move: 플레이어는 SetMoveDirection+RequestMove. 입력 없으면 MoveState.IsComplete→Idle. 동료는 AIBrain.RequestIdle.
/// - Attack: RequestAttack 진입. Attacker.IsAttackEnded 시 IsComplete→Idle
/// - Dead: Model.OnDeath 진입. RequestRespawn 후 Idle
/// </summary>
public class CharacterStateMachine : MonoBehaviour
{
    private Character _character;
    private Dictionary<CharacterState, CharacterStateBase> _states;

    [SerializeField, Tooltip("런타임 현재 상태 확인용")]
    private CharacterState _currentState = CharacterState.Idle;

    public CharacterState CurrentState => _currentState;
    public bool CanMove => GetState(_currentState)?.CanMove ?? false;

    public event Action<CharacterState, CharacterState> OnStateChanged;

    public void Initialize(Character character)
    {
        _character = character;
        _states = new Dictionary<CharacterState, CharacterStateBase>
        {
            [CharacterState.Idle] = new CharacterIdleState(this, _character),
            [CharacterState.Move] = new CharacterMoveState(this, _character),
            [CharacterState.Attack] = new CharacterAttackState(this, _character),
            [CharacterState.Dead] = new CharacterDeadState(this, _character),
        };

        _currentState = CharacterState.Idle;
        GetState(_currentState)?.Enter();

        if (_character.Model != null)
            _character.Model.OnDeath += HandleDeath;
    }

    private void OnDestroy()
    {
        if (_character?.Model != null)
            _character.Model.OnDeath -= HandleDeath;
    }

    private CharacterStateBase GetState(CharacterState key) =>
        _states != null && _states.TryGetValue(key, out CharacterStateBase s) ? s : null;

    private void HandleDeath()
    {
        if (_currentState != CharacterState.Dead)
            ChangeState(CharacterState.Dead);
    }

    private void Update()
    {
        GetState(_currentState)?.Update();
        if (GetState(_currentState)?.IsComplete == true)
            ChangeState(CharacterState.Idle);
    }

    public void RequestIdle()
    {
        if (_currentState == CharacterState.Dead) return;
        if (_currentState == CharacterState.Attack) return; // Attack은 애니 종료·Fallback으로만 종료
        ChangeState(CharacterState.Idle);
    }

    /// <summary>부활 시 Dead → Idle 전환. RespawnController 호출.</summary>
    public void RequestRespawn()
    {
        if (_currentState != CharacterState.Dead) return;
        ChangeState(CharacterState.Idle);
    }

    public void RequestMove()
    {
        if (_currentState == CharacterState.Attack || _currentState == CharacterState.Dead) return;
        if (_currentState == CharacterState.Move) return;
        ChangeState(CharacterState.Move);
    }

    public bool RequestAttack()
    {
        if (_currentState != CharacterState.Idle && _currentState != CharacterState.Move)
            return false;
        ChangeState(CharacterState.Attack);
        return true;
    }

    private void ChangeState(CharacterState newState)
    {
        if (_currentState == newState) return;

        CharacterState previous = _currentState;
        GetState(_currentState)?.Exit();
        _currentState = newState;
        GetState(_currentState)?.Enter();
        OnStateChanged?.Invoke(previous, _currentState);
    }
}
