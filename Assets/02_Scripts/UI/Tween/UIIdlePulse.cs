using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 아이들 시 스케일 펄스(필수) + 아웃라인 알파 펄스(선택). TweenPreset(IdlePulsePreset) 사용.
/// 버튼 등에 붙여 두면 가만히 있을 때 "누르고 싶게" 만드는 연출.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UIIdlePulse : MonoBehaviour
{
    [Header("Preset")]
    [SerializeField] private bool _usePreset = true;
    [SerializeField] private bool _useGlowPreset = false;

    [Header("Custom (Use Preset 해제 시)")]
    [SerializeField] private IdlePulsePresetData _custom = new()
    {
        ScaleMin = 1f,
        ScaleMax = 1.06f,
        Duration = 0.9f,
        Ease = Ease.InOutSine
    };

    [Header("Outline (글로우 펄스, 선택)")]
    [SerializeField] private Outline _outline;

    private RectTransform _rect;
    private Tweener _scaleTween;
    private Tweener _outlineTween;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        if (_outline == null)
            _outline = GetComponent<Outline>();
    }

    private void OnEnable()
    {
        IdlePulsePresetData data = GetData();
        Vector3 scaleMin = Vector3.one * data.ScaleMin;
        Vector3 scaleMax = Vector3.one * data.ScaleMax;

        _rect.localScale = scaleMin;
        _scaleTween = _rect.DOScale(scaleMax, data.Duration * 0.5f)
            .SetEase(data.Ease)
            .SetLoops(-1, LoopType.Yoyo)
            .SetTarget(this);

        if (_outline != null && data.OutlineAlphaMin < data.OutlineAlphaMax)
        {
            Color c = _outline.effectColor;
            _outline.effectColor = new Color(c.r, c.g, c.b, data.OutlineAlphaMin);
            _outlineTween = DOTween.To(
                    () => _outline.effectColor.a,
                    a => _outline.effectColor = new Color(c.r, c.g, c.b, a),
                    data.OutlineAlphaMax,
                    data.Duration * 0.5f)
                .SetEase(data.Ease)
                .SetLoops(-1, LoopType.Yoyo)
                .SetTarget(this);
        }
    }

    private void OnDisable()
    {
        DOTween.Kill(this);
        _scaleTween = null;
        _outlineTween = null;
    }

    private IdlePulsePresetData GetData()
    {
        if (_usePreset)
            return _useGlowPreset ? IdlePulsePreset.WithGlow : IdlePulsePreset.Data;
        return _custom;
    }
}
