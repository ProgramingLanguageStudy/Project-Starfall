using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Play 씬 세이브/로드 조율. ISaveHandler 구현, SaveManager에 등록.
/// PlayScene에서 Initialize 호출 시 의존성 주입. 인스펙터에서 Contributor 할당.
/// </summary>
public class PlaySaveCoordinator : MonoBehaviour, ISaveHandler
{
    [SerializeField] [Tooltip("세이브/로드에 참여할 Contributor. SaveOrder 순으로 처리")]
    private List<SaveContributorBehaviour> _contributors = new List<SaveContributorBehaviour>();

    public void Initialize(SquadController squadController, FlagSystem flagSystem, QuestPresenter questPresenter, Inventory inventory)
    {
        if (_contributors == null) return;
        foreach (var c in _contributors)
        {
            if (c == null) continue;
            if (c is SquadSaveContributor squad) squad.Initialize(squadController);
            else if (c is FlagSaveContributor flag) flag.Initialize(flagSystem);
            else if (c is QuestSaveContributor quest) quest.Initialize(questPresenter, flagSystem, inventory);
            else if (c is InventorySaveContributor inv) inv.Initialize(inventory);
        }
    }

    private void OnEnable()
    {
        if (GameManager.Instance?.SaveManager != null)
            GameManager.Instance.SaveManager.Register(this);
    }

    private void OnDisable()
    {
        if (GameManager.Instance?.SaveManager != null)
            GameManager.Instance.SaveManager.Unregister(this);
    }

    public void Gather(SaveData data)
    {
        if (data == null || _contributors == null) return;
        foreach (var c in _contributors.Where(x => x != null).OrderBy(x => x.SaveOrder))
            c.Gather(data);
    }

    /// <summary>세이브 실행. PlayScene 입력에서 Request.</summary>
    public void RequestSave() => _ = GameManager.Instance?.SaveManager?.SaveAsync();

    public void Apply(SaveData data)
    {
        if (data == null || _contributors == null) return;
        foreach (var c in _contributors.Where(x => x != null).OrderBy(x => x.SaveOrder))
            c.Apply(data);
    }
}
