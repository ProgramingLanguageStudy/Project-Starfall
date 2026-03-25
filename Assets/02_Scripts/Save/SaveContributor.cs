using UnityEngine;

/// <summary>
/// ISaveContributor를 구현하는 MonoBehaviour 베이스. 
/// OnEnable/OnDisable에서 자동으로 SaveManager에 등록/해제 처리.
/// 자식 클래스는 Gather/Apply만 구현하면 됨.
/// </summary>
public abstract class SaveContributor : MonoBehaviour, ISaveContributor
{
    public abstract int SaveOrder { get; }
    public abstract void Gather(SaveData data);
    public abstract void Apply(SaveData data);

    protected virtual void OnEnable()
    {
        var sm = GameManager.Instance?.SaveManager;
        if (sm != null)
        {
            sm.Register(this);
        }
        else
        {
            Debug.LogError($"[SaveContributor] SaveManager null, cannot register: {GetType().Name}");
        }
    }

    protected virtual void OnDisable()
    {
        var sm = GameManager.Instance?.SaveManager;
        if (sm != null)
        {
            sm.Unregister(this);
        }
    }
}
