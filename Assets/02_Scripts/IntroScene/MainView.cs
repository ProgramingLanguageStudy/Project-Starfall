using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메인 패널 (Game Start, Logout 버튼). 로그인됐을 때 표시.
/// </summary>
public class MainView : MonoBehaviour
{
    [SerializeField] private UITweenFacade _facade;
    [SerializeField] private Button _gameStartButton;
    [SerializeField] private Button _logoutButton;

    public event Action OnGameStartRequested;
    public event Action OnLogoutRequested;

    public void Initialize()
    {
        _gameStartButton?.onClick.AddListener(() => OnGameStartRequested?.Invoke());
        _logoutButton?.onClick.AddListener(() => OnLogoutRequested?.Invoke());

        if (_facade != null)
            _facade.gameObject.SetActive(false);
        else
        {
            if (_gameStartButton != null) _gameStartButton.gameObject.SetActive(false);
            if (_logoutButton != null) _logoutButton.gameObject.SetActive(false);
        }
    }

    public void Show()
    {
        if (_facade != null)
            _facade.PlayEnter();
        else
        {
            if (_gameStartButton != null) _gameStartButton.gameObject.SetActive(true);
            if (_logoutButton != null) _logoutButton.gameObject.SetActive(true);
        }
    }

    public void Hide()
    {
        if (_facade != null)
            _facade.PlayExit();
        else
        {
            if (_gameStartButton != null) _gameStartButton.gameObject.SetActive(false);
            if (_logoutButton != null) _logoutButton.gameObject.SetActive(false);
        }
    }
}
