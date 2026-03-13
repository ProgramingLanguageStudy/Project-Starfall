using System;
using UnityEngine;
using TMPro;

/// <summary>
/// 로딩 패널. 상태 텍스트 + 진행률(%).
/// </summary>
public class LoadingView : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private UITweenFacade _facade;
    [SerializeField] private TextMeshProUGUI _statusText;

    public void Initialize()
    {
        var root = _facade != null ? _facade.gameObject : _panel;
        if (root != null)
            root.SetActive(false);
        ClearText();
    }

    public void ClearText()
    {
        if (_statusText != null)
            _statusText.text = string.Empty;
    }

    public void Show(bool clearText = true)
    {
        if (clearText)
            ClearText();
        if (_facade != null)
            _facade.PlayEnter();
        else if (_panel != null)
            _panel.SetActive(true);
    }

    public void Hide(Action onComplete = null)
    {
        if (_facade != null)
            _facade.PlayExit(onComplete);
        else
        {
            if (_panel != null)
                _panel.SetActive(false);
            onComplete?.Invoke();
        }
    }

    /// <param name="progress">null이면 % 미표시(상태 텍스트만). 0~1이면 % 포함.</param>
    public void UpdateProgress(float? progress, string status)
    {
        if (_statusText == null) return;
        var statusMsg = status ?? string.Empty;
        if (progress.HasValue)
        {
            var p = Mathf.Clamp01(progress.Value);
            var percent = Mathf.RoundToInt(p * 100f);
            _statusText.text = string.IsNullOrEmpty(statusMsg) ? $"{percent}%" : $"{statusMsg} {percent}%";
        }
        else
        {
            _statusText.text = statusMsg;
        }
    }
}
