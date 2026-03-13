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
    /// <param name="onComplete">연출 완료 후 호출. Title은 등장+Punch 후 호출.</param>
    public void PlayEnter(Action onComplete = null)
    {
        EnsureCached();
        if (controlActive)
            gameObject.SetActive(true);

        var p = GetActivePreset();
        _rect.localScale = p.ScaleFrom;
        _canvasGroup.alpha = p.AlphaFrom;

        var seq = DOTween.Sequence();
        seq.Join(_rect.DOScale(p.ScaleTo, p.Duration).SetEase(p.Ease));
        seq.Join(_canvasGroup.DOFade(p.AlphaTo, p.Duration).SetEase(p.Ease));

        if (useDefaultPreset && role == UIRole.Title)
        {
            seq.Append(_rect.DOPunchScale(Vector3.one * TitlePreset.PunchStrength, TitlePreset.PunchDuration, 4, 0.5f));
        }

        seq.OnComplete(() => onComplete?.Invoke());
        seq.Play();
    }

    /// <summary>퇴장. 연출 후 controlActive면 SetActive(false).</summary>
    /// <param name="onComplete">퇴장 완료 후 호출.</param>
    public void PlayExit(Action onComplete = null)
    {
        EnsureCached();
        var p = GetActivePreset();
        _rect.localScale = p.ScaleTo;
        _canvasGroup.alpha = p.AlphaTo;

        var seq = DOTween.Sequence();
        seq.Join(_rect.DOScale(p.ScaleFrom, p.Duration).SetEase(p.Ease));
        seq.Join(_canvasGroup.DOFade(p.AlphaFrom, p.Duration).SetEase(p.Ease));
        seq.OnComplete(() =>
        {
            if (controlActive)
                gameObject.SetActive(false);
            onComplete?.Invoke();
        });
        seq.Play();
    }
}
