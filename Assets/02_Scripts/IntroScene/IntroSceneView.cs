using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IntroSceneView : MonoBehaviour
{
    [Header("버튼")]
    [SerializeField] Button _playButton;

    [Header("로딩 UI")]
    [SerializeField] [Tooltip("로딩 패널. 시작 버튼 클릭 시 표시")]
    private GameObject _loadingPanel;
    [SerializeField] private Slider _loadingSlider;
    [SerializeField] private TextMeshProUGUI _loadingStatusText;

    public event Action OnPlayRequested;

    public void Initialize()
    {
        if (GameManager.Instance?.SaveManager != null)
            GameManager.Instance.SaveManager.Load();

        _playButton?.onClick.AddListener(() => OnPlayRequested?.Invoke());

        if (_loadingPanel != null)
            _loadingPanel.SetActive(false);
    }

    /// <summary>로딩 패널 표시.</summary>
    public void ShowLoading()
    {
        if (_loadingPanel != null) _loadingPanel.SetActive(true);
    }

    /// <summary>로딩 진행률·상태 갱신.</summary>
    public void UpdateProgress(float progress, string status)
    {
        if (_loadingSlider != null)
            _loadingSlider.value = Mathf.Clamp01(progress);
        if (_loadingStatusText != null)
            _loadingStatusText.text = status ?? string.Empty;
    }
}
