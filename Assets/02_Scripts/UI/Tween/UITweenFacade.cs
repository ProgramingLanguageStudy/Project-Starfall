using System;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// UI 등장/퇴장 퍼사드. 프리셋(Panel/Toast) 또는 커스텀 수치로 Scale+Alpha 연출.
/// controlActive면 SetActive도 담당.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class UITweenFacade : MonoBehaviour
{
    [Header("Visibility")]
    [Tooltip("true면 등장/퇴장 시 SetActive도 제어. false면 연출만.")]
    [SerializeField] bool controlActive = true;

    [Header("Preset")]
    [Tooltip("체크 시 Role 기본 프리셋 사용. 해제 시 아래 커스텀 수치 사용.")]
    [SerializeField] bool useDefaultPreset = true;
    [SerializeField] UIRole role = UIRole.Panel;

    [Header("Custom (Use Default 체크 해제 시 적용)")]
    [SerializeField] float customDuration = 0.25f;
    [SerializeField] Ease customEase = Ease.OutQuad;
    [SerializeField] Vector3 customScaleFrom = new Vector3(0.9f, 0.9f, 1f);
    [SerializeField] Vector3 customScaleTo = Vector3.one;
    [SerializeField] float customAlphaFrom = 0f;
    [SerializeField] float customAlphaTo = 1f;

    RectTransform _rect;
    CanvasGroup _canvasGroup;
    Tween _currentTween;

    /// <summary>현재 연출 중인지 여부</summary>
    public bool IsPlaying => _currentTween != null && _currentTween.IsActive();
    
    /// <summary>연출 중인지 내부 상태로 추적</summary>
    private bool _isInTransition = false;

    void Awake()
    {
        EnsureCached();
    }

    /// <summary>비활성 오브젝트는 Awake가 늦게 호출될 수 있음. PlayEnter/Exit 전 보장.</summary>
    void EnsureCached()
    {
        if (_rect == null)
        {
            _rect = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    /// <summary>이전 Tween 중지</summary>
    void KillCurrentTween()
    {
        if (_currentTween != null && _currentTween.IsActive())
        {
            _currentTween.Kill();
            _currentTween = null;
        }
        _isInTransition = false;  // 연출 상태 해제
    }

    /// <summary>현재 연출 즉시 중단</summary>
    public void Stop()
    {
        KillCurrentTween();
    }

    UIPresetData GetActivePreset()
    {
        if (useDefaultPreset)
        {
            return role switch
            {
                UIRole.Title => TitlePreset.Data,
                UIRole.Panel => PanelPreset.Data,
                UIRole.Toast => ToastPreset.Data,
                _ => PanelPreset.Data
            };
        }
        return new UIPresetData
        {
            Duration = customDuration,
            Ease = customEase,
            ScaleFrom = customScaleFrom,
            ScaleTo = customScaleTo,
            AlphaFrom = customAlphaFrom,
            AlphaTo = customAlphaTo
        };
    }

    /// <summary>등장. controlActive면 SetActive(true) 후 연출.</summary>
    /// <param name="onComplete">연출 완료 후 호출. Title은 등장+Punch 후 호출.</summary>
    public void PlayEnter(Action onComplete = null)
    {
        EnsureCached();
        
        // 연출 중일 때 UIRole별 다른 동작
        if (_isInTransition) 
        {
            if (role == UIRole.Toast)
            {
                // Toast는 즉시 종료 후 새로 시작
                Stop();
            }
            else
            {
                // 다른 UI는 무시
                onComplete?.Invoke();
                return;
            }
        }
        
        // 이미 열려있고 연출 중이 아니면 아무것도 안 함
        if (gameObject.activeSelf && !IsPlaying) 
        {
            onComplete?.Invoke();
            return;
        }
        
        // 연출 상태 설정
        _isInTransition = true;
        
        if (controlActive)
            gameObject.SetActive(true);

        UIPresetData p = GetActivePreset();
        _rect.localScale = p.ScaleFrom;
        _canvasGroup.alpha = p.AlphaFrom;

        Sequence seq = DOTween.Sequence();
        seq.Join(_rect.DOScale(p.ScaleTo, p.Duration).SetEase(p.Ease));
        seq.Join(_canvasGroup.DOFade(p.AlphaTo, p.Duration).SetEase(p.Ease));

        if (useDefaultPreset && role == UIRole.Title)
        {
            seq.Append(_rect.DOPunchScale(Vector3.one * TitlePreset.PunchStrength, TitlePreset.PunchDuration, 4, 0.5f));
        }

        seq.OnComplete(() => {
            _currentTween = null;
            _isInTransition = false;  // 연출 상태 해제
            onComplete?.Invoke();
        });
        
        _currentTween = seq;
        seq.Play();
    }

    /// <summary>퇴장. 연출 후 controlActive면 SetActive(false).</summary>
    /// <param name="onComplete">퇴장 완료 후 호출.</param>
    public void PlayExit(Action onComplete = null)
    {
        EnsureCached();
        
        // 연출 중이면 무시
        if (_isInTransition) 
        {
            onComplete?.Invoke();
            return;
        }
        
        // 이미 닫혀있고 연출 중이 아니면 아무것도 안 함
        if (!gameObject.activeSelf && !IsPlaying) 
        {
            onComplete?.Invoke();
            return;
        }
        
        // 연출 상태 설정
        _isInTransition = true;
        
        UIPresetData p = GetActivePreset();
        _rect.localScale = p.ScaleTo;
        _canvasGroup.alpha = p.AlphaTo;

        Sequence seq = DOTween.Sequence();
        seq.Join(_rect.DOScale(p.ScaleFrom, p.Duration).SetEase(p.Ease));
        seq.Join(_canvasGroup.DOFade(p.AlphaFrom, p.Duration).SetEase(p.Ease));
        seq.OnComplete(() =>
        {
            if (controlActive)
                gameObject.SetActive(false);
            _currentTween = null;
            _isInTransition = false;  // 연출 상태 해제
            onComplete?.Invoke();
        });
        
        _currentTween = seq;
        seq.Play();
    }

    private void OnDestroy()
    {
        KillCurrentTween();
    }
}
