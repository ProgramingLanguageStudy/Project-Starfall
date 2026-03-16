using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// 씬 전환용 로딩 UI. 로딩바(퍼센트 시각화) + 상태 문구. DontDestroyOnLoad와 함께 사용.
/// Show() 시 캔버스 sort order를 최상단으로 올려 새 씬 UI가 뒤에 그려지도록 함.
/// </summary>
public class SceneTransitionLoadingView : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] [Tooltip("로딩바 배경+채우기+상태텍스트 전체 부모. 비면 Fill·Status만 개별 제어")]
    private GameObject _loadingUIRoot;
    [SerializeField] private Image _progressBarFill;
    [SerializeField] private TextMeshProUGUI _statusText;
    [SerializeField] [Min(0.01f)] private float _progressTweenDuration = 0.25f;

    private const int SortOrderTop = 32767;

    /// <summary>
    /// 전환 뷰 표시.
    /// 기본값(showLoadingUI = true)은 로딩바+텍스트까지 보이고,
    /// false 면 화면 가리기용 마스크만 보이도록 한다.
    /// </summary>
    public void Show(bool showLoadingUI = true)
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
            canvas.sortingOrder = SortOrderTop;
        if (_panel != null)
            _panel.SetActive(true);

        SetLoadingUIVisible(showLoadingUI);

        if (showLoadingUI && _progressBarFill != null)
        {
            _progressBarFill.DOKill();
            _progressBarFill.fillAmount = 0f;
        }
        if (showLoadingUI)
            SetStatus(string.Empty);
    }

    public void Hide()
    {
        if (_panel != null)
            _panel.SetActive(false);
    }

    /// <summary>상태 문구만 표시(퍼센트 숫자 없음). 예: "준비 중...", "완료"</summary>
    public void SetStatus(string status)
    {
        if (_statusText != null)
            _statusText.text = status ?? string.Empty;
    }

    /// <summary>로딩바 + 상태 갱신. progress 0~1은 바에 트윈, status는 문구만 표시.</summary>
    public void UpdateProgress(float? progress, string status)
    {
        // 마스크 전용 모드(로딩 UI 꺼짐)에서는 갱신 안 함
        if (_loadingUIRoot != null && !_loadingUIRoot.activeSelf)
            return;
        if (_loadingUIRoot == null && _progressBarFill != null && !_progressBarFill.gameObject.activeSelf &&
            _statusText != null && !_statusText.gameObject.activeSelf)
            return;

        if (progress.HasValue && _progressBarFill != null)
        {
            var p = Mathf.Clamp01(progress.Value);
            _progressBarFill.DOKill();
            _progressBarFill.DOFillAmount(p, _progressTweenDuration).SetEase(Ease.OutQuad);
        }
        SetStatus(status ?? string.Empty);
    }

    private void SetLoadingUIVisible(bool visible)
    {
        if (_loadingUIRoot != null)
        {
            _loadingUIRoot.SetActive(visible);
            return;
        }
        if (_progressBarFill != null)
            _progressBarFill.gameObject.SetActive(visible);
        if (_statusText != null)
            _statusText.gameObject.SetActive(visible);
    }
}
