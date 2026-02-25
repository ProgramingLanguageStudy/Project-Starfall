using UnityEngine;

/// <summary>
/// 퀘스트 세이브/로드. 진행 중인 퀘스트 목록 저장. 로드 시 QuestSystem 복원 + 플래그 동기화.
/// Gather: 수집형은 currentAmount 저장 안 함(인벤토리가 진실의 원천). 처치/방문형은 저장.
/// Apply: 수집형은 인벤토리에서 개수 참조. 처치/방문형은 entry.currentAmount 사용.
/// PlaySaveCoordinator.Initialize에서 주입. Apply 순서상 Inventory가 Quest보다 먼저 실행되어야 함.
/// </summary>
public class QuestSaveContributor : SaveContributorBehaviour
{
    public override int SaveOrder => 3;

    private QuestPresenter _questPresenter;
    private FlagSystem _flagSystem;
    private Inventory _inventory;

    public void Initialize(QuestPresenter questPresenter, FlagSystem flagSystem, Inventory inventory)
    {
        _questPresenter = questPresenter;
        _flagSystem = flagSystem;
        _inventory = inventory;
    }

    public override void Gather(SaveData data)
    {
        if (data?.quests == null) return;
        if (_questPresenter?.System == null) return;
        var presenter = _questPresenter;
        if (presenter?.System == null) return;

        data.quests.entries.Clear();
        foreach (var quest in presenter.System.GetActiveQuests())
        {
            if (quest == null || string.IsNullOrEmpty(quest.QuestId)) continue;

            int amountToSave = quest.CurrentAmount;
            if (quest.QuestType == QuestType.Gather)
                amountToSave = 0;

            data.quests.entries.Add(new QuestProgressEntry
            {
                questId = quest.QuestId,
                targetId = quest.TargetId ?? "",
                currentAmount = amountToSave
            });
        }
    }

    public override void Apply(SaveData data)
    {
        if (data?.quests == null) return;
        if (_questPresenter?.System == null || _flagSystem == null) return;
        var presenter = _questPresenter;
        var fm = _flagSystem;
        if (presenter?.System == null || fm == null) return;

        foreach (var entry in data.quests.entries)
        {
            if (string.IsNullOrEmpty(entry.questId)) continue;
            if (presenter.System.HasQuest(entry.questId)) continue;

            var questData = Resources.Load<QuestData>($"Quests/{entry.questId}");
            if (questData == null) continue;

            presenter.System.AcceptQuest(questData);
            fm.SetFlag(GameStateKeys.QuestAccepted(entry.questId), 1);

            int amount;
            if (questData.QuestType == QuestType.Gather && _inventory != null && !string.IsNullOrEmpty(entry.targetId))
                amount = _inventory.GetTotalCount(entry.targetId);
            else
                amount = entry.currentAmount;

            if (!string.IsNullOrEmpty(entry.targetId))
                presenter.System.SetTaskProgress(entry.questId, entry.targetId, amount);

            if (amount >= questData.TargetAmount)
                fm.SetFlag(GameStateKeys.QuestObjectivesDone(entry.questId), 1);
        }
    }
}
