using UnityEngine;

public class CharacterAttackState : CharacterStateBase
{
    private float _timer;
    private const float FallbackDuration = 1.5f;

    public CharacterAttackState(CharacterStateMachine machine, Character character) : base(machine, character) { }

    public override void Enter()
    {
        _timer = 0f;
        Character?.StopMovement();
        Character?.Attacker?.Begin();
    }

    public override void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= FallbackDuration)
            Machine.RequestIdle();
    }

    public override void Exit()
    {
        Character?.Attacker?.End();
        Character?.Animator?.ResetToIdle();
    }
}
