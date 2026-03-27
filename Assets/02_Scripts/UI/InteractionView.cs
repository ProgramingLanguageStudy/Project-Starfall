using UnityEngine;
using TMPro;

/// <summary>
/// 상호작용 안내 문구 표시 전용 View. PlaySceneView 등이 OnInteractTargetChanged 구독 후 Refresh 호출.
/// </summary>
public class InteractionView : MonoBehaviour
{
    [SerializeField] private GameObject _uiPanel;
    [SerializeField] private UITweenFacade _uiFacade;
    [SerializeField] private TextMeshProUGUI _msgText;

    /// <summary>PlaySceneView 등에서 호출. 패널 초기 비활성화.</summary>
    public void Initialize()
    {
        GameObject panel = _uiFacade != null ? _uiFacade.gameObject : _uiPanel;
        if (panel != null)
            panel.SetActive(false);
    }

    /// <summary>메시지 설정 후 표시/숨김. 빈 문자열이면 퇴장.</summary>
    public void Refresh(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            if (_uiFacade != null)
                _uiFacade.PlayExit();
            else if (_uiPanel != null)
                _uiPanel.SetActive(false);
        }
        else
        {
            if (_msgText != null)
                _msgText.text = message;
            if (_uiFacade != null)
                _uiFacade.PlayEnter();
            else if (_uiPanel != null)
                _uiPanel.SetActive(true);
        }
    }
}
