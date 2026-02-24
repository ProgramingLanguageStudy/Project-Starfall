/// <summary>진행 중인 퀘스트 하나의 런타임 상태. QuestData를 필드로 갖고 생성.</summary>
public class QuestModel
{
    private readonly QuestData _data;

    public QuestData Data => _data;

    public string QuestId => _data.QuestId;
    public string Title => _data.Title;
    public string Description => _data.Description;
    public QuestType QuestType => _data.QuestType;
    public string TargetId => _data.TargetId;
    public int TargetAmount => _data.TargetAmount;

    public int CurrentAmount { get; set; }

    public bool IsCompleted => CurrentAmount >= TargetAmount;
    /// <summary>호환용. 단일 목표이므로 IsCompleted와 동일.</summary>
    public bool IsAllTasksCompleted() => IsCompleted;

    public QuestModel(QuestData data)
    {
        _data = data;
        CurrentAmount = 0;
    }
}
