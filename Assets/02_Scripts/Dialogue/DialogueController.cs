using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 대화 흐름. OnNpcInteracted → Select → Presenter.
/// </summary>
public class DialogueController : MonoBehaviour
{
    [SerializeField] private DialogueSelector _selector;
    [SerializeField] private DialoguePresenter _presenter;
    private QuestController _questController;
    private FlagSystem _flagSystem;

    public void Initialize(QuestController questController, FlagSystem flagSystem)
    {
        if (_questController == null && questController != null)
            _questController = questController;
        _flagSystem = flagSystem;
        _presenter.Initialize();
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
        DialogueData main = _selector != null ? _selector.SelectMain(npcId) : null;
        if (main == null) return;

        IReadOnlyList<DialogueData> questList = main.category == DialogueCategory.Casual && _selector != null
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
        if (_questController == null || string.IsNullOrEmpty(data.questId)) return;
        switch (data.questDialogueType)
        {
            case QuestDialogueType.Complete: _questController.RequestCompleteQuest(data.questId); break;
            case QuestDialogueType.Accept: _questController.RequestAcceptQuest(data.questId); break;
        }
    }

    private void ApplyFlags(DialogueData data)
    {
        if (data.flagsToModify == null) return;
        if (_flagSystem == null) return;
        FlagSystem fm = _flagSystem;
        foreach (FlagModification mod in data.flagsToModify)
        {
            if (string.IsNullOrEmpty(mod.key)) continue;
            if (mod.op == FlagOp.Set) fm.SetFlag(mod.key, mod.value);
            else fm.AddFlag(mod.key, mod.value);
        }
    }
}
