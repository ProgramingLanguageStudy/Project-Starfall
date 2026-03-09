using UnityEngine;
using TMPro;

/// <summary>
/// 골드 HUD 표시. CurrencyPresenter에서 Refresh 호출.
/// </summary>
public class CurrencyView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _goldText;
    [SerializeField] private string _format = "{0:N0}";

    /// <summary>표시할 골드 갱신.</summary>
    public void Refresh(int gold)
    {
        if (_goldText != null)
            _goldText.text = string.Format(_format, gold);
    }
}
