using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>획득 알림 한 줄. 아이콘 + x수량 표시 후 일정 시간 뒤 페이드 아웃하며 제거.</summary>
public class ItemPickupSlot : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _descText;

    [Tooltip("표시 시간(초). 이후 페이드 아웃.")]
    [SerializeField] private float _displayDuration = 2.5f;
    [Tooltip("사라지는 페이드 시간(초).")]
    [SerializeField] private float _fadeOutDuration = 0.3f;

    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    /// <summary>아이템 데이터와 수량으로 표시하고, displayDuration 후 페이드 아웃 후 제거.</summary>
    public void Show(ItemData itemData, int amount)
    {
        if (_icon != null && itemData != null)
            _icon.sprite = itemData.Icon;
        if (_descText != null)
            _descText.text = itemData.ItemName + " x " + amount;

        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;

        StopAllCoroutines();
        StartCoroutine(DisplayThenFadeOut());
    }

    /// <summary>골드 획득 표시.</summary>
    public void ShowGold(int amount)
    {
        if (_icon != null)
            _icon.sprite = null; // 골드 아이콘은 별도 지정 가능
        if (_descText != null)
            _descText.text = "Gold +" + amount;

        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;

        StopAllCoroutines();
        StartCoroutine(DisplayThenFadeOut());
    }

    private IEnumerator DisplayThenFadeOut()
    {
        yield return new WaitForSeconds(_displayDuration);

        float elapsed = 0f;
        while (elapsed < _fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / _fadeOutDuration);
            yield return null;
        }

        Destroy(gameObject);
    }
}
