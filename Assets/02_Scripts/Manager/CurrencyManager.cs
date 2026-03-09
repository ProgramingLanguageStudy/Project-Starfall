using System;
using UnityEngine;

/// <summary>
/// 계정 귀속 재화(골드) 관리. GameManager 하위. 세이브/로드는 GlobalSaveCoordinator → CurrencySaveContributor.
/// 접근: GameManager.Instance.CurrencyManager
/// </summary>
public class CurrencyManager : MonoBehaviour
{
    private int _gold;

    /// <summary>현재 골드. UI 표시용.</summary>
    public int Gold => _gold;

    /// <summary>골드 변경 시 (변경 후 값). UI 갱신용.</summary>
    public event Action<int> OnGoldChanged;

    /// <summary>로드 시 호출. CurrencySaveContributor에서 사용.</summary>
    public void LoadFromSave(int gold)
    {
        _gold = Mathf.Max(0, gold);
        OnGoldChanged?.Invoke(_gold);
    }

    /// <summary>골드 추가. amount는 0 이상.</summary>
    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        _gold = Mathf.Max(0, _gold + amount);
        OnGoldChanged?.Invoke(_gold);
    }

    /// <summary>골드 차감 시도. 보유량 이상이면 실패. 성공 시 true.</summary>
    public bool TrySpendGold(int amount)
    {
        if (amount <= 0) return true;
        if (_gold < amount) return false;
        _gold -= amount;
        OnGoldChanged?.Invoke(_gold);
        return true;
    }
}
