using System;
using UnityEngine;
using UnityEngine.UI;

public class SettingsView : PanelViewBase
{
    [SerializeField] GameObject _settingsPanel;
    [SerializeField] UITweenFacade _settingsFacade;
    [SerializeField] Button _closeButton;
    [SerializeField] Button _escapeButton;
    [SerializeField] Button _introButton;
    [SerializeField] Button _quitButton;

    /// <summary>끼임 탈출 버튼 클릭 시 발행. PlayScene에서 구독해 분대를 마을로 텔레포트.</summary>
    public event Action OnEscapeRequested;

    public void Initialize()
    {
        if (_settingsFacade != null && _settingsFacade.gameObject.activeSelf)
            _settingsFacade.gameObject.SetActive(false);
        else if (_settingsPanel != null)
            _settingsPanel.SetActive(false);
        _closeButton.onClick.AddListener(() => ToggleSettings());
        _escapeButton.onClick.AddListener(() => OnEscapeRequested?.Invoke());
        _introButton.onClick.AddListener(() => OnIntroButtonClicked());
        _quitButton.onClick.AddListener(() => OnQuitButtonClicked());
    }

    /// <summary>설정 토글. PlayScene 입력에서 Request.</summary>
    public void RequestToggle() => ToggleSettings();

    private void ToggleSettings()
    {
        GameObject panel = _settingsFacade != null ? _settingsFacade.gameObject : _settingsPanel;
        if (panel == null) return;
        bool isOpening = !panel.activeSelf;

        if (isOpening)
        {
            OpenPanel();
        }
        else
        {
            ClosePanel();
        }
    }

    protected override void OnPanelOpened()
    {
        if (_settingsFacade != null)
            _settingsFacade.PlayEnter();
        else if (_settingsPanel != null)
            _settingsPanel.SetActive(true);
    }

    protected override void OnPanelClosed()
    {
        if (_settingsFacade != null)
            _settingsFacade.PlayExit();
        else if (_settingsPanel != null)
            _settingsPanel.SetActive(false);
    }

    public void OnIntroButtonClicked()
    {
        SceneLoadManager slm = GameManager.Instance?.SceneLoadManager;
        if (slm == null)
        {
            Debug.LogError("[SettingsView] SceneLoadManager is null.");
            return;
        }
        slm.RequestLoadIntroFromPlay();
    }

    public void OnQuitButtonClicked()
    {
        Application.Quit();
    }
}
