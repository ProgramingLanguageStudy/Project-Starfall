using System;
using UnityEngine;

/// <summary>
/// 대화 상태 보관. 현재 문장·인덱스·category 노출.
/// </summary>
public class DialogueModel
{
    private DialogueData _data;
    private int _currentIndex;

    public bool IsTalking => _data != null;
    public int CurrentIndex => _currentIndex;
    public int LineCount => _data?.Lines?.Length ?? 0;
    public DialogueCategory Category => _data?.category ?? DialogueCategory.Casual;

    public string CurrentSpeakerName => _data?.SpeakerDisplayName ?? "";
    public string CurrentQuestId => _data?.questId;

    public event Action OnDialogueStateChanged;
    public event Action<DialogueData> OnDialogueEnd;

    public void SetDialogue(DialogueData data)
    {
        _data = data;
        _currentIndex = 0;
        OnDialogueStateChanged?.Invoke();
    }

    public void SetCurrentIndex(int index)
    {
        _currentIndex = index;
        OnDialogueStateChanged?.Invoke();
    }

    public void SetRandomLine()
    {
        var lines = _data?.Lines;
        if (lines == null || lines.Length == 0) return;
        _currentIndex = UnityEngine.Random.Range(0, lines.Length);
        OnDialogueStateChanged?.Invoke();
    }

    public void Clear()
    {
        if (_data == null) return;
        var ended = _data;
        _data = null;
        _currentIndex = 0;
        OnDialogueEnd?.Invoke(ended);
        OnDialogueStateChanged?.Invoke();
    }

    public string GetCurrentSentence()
    {
        var lines = _data?.Lines;
        if (lines == null || _currentIndex < 0 || _currentIndex >= lines.Length) return "";
        return lines[_currentIndex];
    }

    public bool IsLastLine => _data != null && _currentIndex >= _data.Lines.Length - 1;
}
