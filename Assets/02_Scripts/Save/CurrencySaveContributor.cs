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

    /// <summary>
    /// Unregister는 OnDisable이 아니라 OnDestroy에서만 한다.
    /// 에디터 Play 종료 시 OnDisable → 저장 코루틴 순으로 가면 Gather 직전에 빠져 gold=0이 될 수 있음.
    /// </summary>
    private void OnDestroy()
    {
        if (GameManager.Instance?.SaveManager != null)
            GameManager.Instance.SaveManager.Unregister(this);
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
