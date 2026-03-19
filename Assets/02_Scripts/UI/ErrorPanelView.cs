using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 전역 에러 메시지 표시. UIManager가 로드 후 Show/Hide.
/// </summary>
public class ErrorPanelView : MonoBehaviour
{
    [SerializeField] private GameObject _root;
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private Button _closeButton;

    private void Awake()
    {
        if (_closeButton != null)
            _closeButton.onClick.AddListener(Hide);
    }

    public void Show(string message)
    {
        if (_messageText != null)
            _messageText.text = message ?? string.Empty;
        if (_root != null)
            _root.SetActive(true);
        else
            gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (_root != null)
            _root.SetActive(false);
        else
            gameObject.SetActive(false);
    }
}
