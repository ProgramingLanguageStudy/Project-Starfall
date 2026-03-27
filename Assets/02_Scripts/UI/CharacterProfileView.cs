using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 분대원 한 명의 프로필(아이콘 + HP 바 + 선택 스케일)을 담당하는 뷰.
/// 데이터(캐릭터, HP)는 PlayScene → PlaySceneView 경유로 주입받는다.
/// </summary>
public class CharacterProfileView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform _root;
    [SerializeField] private Image _iconImage;
    [SerializeField] private Image _hpFillImage;
    [SerializeField] [Tooltip("HP 바 전체 루트 (배경 포함). 비면 _hpFillImage 부모 사용")]
    private GameObject _hpRoot;

    [Header("Empty Slot")]
    [SerializeField] [Tooltip("분대원이 없을 때 표시할 기본 아이콘 (예: 빈 슬롯)")]
    private Sprite _emptyIcon;

    [Header("Selection")]
    [SerializeField] [Min(1f)] private float _selectedScale = 1.2f;
    [SerializeField] private float _scaleTweenDuration = 0.15f;
    [SerializeField] private Ease _scaleEase = Ease.OutQuad;

    private Tweener _scaleTween;
    private CharacterModel _model;

    private void Awake()
    {
        if (_root == null)
            _root = transform as RectTransform;
    }

    private void OnDestroy()
    {
        if (_model != null)
            _model.OnHpChanged -= OnHpChanged;
    }

    /// <summary>CharacterModel을 주입받아 아이콘/HP를 초기화하고 HP 이벤트를 구독.</summary>
    public void Initialize(CharacterModel model)
    {
        if (_model != null)
            _model.OnHpChanged -= OnHpChanged;

        _model = model;
        if (_model == null)
        {
            SetIcon(_emptyIcon);
            SetHealth(0, 1);
            SetHpRootVisible(false);
            return;
        }

        CharacterData data = _model.Data;
        SetIcon(data != null ? data.portrait : null);
        SetHealth(_model.CurrentHp, _model.MaxHp);
        _model.OnHpChanged += OnHpChanged;
        SetHpRootVisible(true);
    }

    private void OnHpChanged(int currentHp, int maxHp)
    {
        SetHealth(currentHp, maxHp);
    }

    public void SetIcon(Sprite sprite)
    {
        if (_iconImage == null) return;
        _iconImage.sprite = sprite;
        _iconImage.enabled = sprite != null;
    }

    /// <summary>0~1 정규화 체력값. maxHp가 0이면 0 처리.</summary>
    public void SetHealth(int currentHp, int maxHp)
    {
        if (_hpFillImage == null) return;
        _hpFillImage.fillAmount = maxHp > 0 ? (float)currentHp / maxHp : 0f;
    }

    public void SetSelected(bool isSelected)
    {
        if (_root == null) return;

        Vector3 targetScale = isSelected ? Vector3.one * _selectedScale : Vector3.one;
        _scaleTween?.Kill();
        _scaleTween = _root.DOScale(targetScale, _scaleTweenDuration)
            .SetEase(_scaleEase)
            .SetUpdate(true); // 일시정지 중에도 자연스럽게
    }

    private void SetHpRootVisible(bool visible)
    {
        if (_hpRoot != null)
        {
            _hpRoot.SetActive(visible);
            return;
        }

        if (_hpFillImage != null && _hpFillImage.transform.parent != null)
            _hpFillImage.transform.parent.gameObject.SetActive(visible);
    }
}

