using UnityEngine;

/// <summary>
/// ISaveContributor를 구현하는 MonoBehaviour 베이스. FindObjectsByType으로 수집 가능.
/// Awake에서 자동으로 SaveManager에 등록/해제 처리.
/// </summary>
public abstract class SaveContributorBehaviour : MonoBehaviour, ISaveContributor
{
    public abstract int SaveOrder { get; }
    public abstract void Gather(SaveData data);
    public abstract void Apply(SaveData data);

    protected void OnEnable()
    {
        var sm = GameManager.Instance?.SaveManager;
        Debug.Log($"[SaveContributorBehaviour] OnEnable: {GetType().Name}, SaveManager={sm != null}");
        
        if (sm != null)
        {
            sm.Register(this);
            Debug.Log($"[SaveContributorBehaviour] Registered: {GetType().Name}");
        }
        else
        {
            Debug.LogWarning($"[SaveContributorBehaviour] SaveManager null, cannot register: {GetType().Name}");
        }
    }

    protected void OnDisable()
    {
        var sm = GameManager.Instance?.SaveManager;
        if (sm != null)
        {
            sm.Unregister(this);
        }
    }
}
