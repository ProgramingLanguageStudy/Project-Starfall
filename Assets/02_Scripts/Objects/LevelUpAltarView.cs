using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpAltarView : PanelViewBase
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private UITweenFacade _panelFacade;
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _submitButton;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _bodyText;
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private TextMeshProUGUI _submitButtonText;
    [SerializeField] private Image _characterPortrait;

    public event Action OnSubmitRequested;
    public event Action OnCloseRequested;

    public void Initialize()
    {
        var root = _panelFacade != null ? _panelFacade.gameObject : _panel;
        if (root != null)
            root.SetActive(false);

        if (_closeButton != null)
            _closeButton.onClick.AddListener(HandleCloseClicked);
        if (_submitButton != null)
            _submitButton.onClick.AddListener(HandleSubmitClicked);
    }

    public void RequestOpen()
    {
        if (IsOpen) return;
        OpenPanel();
    }

    public void RequestClose()
    {
        if (!IsOpen) return;
        ClosePanel();
    }

    public void SetTitle(string text)
    {
        if (_titleText != null)
            _titleText.text = text ?? string.Empty;
    }

    public void SetBody(string text)
    {
        if (_bodyText != null)
            _bodyText.text = text ?? string.Empty;
    }

    public void SetMessage(string text)
    {
        if (_messageText != null)
            _messageText.text = text ?? string.Empty;
    }

    public void SetSubmitButton(string label, bool interactable)
    {
        if (_submitButtonText != null)
            _submitButtonText.text = label ?? string.Empty;
        if (_submitButton != null)
            _submitButton.interactable = interactable;
    }

    public void SetCharacterPortrait(Sprite portrait)
    {
        if (_characterPortrait != null)
            _characterPortrait.sprite = portrait;
    }

    private void HandleSubmitClicked()
    {
        OnSubmitRequested?.Invoke();
    }

    private void HandleCloseClicked()
    {
        OnCloseRequested?.Invoke();
    }

    protected override void OnPanelOpened()
    {
        if (_panelFacade != null)
            _panelFacade.PlayEnter();
        else if (_panel != null)
            _panel.SetActive(true);
        else
            gameObject.SetActive(true);
    }

    protected override void OnPanelClosed()
    {
        if (_panelFacade != null)
            _panelFacade.PlayExit();
        else if (_panel != null)
            _panel.SetActive(false);
        else
            gameObject.SetActive(false);
    }
}
