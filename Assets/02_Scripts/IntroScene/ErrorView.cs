using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 에러 알림 패널. 메시지 표시, 닫기 버튼.
/// </summary>
public class ErrorView : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private UITweenFacade _facade;
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private Button _closeButton;

    public void Initialize()
    {
        _closeButton?.onClick.AddListener(Hide);

        var root = _facade != null ? _facade.gameObject : _panel;
        if (root != null)
            root.SetActive(false);
    }

    public void Show(string message)
    {
        if (_messageText != null)
            _messageText.text = message ?? string.Empty;
        if (_facade != null)
            _facade.PlayEnter();
        else if (_panel != null)
            _panel.SetActive(true);
    }

    public void Hide()
    {
        if (_facade != null)
            _facade.PlayExit();
        else if (_panel != null)
            _panel.SetActive(false);
    }
}
