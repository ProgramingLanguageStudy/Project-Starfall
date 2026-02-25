using System;
using UnityEngine;

/// <summary>
/// 대화 재생. 시작·다음·종료만 담당.
/// </summary>
public class DialogueSystem : MonoBehaviour
{
    private readonly DialogueModel _model = new DialogueModel();

    public DialogueModel Model => _model;
    public bool IsTalking => _model.IsTalking;

    public event Action<DialogueData> OnDialogueEnd
    {
        add => _model.OnDialogueEnd += value;
        remove => _model.OnDialogueEnd -= value;
    }

    public void StartDialogue(DialogueData main)
    {
        if (main == null) return;
        var wasTalking = _model.IsTalking;
        _model.SetDialogue(main);

        if (main.category == DialogueCategory.Casual)
            _model.SetRandomLine();

        if (!wasTalking)
            GameEvents.OnCursorShowRequested?.Invoke();
    }

    public void Next()
    {
        if (!_model.IsTalking) return;

        if (_model.Category == DialogueCategory.Casual)
        {
            _model.SetRandomLine();
            return;
        }

        if (_model.CurrentIndex + 1 >= _model.LineCount)
            Finish();
        else
            _model.SetCurrentIndex(_model.CurrentIndex + 1);
    }

    public void EndDialogue()
    {
        if (!_model.IsTalking) return;
        Finish();
    }

    private void Finish()
    {
        _model.Clear();
        GameEvents.OnCursorHideRequested?.Invoke();
    }
}
