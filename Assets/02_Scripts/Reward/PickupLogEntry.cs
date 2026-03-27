using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>획득 로그 한 항목. 아이콘 + x수량 표시 후 일정 시간 뒤 페이드 아웃하며 제거.</summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class PickupLogEntry : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _descText;

    [Tooltip("표시 시간(초). 이후 페이드 아웃.")]
    [SerializeField] private float _displayDuration = 2.5f;
    [Tooltip("사라지는 페이드 시간(초).")]
    [SerializeField] private float _fadeOutDuration = 0.3f;

    [Tooltip("골드 아이콘 스프라이트")]
    [SerializeField] private Sprite _goldIcon;

    private CanvasGroup _canvasGroup;
    private Poolable _poolable;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        _poolable = GetComponent<Poolable>();
    }

    private void OnEnable()
    {
        // 풀에서 재사용될 때 초기화
        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;
    }

    /// <summary>아이템 데이터와 수량으로 표시하고, displayDuration 후 퇴장 연출 후 제거.</summary>
    public void Show(ItemData itemData, int amount)
    {
        if (_icon != null && itemData != null)
            _icon.sprite = itemData.Icon;
        if (_descText != null)
            _descText.text = itemData.ItemName + " x " + amount;

        StopAllCoroutines();
        StartCoroutine(DisplayThenExit());
    }

    /// <summary>골드 획득 표시.</summary>
    public void ShowGold(int amount)
    {
        if (_icon != null)
        {
            if (_goldIcon != null)
                _icon.sprite = _goldIcon;
            else
                _icon.sprite = null; // 아이콘이 없으면 비워둠
        }
        if (_descText != null)
            _descText.text = "Gold +" + amount;

        StopAllCoroutines();
        StartCoroutine(DisplayThenExit());
    }

    private IEnumerator DisplayThenExit()
    {
        // 등장: 즉시 표시
        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(_displayDuration);

        // 퇴장: 페이드 아웃
        float elapsed = 0f;
        while (elapsed < _fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / _fadeOutDuration);
            yield return null;
        }

        OnExitComplete();
    }

    private void OnExitComplete()
    {
        if (_poolable != null)
            _poolable.ReturnToPool();
        else
            Destroy(gameObject);
    }
}
