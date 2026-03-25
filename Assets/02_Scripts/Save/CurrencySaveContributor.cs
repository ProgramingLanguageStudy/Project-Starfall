using UnityEngine;

/// <summary>
/// 골드 세이브/로드. CurrencyManager와 같은 GameObject에 두고 SaveManager에 직접 등록.
/// </summary>
public class CurrencySaveContributor : SaveContributor
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

    public override void Gather(SaveData data)
    {
        if (data == null || _currencyManager == null)
        {
            if (SaveDevSettings.LogSaveDiagnostics)
                Debug.LogWarning(
                    $"[SaveDiag] CurrencySaveContributor.Gather skipped (gold not written). dataNull={data == null} currencyManagerNull={_currencyManager == null}");
            return;
        }

        data.gold = _currencyManager.Gold;
        if (SaveDevSettings.LogSaveDiagnostics)
            Debug.Log($"[SaveDiag] CurrencySaveContributor.Gather → wrote gold={data.gold}");
    }

    public override void Apply(SaveData data)
    {
        if (data == null || _currencyManager == null)
        {
            if (SaveDevSettings.LogSaveDiagnostics)
                Debug.LogWarning(
                    $"[SaveDiag] CurrencySaveContributor.Apply skipped. dataNull={data == null} currencyManagerNull={_currencyManager == null}");
            return;
        }

        if (SaveDevSettings.LogSaveDiagnostics)
            Debug.Log($"[SaveDiag] CurrencySaveContributor.Apply saveData.gold={data.gold} → LoadFromSave");
        _currencyManager.LoadFromSave(data.gold);
    }
}
