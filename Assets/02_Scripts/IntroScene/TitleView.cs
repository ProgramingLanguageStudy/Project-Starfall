using System;
using UnityEngine;

/// <summary>
/// 타이틀 패널 (로고/제목). 씬 시작 시 생명주기 관리자가 Show 호출.
/// </summary>
public class TitleView : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private UITweenFacade _facade;

    public void Initialize()
    {
        var root = _facade != null ? _facade.gameObject : _panel;
        if (root != null)
            root.SetActive(false);
    }

    public void Show(Action onComplete = null)
    {
        if (_facade != null)
            _facade.PlayEnter(onComplete);
        else
        {
            if (_panel != null)
                _panel.SetActive(true);
            onComplete?.Invoke();
        }
    }

    public void Hide()
    {
        if (_facade != null)
            _facade.PlayExit();
        else if (_panel != null)
            _panel.SetActive(false);
    }
}
