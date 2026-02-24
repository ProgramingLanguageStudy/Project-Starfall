using System;
using UnityEngine;

/// <summary>
/// 대화 독립 시스템. 하는 일만: 받은 대화 재생, 다음 문장, 닫기. 로딩·선택·버튼 종류는 모름.
/// </summary>
public class DialogueSystem : MonoBehaviour
{
    private readonly DialogueModel _model = new DialogueModel();
    private Action _onComplete;

    public DialogueModel Model => _model;
    public bool IsTalking => _model.IsTalking;
    public string CurrentSpeakerName => _model.CurrentSpeakerName;

    /// <summary>대화 종료 시. 종료된 DialogueData 전달 (조율층에서 플래그·퀘스트 처리용).</summary>
    public event Action<DialogueData> OnDialogueEnd
    {
        add => _model.OnDialogueEnd += value;
        remove => _model.OnDialogueEnd -= value;
    }

    /// <summary>대화 시작. Coordinator가 호출. 받은 내용만 재생.</summary>
    public void StartDialogue(DialogueData data, Action onComplete = null)
    {
        if (data == null) return;
        _onComplete = onComplete;
        _model.SetDialogue(data);
        GameEvents.OnCursorShowRequested?.Invoke();
    }

    /// <summary>다음 문장. 끝이면 자동 종료.</summary>
    public void Next()
    {
        if (!_model.IsTalking) return;
        int next = _model.CurrentIndex + 1;
        if (next >= _model.LineCount)
        {
            _model.Clear();
            GameEvents.OnCursorHideRequested?.Invoke();
            _onComplete?.Invoke();
            _onComplete = null;
        }
        else
        {
            _model.SetCurrentIndex(next);
        }
    }

    /// <summary>대화 강제 종료.</summary>
    public void EndDialogue()
    {
        if (!_model.IsTalking) return;
        _model.Clear();
        GameEvents.OnCursorHideRequested?.Invoke();
        _onComplete?.Invoke();
        _onComplete = null;
    }
}
