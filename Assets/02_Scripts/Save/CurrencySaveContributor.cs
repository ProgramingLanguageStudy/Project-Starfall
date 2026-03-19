using UnityEngine;

/// <summary>
/// 골드 세이브/로드. CurrencyManager와 같은 GameObject에 두고 SaveManager에 직접 등록.
/// </summary>
public class CurrencySaveContributor : SaveContributorBehaviour
{
    public override int SaveOrder => 4; // Play Contributor(0~3) 이후

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
