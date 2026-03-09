using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 계정 귀속 데이터(골드 등) 세이브/로드 조율. GameManager 하위, SaveManager에 등록.
/// Contributors는 런타임에 수집(CurrencySaveContributor는 CurrencyManager GameObject에 있음).
/// </summary>
public class GlobalSaveCoordinator : MonoBehaviour, ISaveHandler
{
    private List<SaveContributorBehaviour> _contributors;

    private void Awake()
    {
        _contributors = new List<SaveContributorBehaviour>();

        var cm = GameManager.Instance?.CurrencyManager;
        if (cm != null)
        {
            var contrib = cm.GetComponent<CurrencySaveContributor>();
            if (contrib == null)
                contrib = cm.gameObject.AddComponent<CurrencySaveContributor>();
            contrib.Initialize(cm);
            _contributors.Add(contrib);
        }
    }

    private void OnEnable()
    {
        if (GameManager.Instance?.SaveManager != null)
            GameManager.Instance.SaveManager.Register(this);
    }

    private void OnDisable()
    {
        if (GameManager.Instance?.SaveManager != null)
            GameManager.Instance.SaveManager.Unregister(this);
    }

    public void Gather(SaveData data)
    {
        if (data == null || _contributors == null) return;
        foreach (var c in _contributors.Where(x => x != null).OrderBy(x => x.SaveOrder))
            c.Gather(data);
    }

    public void Apply(SaveData data)
    {
        if (data == null || _contributors == null) return;
        foreach (var c in _contributors.Where(x => x != null).OrderBy(x => x.SaveOrder))
            c.Apply(data);
    }
}
