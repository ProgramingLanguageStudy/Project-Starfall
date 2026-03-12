using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 포탈 메뉴 안에서 포탈 하나를 나타내는 슬롯. Button + TextMeshProUGUI만 보유.
/// </summary>
public class PortalSlot : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private TextMeshProUGUI _labelText;

    /// <summary> 표시 이름과 클릭 시 동작 설정. View에서 한 번만 호출. </summary>
    public void Set(string displayName, Action onClick)
    {
        if (_labelText != null)
            _labelText.text = displayName;

        if (_button != null && onClick != null)
        {
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => onClick());
        }
    }
}
