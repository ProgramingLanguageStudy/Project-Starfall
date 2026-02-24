using System.Collections.Generic;

/// <summary>
/// IQuestCompletedHandler 등록·호출. PlaySceneServices.QuestCompleted로 접근.
/// </summary>
public class QuestCompletedRegistry
{
    private readonly List<IQuestCompletedHandler> _handlers = new();

    public void Register(IQuestCompletedHandler handler)
    {
        if (handler != null && !_handlers.Contains(handler))
            _handlers.Add(handler);
    }

    public void Unregister(IQuestCompletedHandler handler)
    {
        _handlers.Remove(handler);
    }

    public void InvokeAll(QuestData data)
    {
        var copy = new List<IQuestCompletedHandler>(_handlers);
        foreach (var h in copy)
            h.OnQuestCompleted(data);
    }

    public void Clear() => _handlers.Clear();
}
