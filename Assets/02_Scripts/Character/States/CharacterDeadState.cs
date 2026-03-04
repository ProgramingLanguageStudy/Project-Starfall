using UnityEngine;

/// <summary>
/// 죽음 상태. Character.Die() / Revive() 호출.
/// </summary>
public class CharacterDeadState : CharacterStateBase
{
    private float _deadTimer;
    private const float RespawnDelay = 2f;

    public CharacterDeadState(CharacterStateMachine machine, Character character) : base(machine, character) { }

    public override void Enter()
    {
        Character?.Die();
        _deadTimer = 0f;
    }

    public override void Update()
    {
        _deadTimer += Time.deltaTime;
        if (_deadTimer >= RespawnDelay && Character != null)
        {
            GameEvents.OnCharacterRespawnRequested?.Invoke(Character);
        }
    }

    public override void Exit()
    {
        Character?.Revive();
    }
}
