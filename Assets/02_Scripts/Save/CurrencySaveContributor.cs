using UnityEngine;

/// <summary>
/// 골드 세이브/로드. GlobalSaveCoordinator 하위. CurrencyManager와 같은 GameObject에 두거나 Initialize로 주입.
/// </summary>
public class CurrencySaveContributor : SaveContributorBehaviour
{
    public override int SaveOrder => 0;

    private CurrencyManager _currencyManager;

    public void Initialize(CurrencyManager currencyManager)
    {
        _currencyManager = currencyManager;
    }

    private void Awake()
    {
        if (_currencyManager == null)
            _currencyManager = GetComponent<CurrencyManager>();
    }

    public override void Gather(SaveData data)
    {
        if (data == null || _currencyManager == null) return;
        data.gold = _currencyManager.Gold;
    }

    public override void Apply(SaveData data)
    {
        if (data == null || _currencyManager == null) return;
        _currencyManager.LoadFromSave(data.gold);
    }
}
