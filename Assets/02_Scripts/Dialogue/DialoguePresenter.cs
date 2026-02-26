using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Model↔View 연결. 다음/끝내기/퀘스트 버튼 처리.
/// </summary>
public class DialoguePresenter : MonoBehaviour
{
    [SerializeField] private DialogueSystem _system;
    [SerializeField] private DialogueView _view;

    public event Action<DialogueData> OnDialogueEnded;

    private DialogueModel Model => _system?.Model;
    private DialogueData _main;
    private IReadOnlyList<DialogueData> _questList;

    private void Awake()
    {
        if (_system == null) _system = FindFirstObjectByType<DialogueSystem>();
        if (_view == null) _view = FindFirstObjectByType<DialogueView>();
    }

    public void Initialize()
    {
        _view.Initialize();
    }

    private void OnEnable()
    {
        if (Model != null) Model.OnDialogueStateChanged += RefreshView;
        if (_system != null) _system.OnDialogueEnd += HandleDialogueEnd;
        if (_view != null)
        {
            _view.OnNextClicked += HandleNext;
            _view.OnEndClicked += HandleEnd;
            _view.OnQuestDialogueSelected += HandleQuestSelected;
        }
    }

    private void OnDisable()
    {
        if (Model != null) Model.OnDialogueStateChanged -= RefreshView;
        if (_system != null) _system.OnDialogueEnd -= HandleDialogueEnd;
        if (_view != null)
        {
            _view.OnNextClicked -= HandleNext;
            _view.OnEndClicked -= HandleEnd;
            _view.OnQuestDialogueSelected -= HandleQuestSelected;
        }
    }

    public void RequestStartDialogue(DialogueData main, IReadOnlyList<DialogueData> questList)
    {
        if (main == null || _system == null || _system.IsTalking) return;
        _main = main;
        _questList = questList;
        _system.StartDialogue(main);
        RefreshView();
    }

    private void HandleDialogueEnd(DialogueData data)
    {
        _main = null;
        _questList = null;
        OnDialogueEnded?.Invoke(data);
    }

    private void RefreshView()
    {
        if (_view == null) return;
        if (Model != null && Model.IsTalking)
        {
            _view.Display(Model.CurrentSpeakerName, Model.GetCurrentSentence());
            _view.SetButtonMode(Model.Category, _questList, Model.IsLastLine);
        }
        else
            _view.Close();
    }

    private void HandleNext()
    {
        if (_system == null) return;
        if (_view != null && _view.TrySkipTyping()) return;
        _system.Next();
    }

    private void HandleEnd() => _system?.EndDialogue();

    private void HandleQuestSelected(DialogueData quest)
    {
        if (quest == null || _system == null) return;
        _main = quest;
        _questList = null;
        _system.StartDialogue(quest);
        _view.SetButtonMode(DialogueCategory.Quest, null, false);
        RefreshView();
    }
}
