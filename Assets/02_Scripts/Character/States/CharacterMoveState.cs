using UnityEngine;

public class CharacterMoveState : CharacterStateBase
{
    public override bool CanMove => true;

    /// <summary>플레이어: 입력 없으면 완료. 동료: AIBrain.RequestIdle로 종료.</summary>
    public override bool IsComplete => Character != null && Character.IsPlayer && !Character.HasMoveInput;

    public CharacterMoveState(CharacterStateMachine machine, Character character) : base(machine, character) { }

    public override void Enter() { }

    public override void Update()
    {
        if (Character == null) return;

        Character.ApplyMovement();

        var anim = Character.Animator;
        var model = Character.Model;
        if (anim == null)
        {
            Debug.LogError("[CharacterMoveState] Character.Animator is null.");
            return;
        }

        if (model == null)
        {
            Debug.LogError("[CharacterMoveState] Character.Model is null.");
            return;
        }

        anim.Move(model.CurrentMoveSpeed);
    }

    public override void Exit() { }
}
