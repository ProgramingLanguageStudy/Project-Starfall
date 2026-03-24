using System;
using System.Collections.Generic;

/// <summary>
/// Chest 등록/해제를 관리하는 레지스트리.
/// - 월드 오브젝트가 "언제 생성되었는지"와 무관하게, 등록 시점에만 의존하도록 만든다.
/// - 저장/로드 적용은 레지스트리 밖(Contributor/Applier)에서 처리한다.
/// </summary>
public class ChestRegistry
{
    private readonly HashSet<Chest> _items = new HashSet<Chest>();

    public event Action<Chest> OnRegistered;

    public IEnumerable<Chest> Items => _items;

    public void Register(Chest chest)
    {
        if (chest == null) return;
        if (_items.Add(chest))
            OnRegistered?.Invoke(chest);
    }

    public void Unregister(Chest chest)
    {
        if (chest == null) return;
        _items.Remove(chest);
    }

    public void Clear()
    {
        _items.Clear();
        OnRegistered = null;
    }
}

