using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsView : PanelViewBase
{
    [SerializeField] GameObject _settingsPanel;
    [SerializeField] Button _closeButton;
    [SerializeField] Button _escapeButton;
    [SerializeField] Button _introButton;
    [SerializeField] Button _quitButton;

    /// <summary>끼임 탈출 버튼 클릭 시 발행. PlayScene에서 구독해 분대를 마을로 텔레포트.</summary>
    public event Action OnEscapeRequested;

    public void Initialize()
    {
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
        if (_settingsPanel == null) return;
        bool isOpening = !_settingsPanel.activeSelf;

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
        _settingsPanel.SetActive(true);
    }

    protected override void OnPanelClosed()
    {
        _settingsPanel.SetActive(false);
    }

    public void OnIntroButtonClicked()
    {
        GameManager.Instance.SaveManager.Save();
        SceneManager.LoadScene("Intro");
    }

    public void OnQuitButtonClicked()
    {
        GameManager.Instance.SaveManager.Save();
        Application.Quit();
    }
}
