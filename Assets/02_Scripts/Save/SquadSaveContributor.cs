using UnityEngine;

/// <summary>
/// 분대 세이브/로드 기여. PlaySaveCoordinator.Initialize에서 주입.
/// </summary>
public class SquadSaveContributor : SaveContributorBehaviour
{
    public override int SaveOrder => 0;

    private SquadController _squadController;

    public void Initialize(SquadController squadController)
    {
        _squadController = squadController;
    }

    public override void Gather(SaveData data)
    {
        if (data?.squad == null) return;
        if (_squadController == null) return;

        data.squad.members.Clear();
        data.squad.currentPlayerId = "";
        data.squad.playerPosition = default;
        data.squad.playerRotationY = 0f;

        var current = _squadController.PlayerCharacter;
        if (current != null)
        {
            data.squad.playerPosition = current.transform.position;
            data.squad.playerRotationY = current.transform.eulerAngles.y;
            if (current.Model?.Data != null)
            {
                var id = current.Model.Data.Id;
                data.squad.currentPlayerId = !string.IsNullOrEmpty(id) ? id : current.Model.Data.displayName;
            }
        }

        var slots = _squadController.Characters;
        for (int slot = 0; slot < Squad.SlotCount; slot++)
        {
            var c = slot < slots.Count ? slots[slot] : null;
            if (c == null || c.Model?.Data == null) continue;
            var m = new CharacterMemberData();
            var id = c.Model.Data.Id;
            m.id = !string.IsNullOrEmpty(id) ? id : c.Model.Data.displayName;
            m.level = c.Model.Level;
            m.currentHp = c.Model.CurrentHp;
            m.slotIndex = slot;
            data.squad.members.Add(m);
        }
    }

    public override void Apply(SaveData data)
    {
        if (data?.squad == null) return;
        if (_squadController == null) return;

        // SquadController가 세이브 데이터 전체를 적용하도록 위임.
        _squadController.ApplySaveData(data.squad);
    }
}
