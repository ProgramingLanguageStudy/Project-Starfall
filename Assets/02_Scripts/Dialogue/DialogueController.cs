using UnityEngine;

/// <summary>
/// 대화 흐름. OnNpcInteracted → Select → Presenter.
/// </summary>
public class DialogueController : MonoBehaviour
{
    [SerializeField] private DialogueSelector _selector;
    [SerializeField] private DialoguePresenter _presenter;
    private QuestPresenter _questPresenter;
    private FlagSystem _flagSystem;

    public void Initialize(QuestPresenter questPresenter, FlagSystem flagSystem)
    {
        if (_questPresenter == null && questPresenter != null)
            _questPresenter = questPresenter;
        _flagSystem = flagSystem;
        _selector?.Initialize(flagSystem);
    }

    private void OnEnable()
    {
        PlaySceneEventHub.OnNpcInteracted += HandleNpcInteracted;
        if (_presenter != null) _presenter.OnDialogueEnded += HandleDialogueEnded;
    }

    private void OnDisable()
    {
        PlaySceneEventHub.OnNpcInteracted -= HandleNpcInteracted;
        if (_presenter != null) _presenter.OnDialogueEnded -= HandleDialogueEnded;
    }

    private void HandleNpcInteracted(string npcId)
    {
        var main = _selector != null ? _selector.SelectMain(npcId) : null;
        if (main == null) return;

        var questList = main.category == DialogueCategory.Casual && _selector != null
            ? _selector.GetAvailableQuests(npcId)
            : null;

        _presenter.RequestStartDialogue(main, questList);
    }

    private void HandleDialogueEnded(DialogueData data)
    {
        if (data == null) return;
        ApplyFlags(data);
        RequestQuestAction(data);
    }

    private void RequestQuestAction(DialogueData data)
    {
        if (_questPresenter == null || string.IsNullOrEmpty(data.questId)) return;
        switch (data.questDialogueType)
        {
            case QuestDialogueType.Complete: _questPresenter.RequestCompleteQuest(data.questId); break;
            case QuestDialogueType.Accept: _questPresenter.RequestAcceptQuest(data.questId); break;
        }
    }

    private void ApplyFlags(DialogueData data)
    {
        if (data.flagsToModify == null) return;
        if (_flagSystem == null) return;
        var fm = _flagSystem;
        foreach (var mod in data.flagsToModify)
        {
            if (string.IsNullOrEmpty(mod.key)) continue;
            if (mod.op == FlagOp.Set) fm.SetFlag(mod.key, mod.value);
            else fm.AddFlag(mod.key, mod.value);
        }
    }
}
