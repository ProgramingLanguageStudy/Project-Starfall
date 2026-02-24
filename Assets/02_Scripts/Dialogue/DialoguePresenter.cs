using System;
using UnityEngine;

/// <summary>
/// DialogueModel과 DialogueView 연결. 다음/끝내기만 System에 전달.
/// </summary>
public class DialoguePresenter : MonoBehaviour
{
    [SerializeField] private DialogueSystem _system;
    [SerializeField] private DialogueView _view;

    /// <summary>대화 종료 시 발행. Controller가 구독.</summary>
    public event Action<DialogueData> OnDialogueEnded;

    private DialogueModel Model => _system != null ? _system.Model : null;

    private void Awake()
    {
        if (_system == null) _system = FindFirstObjectByType<DialogueSystem>();
        if (_view == null) _view = FindFirstObjectByType<DialogueView>();
        if (_system == null) Debug.LogWarning("[DialoguePresenter] DialogueSystem이 없습니다.");
        if (_view == null) Debug.LogWarning("[DialoguePresenter] DialogueView가 없습니다.");
    }

    private void OnEnable()
    {
        if (Model != null)
            Model.OnDialogueStateChanged += RefreshView;
        if (_system != null)
            _system.OnDialogueEnd += HandleDialogueEnd;
        if (_view != null)
        {
            _view.OnNextClicked += HandleNext;
            _view.OnEndClicked += HandleEnd;
        }
    }

    private void OnDisable()
    {
        if (Model != null)
            Model.OnDialogueStateChanged -= RefreshView;
        if (_system != null)
            _system.OnDialogueEnd -= HandleDialogueEnd;
        if (_view != null)
        {
            _view.OnNextClicked -= HandleNext;
            _view.OnEndClicked -= HandleEnd;
        }
    }

    /// <summary>Controller에서 호출. 대화 시작 요청.</summary>
    public void RequestStartDialogue(DialogueData data)
    {
        if (data == null || _system == null || _system.IsTalking) return;
        _system.StartDialogue(data);
    }

    private void HandleDialogueEnd(DialogueData data)
    {
        OnDialogueEnded?.Invoke(data);
    }

    private void RefreshView()
    {
        if (_view == null) return;
        if (Model != null && Model.IsTalking)
            _view.Display(Model.GetSpeakerName(), Model.GetCurrentSentence());
        else
            _view.Close();
    }

    private void HandleNext()
    {
        if (_system == null) return;
        if (_view != null && _view.TrySkipTyping())
            return;
        _system.Next();
    }

    private void HandleEnd()
    {
        _system?.EndDialogue();
    }
}
