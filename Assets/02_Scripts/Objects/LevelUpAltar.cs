using UnityEngine;

/// <summary>
/// 재료를 제출해 캐릭터 레벨을 올리는 상호작용 오브젝트.
/// </summary>
public class LevelUpAltar : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemData _requiredItem;

    private Inventory _inventory;
    private SquadController _squadController;

    private void Awake()
    {
        _inventory = FindFirstObjectByType<Inventory>();
        _squadController = FindFirstObjectByType<SquadController>();
    }

    public string GetInteractText()
    {
        var c = _squadController != null ? _squadController.PlayerCharacter : null;
        var model = c != null ? c.Model : null;
        if (model == null || _requiredItem == null) return "제단";

        int maxLevel = model.Data != null && model.Data.maxLevel > 0 ? model.Data.maxLevel : int.MaxValue;
        if (model.Level >= maxLevel) return $"제단 (Lv {model.Level}) (최대)";

        int need = model.Level;
        int have = _inventory != null ? _inventory.GetTotalCount(_requiredItem.Id) : 0;
        return $"제단 (Lv {model.Level} → {model.Level + 1}) ({_requiredItem.ItemName} {have}/{need})";
    }

    public void Interact(IInteractReceiver receiver)
    {
        var character = receiver as Character;
        if (character == null) return;
        if (character.Model == null) return;
        if (_requiredItem == null) return;

        if (_inventory == null)
            _inventory = FindFirstObjectByType<Inventory>();
        if (_inventory == null) return;

        int maxLevel = character.Model.Data != null && character.Model.Data.maxLevel > 0 ? character.Model.Data.maxLevel : int.MaxValue;
        if (character.Model.Level >= maxLevel) return;

        int need = character.Model.Level;
        int have = _inventory.GetTotalCount(_requiredItem.Id);
        if (have < need) return;

        if (!_inventory.RemoveItem(_requiredItem.Id, need)) return;

        character.Model.TryLevelUp();
    }
}

