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
                var id = current.Model.Data.characterId;
                data.squad.currentPlayerId = !string.IsNullOrEmpty(id) ? id : current.Model.Data.displayName;
            }
        }

        foreach (var c in _squadController.Characters)
        {
            if (c == null || c.Model?.Data == null) continue;
            var m = new CharacterMemberData();
            var id = c.Model.Data.characterId;
            m.characterId = !string.IsNullOrEmpty(id) ? id : c.Model.Data.displayName;
            m.currentHp = c.Model.CurrentHp;
            data.squad.members.Add(m);
        }
    }

    public override void Apply(SaveData data)
    {
        if (data?.squad == null) return;
        if (_squadController == null) return;

        // 1. 조종 대상 전환
        Character targetPlayer = null;
        if (!string.IsNullOrEmpty(data.squad.currentPlayerId))
        {
            foreach (var c in _squadController.Characters)
            {
                if (c?.Model?.Data == null) continue;
                var id = c.Model.Data.characterId;
                var name = c.Model.Data.displayName;
                if (data.squad.currentPlayerId == id || data.squad.currentPlayerId == name)
                {
                    targetPlayer = c;
                    break;
                }
            }
            if (targetPlayer != null && targetPlayer != _squadController.PlayerCharacter)
                _squadController.SetPlayerCharacter(targetPlayer);
        }
        var playerChar = _squadController.PlayerCharacter ?? _squadController.DefaultPlayer;

        // 2. 플레이어 위치 적용
        if (playerChar != null)
        {
            playerChar.Teleport(data.squad.playerPosition);
            playerChar.transform.eulerAngles = new Vector3(0f, data.squad.playerRotationY, 0f);
        }

        // 3. 동료들을 플레이어 주위로 재배치
        _squadController.RepositionCompanionsAround(playerChar != null ? playerChar.transform : null);

        // 4. 멤버별 체력 적용
        foreach (var m in data.squad.members)
        {
            if (string.IsNullOrEmpty(m.characterId)) continue;
            foreach (var c in _squadController.Characters)
            {
                if (c?.Model?.Data == null) continue;
                var id = c.Model.Data.characterId;
                var name = c.Model.Data.displayName;
                if (m.characterId == id || m.characterId == name)
                {
                    c.Model.SetCurrentHpForLoad(m.currentHp);
                    break;
                }
            }
        }
    }
}
