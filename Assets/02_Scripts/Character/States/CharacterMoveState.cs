using UnityEngine;

public class CharacterMoveState : CharacterStateBase
{
    public override bool CanMove => true;

    public CharacterMoveState(CharacterStateMachine machine, Character character) : base(machine, character) { }

    public override void Enter() { }

    public override void Update()
    {
        Character?.ApplyMovement();
    }

    public override void Exit() { }
}
