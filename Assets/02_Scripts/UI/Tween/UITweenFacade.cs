using System;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// UI 등장/퇴장 퍼사드. 자기(및 자식)의 IUITweenBase를 찾아 PlayEnter/PlayExit로 일괄 재생.
/// controlActive가 true면 SetActive도 담당. Tween 없으면 즉시 SetActive만 수행.
/// </summary>
public class UITweenFacade : MonoBehaviour
{
    [Header("Auto")]
    [SerializeField] bool playEnterOnStart;

    [Header("Visibility")]
    [Tooltip("true면 등장/퇴장 시 SetActive도 제어. false면 연출만.")]
    [SerializeField] bool controlActive = true;

    IUITweenBase[] _tweens;

    void Awake()
    {
        RefreshTweens();
    }

    void Start()
    {
        if (playEnterOnStart)
            PlayEnter();
    }

    /// <summary>수집 후 재생 전에 호출 가능. Runtime에 자식이 바뀌면 재호출.</summary>
    public void RefreshTweens()
    {
        _tweens = GetComponentsInChildren<IUITweenBase>(true);
    }

    /// <summary>등장. controlActive면 SetActive(true) 후 연출. Tween 없으면 SetActive만.</summary>
    public void PlayEnter()
    {
        if (controlActive)
            gameObject.SetActive(true);

        if (_tweens == null || _tweens.Length == 0)
            return;

        var seq = DOTween.Sequence();
        foreach (var t in _tweens)
        {
            if (t != null)
                seq.Join(t.PlayForward());
        }
        seq.Play();
    }

    /// <summary>퇴장. 연출 후 controlActive면 SetActive(false). Tween 없으면 즉시 SetActive(false).</summary>
    /// <param name="onComplete">퇴장 완료 후 호출 (연출 있으면 연출 끝난 뒤, 없으면 즉시).</param>
    public void PlayExit(Action onComplete = null)
    {
        if (_tweens == null || _tweens.Length == 0)
        {
            if (controlActive)
                gameObject.SetActive(false);
            onComplete?.Invoke();
            return;
        }

        var seq = DOTween.Sequence();
        foreach (var t in _tweens)
        {
            if (t != null)
                seq.Join(t.PlayReverse());
        }
        seq.OnComplete(() =>
        {
            if (controlActive)
                gameObject.SetActive(false);
            onComplete?.Invoke();
        });
        seq.Play();
    }
}
