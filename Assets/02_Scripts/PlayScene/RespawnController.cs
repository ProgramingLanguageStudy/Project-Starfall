using UnityEngine;

/// <summary>
/// 부활 요청 시 마을 포탈 위치로 이동·체력 회복·Idle 전환.
/// </summary>
public class RespawnController : MonoBehaviour
{
    [SerializeField] [Tooltip("부활 위치. 마을 포탈 등")]
    private Portal _respawnPortal;

    private void OnEnable()
    {
        GameEvents.OnCharacterRespawnRequested += HandleCharacterRespawnRequested;
    }

    private void OnDisable()
    {
        GameEvents.OnCharacterRespawnRequested -= HandleCharacterRespawnRequested;
    }

    private void HandleCharacterRespawnRequested(Character character)
    {
        if (character == null) return;

        if (_respawnPortal != null)
            character.Teleport(_respawnPortal.ArrivalPosition);
        character.Model?.Heal(character.Model.MaxHp);
        character.Animator?.ResetToIdle();
        character.StateMachine?.RequestIdle();
    }
}
