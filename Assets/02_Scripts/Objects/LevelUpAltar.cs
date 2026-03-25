using System;
using UnityEngine;

/// <summary>
/// 재료를 제출해 캐릭터 레벨을 올리는 상호작용 오브젝트.
/// </summary>
public class LevelUpAltar : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemData _requiredItem;

    public event Action<LevelUpAltar, Character> OnInteracted;

    public ItemData RequiredItem => _requiredItem;

    public string GetInteractText() => "제단";

    public void Interact(IInteractReceiver receiver)
    {
        var character = receiver as Character;
        if (character == null) return;
        if (character.Model == null) return;
        OnInteracted?.Invoke(this, character);
    }
}
