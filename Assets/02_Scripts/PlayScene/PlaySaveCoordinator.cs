using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Play 씬 세이브 Contributor 초기화·등록·언로드. SaveManager에 Contributor 직접 등록.
/// PlayScene에서 Initialize 호출 시 의존성 주입. 인스펙터에서 Contributor 할당.
/// </summary>
public class PlaySaveCoordinator : MonoBehaviour
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
        var sm = GameManager.Instance?.SaveManager;
        if (sm == null || _contributors == null) return;
        foreach (var c in _contributors)
        {
            if (c != null) sm.Register(c);
        }
    }

    private void OnDisable()
    {
        var sm = GameManager.Instance?.SaveManager;
        if (sm == null) return;
        sm.SaveBeforeUnload(); // Unregister 전에 저장 (빈 squad 방지)
        if (_contributors == null) return;
        foreach (var c in _contributors)
        {
            if (c != null) sm.Unregister(c);
        }
    }

    /// <summary>세이브 실행. PlayScene 입력에서 Request.</summary>
    public void RequestSave()
    {
        var sm = GameManager.Instance?.SaveManager;
        if (sm != null)
            sm.StartCoroutine(sm.SaveAsync(null));
    }
}
