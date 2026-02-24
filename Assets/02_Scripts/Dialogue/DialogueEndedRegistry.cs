using System.Collections.Generic;

/// <summary>
/// IDialogueEndedHandler 등록·호출. PlaySceneServices.DialogueEnded로 접근.
/// </summary>
public class DialogueEndedRegistry
{
    private readonly List<IDialogueEndedHandler> _handlers = new();

    public void Register(IDialogueEndedHandler handler)
    {
        if (handler != null && !_handlers.Contains(handler))
            _handlers.Add(handler);
    }

    public void Unregister(IDialogueEndedHandler handler)
    {
        _handlers.Remove(handler);
    }

    public void InvokeAll(DialogueData data)
    {
        var copy = new List<IDialogueEndedHandler>(_handlers);
        foreach (var h in copy)
            h.OnDialogueEnded(data);
    }

    public void Clear() => _handlers.Clear();
}
