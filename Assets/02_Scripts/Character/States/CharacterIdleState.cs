using UnityEngine;

public class CharacterIdleState : CharacterStateBase
{
    public override bool CanMove => true;
    public CharacterIdleState(CharacterStateMachine machine, Character character) : base(machine, character) { }

    public override void Enter()
    {
        Character?.StopMovement();
    }

    public override void Update() { }

    public override void Exit() { }
}
