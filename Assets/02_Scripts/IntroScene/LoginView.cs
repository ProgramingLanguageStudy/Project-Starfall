using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로그인 패널. 이메일/비밀번호 입력, 로그인/회원가입 버튼.
/// </summary>
public class LoginView : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private UITweenFacade _facade;
    [SerializeField] private TMP_InputField _emailInput;
    [SerializeField] private TMP_InputField _passwordInput;
    [SerializeField] private Button _loginButton;
    [SerializeField] private Button _signUpButton;
    [SerializeField] private Button _guestButton;

    public event Action<string, string> OnLoginRequested;
    public event Action<string, string> OnSignUpRequested;
    /// <summary>계정 없이 진행(로컬 세이브·ReadyGuest). Firebase 익명 로그인 아님.</summary>
    public event Action OnGuestPlayRequested;

    public void Initialize()
    {
        _loginButton?.onClick.AddListener(HandleLoginClick);
        _signUpButton?.onClick.AddListener(HandleSignUpClick);
        _guestButton?.onClick.AddListener(HandleGuestClick);

        var root = _facade != null ? _facade.gameObject : _panel;
        if (root != null)
            root.SetActive(false);
    }

    private void HandleLoginClick()
    {
        var (email, password) = GetCredentials();
        OnLoginRequested?.Invoke(email, password);
    }

    private void HandleSignUpClick()
    {
        var (email, password) = GetCredentials();
        OnSignUpRequested?.Invoke(email, password);
    }

    private void HandleGuestClick() => OnGuestPlayRequested?.Invoke();

    private (string email, string password) GetCredentials()
    {
        var email = _emailInput != null ? _emailInput.text.Trim() : string.Empty;
        var password = _passwordInput != null ? _passwordInput.text : string.Empty;
        return (email, password);
    }

    public void Show()
    {
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

    public void SetInteractable(bool interactable)
    {
        if (_loginButton != null) _loginButton.interactable = interactable;
        if (_signUpButton != null) _signUpButton.interactable = interactable;
        if (_guestButton != null) _guestButton.interactable = interactable;
        if (_emailInput != null) _emailInput.interactable = interactable;
        if (_passwordInput != null) _passwordInput.interactable = interactable;
    }
}
