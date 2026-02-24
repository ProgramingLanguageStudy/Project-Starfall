using System;

/// <summary>
/// 대화 상태만 보관. 현재 문장 반환·인덱스 노출. 제어(다음/종료)는 System에서.
/// </summary>
public class DialogueModel
{
    private DialogueData _data;
    private int _currentIndex;

    public bool IsTalking => _data != null;
    public int CurrentIndex => _currentIndex;
    public int LineCount => _data?.Lines?.Length ?? 0;

    public string CurrentNpcId => _data?.npcId ?? "";
    public string CurrentSpeakerName => _data?.SpeakerDisplayName ?? "";
    /// <summary>퀘스트 관련 대화일 때 퀘스트 ID.</summary>
    public string CurrentQuestId => _data != null ? _data.questId : null;

    public event Action OnDialogueStateChanged;
    /// <summary>대화 종료 시. 종료된 DialogueData를 전달 (플래그·퀘스트 처리용).</summary>
    public event Action<DialogueData> OnDialogueEnd;

    /// <summary>System이 대화 시작 시 호출. 데이터만 넣고 인덱스 0.</summary>
    public void SetDialogue(DialogueData data)
    {
        _data = data;
        _currentIndex = 0;
        OnDialogueStateChanged?.Invoke();
    }

    /// <summary>System이 다음 문장으로 넘길 때 호출.</summary>
    public void SetCurrentIndex(int index)
    {
        _currentIndex = index;
        OnDialogueStateChanged?.Invoke();
    }

    /// <summary>System이 대화 종료 시 호출.</summary>
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
        if (lines == null || _currentIndex < 0 || _currentIndex >= lines.Length)
            return "";
        return lines[_currentIndex];
    }

    public string GetSpeakerName() => CurrentSpeakerName;
}
