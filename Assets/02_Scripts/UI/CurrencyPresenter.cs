using UnityEngine;

/// <summary>
/// CurrencyManager ↔ CurrencyView 연결. OnGoldChanged 구독 후 View 갱신.
/// Play 씬 HUD에 배치. CurrencyView 인스펙터 연결.
/// </summary>
public class CurrencyPresenter : MonoBehaviour
{
    [SerializeField] private CurrencyView _view;

    private void OnEnable()
    {
        var cm = GameManager.Instance?.CurrencyManager;
        if (cm == null) return;

        cm.OnGoldChanged += HandleGoldChanged;
        HandleGoldChanged(cm.Gold);
    }

    private void OnDisable()
    {
        var cm = GameManager.Instance?.CurrencyManager;
        if (cm != null)
            cm.OnGoldChanged -= HandleGoldChanged;
    }

    private void HandleGoldChanged(int gold)
    {
        _view?.Refresh(gold);
    }
}
