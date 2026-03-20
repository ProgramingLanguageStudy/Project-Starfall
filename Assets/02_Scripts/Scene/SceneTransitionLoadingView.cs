using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// 씬 전환용 로딩 UI. Slider(0~1 정규화 진행률) + 상태 문구. DontDestroyOnLoad와 함께 사용.
/// Show() 시 캔버스 sort order를 최상단으로 올려 새 씬 UI가 뒤에 그려지도록 함.
/// </summary>
public class SceneTransitionLoadingView : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] [Tooltip("진행률 0~1을 Slider min~max에 매핑. 인스펙터에서 min/max(예: 0~1 또는 0~100) 설정")]
    private Slider _progressSlider;
    [SerializeField] private TextMeshProUGUI _statusText;
    [SerializeField] [Min(0.01f)] private float _progressTweenDuration = 0.25f;

    private const int SortOrderTop = 32767;

    public void Show()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
            canvas.sortingOrder = SortOrderTop;
        if (_panel != null)
            _panel.SetActive(true);

        if (_progressSlider != null)
        {
            _progressSlider.gameObject.SetActive(true);
            _progressSlider.DOKill();
            _progressSlider.value = _progressSlider.minValue;
        }
        if (_statusText != null)
            _statusText.gameObject.SetActive(true);

        SetStatus(string.Empty);
    }

    public void Hide()
    {
        if (_panel != null)
            _panel.SetActive(false);
    }

    /// <summary>상태 문구만 표시. 예: "준비 중...", "완료"</summary>
    public void SetStatus(string status)
    {
        if (_statusText != null)
            _statusText.text = status ?? string.Empty;
    }

    /// <summary>로딩바 + 상태 갱신. progress는 0~1 정규화값 → Slider min~max로 변환. 1f는 즉시 max.</summary>
    public void UpdateProgress(float? progress, string status)
    {
        if (progress.HasValue && _progressSlider != null)
        {
            var p = Mathf.Clamp01(progress.Value);
            var min = _progressSlider.minValue;
            var max = _progressSlider.maxValue;
            var targetValue = min + (max - min) * p;

            _progressSlider.DOKill();
            if (p >= 1f - 1e-4f)
                _progressSlider.value = max;
            else
                _progressSlider.DOValue(targetValue, _progressTweenDuration).SetEase(Ease.OutQuad);
        }
        SetStatus(status ?? string.Empty);
    }
}
