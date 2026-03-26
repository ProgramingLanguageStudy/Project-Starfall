using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 인벤토리 UI 표시만 담당 (MVP의 View). Model을 알지 않으며, Presenter가 전달한 슬롯 데이터로 그립니다.
/// </summary>
public class InventoryView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject _inventoryUIPanel;
    [SerializeField] private RectTransform _mainPanelRect; // 연출용 메인 패널
    [SerializeField] private CanvasGroup _mainCanvasGroup;   // 연출용 Canvas에 달린 CanvasGroup
    [SerializeField] [Tooltip("인벤토리 열 때 맨 위로 보이게 할 ScrollRect. 비면 동작 안 함")]
    private ScrollRect _scrollRect;
    [SerializeField] private Image _dragIcon;
    [SerializeField] private GameObject _itemSlotPrefab;
    [SerializeField] private Transform _slotGroup;
    [SerializeField] private int _maxSlotCount = 30;
    [SerializeField] [Tooltip("오른쪽 상세 패널(아이템 설명 + USE 버튼). 인스펙터에서 할당.")]
    private InventoryTooltipView _tooltipView;
    [SerializeField] private UITweenFacade _inventoryFacade;
    [SerializeField] [Tooltip("인벤토리 닫기(X) 버튼. 누르면 패널 닫힘.")]
    private Button _closeButton;
    
    [Header("재화 UI")]
    [SerializeField] private TextMeshProUGUI _goldText;
    [SerializeField] private string _goldFormat = "{0:N0}";

    private List<ItemSlot> _slots = new List<ItemSlot>();

    /// <summary>인벤토리 패널이 켜져 있는지. PlayScene 등에서 커서/이동 제어용.</summary>
    public bool IsPanelActive => (_inventoryFacade != null ? _inventoryFacade.gameObject : _inventoryUIPanel)?.activeSelf ?? false;

    /// <summary>드롭 끝났을 때 (fromIndex, 스크린 좌표). Presenter가 GetSlotIndexAtPosition으로 목표 찾아 스왑.</summary>
    public event Action<int, Vector2> OnDropEnded;
    /// <summary>상세 패널 USE 버튼으로 아이템 사용 요청 시 (slotIndex). Presenter가 구독해 Model.TryUseItem 호출.</summary>
    public event Action<int> OnUseItemRequested;

    /// <summary>토글 시 Presenter가 구독해 Refresh 호출 유도.</summary>
    public event Action OnRefreshRequested;

    /// <summary>슬롯 생성·패널·툴팁 초기 상태. Presenter가 호출.</summary>
    public void Initialize()
    {
        CreateSlots();
        var panel = _inventoryFacade != null ? _inventoryFacade.gameObject : _inventoryUIPanel;
        if (panel != null)
            panel.SetActive(false);
        _tooltipView?.Initialize();
        if (_tooltipView != null)
            _tooltipView.OnUseRequested += ForwardUseItemRequested;
        if (_closeButton != null)
            _closeButton.onClick.AddListener(CloseInventory);
    }

    private void OnDestroy()
    {
        if (_tooltipView != null)
            _tooltipView.OnUseRequested -= ForwardUseItemRequested;
        if (_closeButton != null)
            _closeButton.onClick.RemoveListener(CloseInventory);
    }

    /// <summary>닫기(X) 버튼용. 패널이 켜져 있을 때만 닫음.</summary>
    private void CloseInventory()
    {
        if (!IsPanelActive) return;
        ToggleInventory();
    }

    private void ForwardUseItemRequested(int slotIndex) => OnUseItemRequested?.Invoke(slotIndex);

    /// <summary>슬롯을 한 번에 생성. 코루틴으로 나누면 패널이 먼저 꺼질 때 중단돼 30개 미만이 될 수 있음.</summary>
    private void CreateSlots()
    {
        for (int i = 0; i < _maxSlotCount; i++)
        {
            var slotGo = Instantiate(_itemSlotPrefab, _slotGroup);
            var slot = slotGo.GetComponent<ItemSlot>();
            slot.SetIndex(i);
            slot.OnDropEnded += (from, pos) => OnDropEnded?.Invoke(from, pos);
            slot.OnSlotSelected += (slotModel) =>
            {
                for (int i = 0; i < _slots.Count; i++)
                    _slots[i].SetSelected(i == slotModel.Index);
                _tooltipView?.Show(slotModel);
            };
            slot.OnDragStarted += () =>
            {
                if (_tooltipView != null)
                    _tooltipView.Hide();
            };
            slot.SetDragIcon(_dragIcon);
            slot.ClearSlot();
            _slots.Add(slot);
        }
    }

    /// <summary>Presenter에서 호출. 해당 슬롯에 Model을 넣고 갱신. 해당 슬롯이 비면 툴팁이 그 슬롯을 보여주고 있을 때 툴팁 닫음.</summary>
    public void RefreshSlot(ItemSlotModel slotModel)
    {
        if (slotModel == null || slotModel.Index < 0 || slotModel.Index >= _slots.Count) return;
        _slots[slotModel.Index].SetModel(slotModel);
        bool slotNowEmpty = slotModel.Item == null || slotModel.Count <= 0;
        if (slotNowEmpty && _tooltipView != null && _tooltipView.ShownSlotIndex == slotModel.Index)
            _tooltipView.Hide();
    }

    /// <summary>스크린 좌표가 속한 슬롯 인덱스. 없으면 -1. 스크롤 반영됨.</summary>
    public int GetSlotIndexAtPosition(Vector2 screenPos)
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            var rt = _slots[i].transform as RectTransform;
            if (rt == null) continue;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPos, null, out var local))
            {
                if (rt.rect.Contains(local))
                    return i;
            }
        }
        return -1;
    }

    public void ToggleInventory()
    {
        var panel = _inventoryFacade != null ? _inventoryFacade.gameObject : _inventoryUIPanel;
        if (panel == null) return;

        bool isOpening = !panel.activeSelf;

        if (isOpening)
        {
            if (_inventoryFacade != null)
                _inventoryFacade.PlayEnter();
            else
                _inventoryUIPanel.SetActive(true);

            _tooltipView?.Hide();
            if (_scrollRect != null)
                _scrollRect.verticalNormalizedPosition = 1f;

            OnRefreshRequested?.Invoke();
            GameEvents.OnCursorShowRequested?.Invoke();
        }
        else
        {
            _tooltipView?.Hide();
            for (int i = 0; i < _slots.Count; i++)
                _slots[i].SetSelected(false);

            GameEvents.OnCursorHideRequested?.Invoke();

            if (_inventoryFacade != null)
                _inventoryFacade.PlayExit();
            else
                _inventoryUIPanel.SetActive(false);
        }
    }
    
    /// <summary>골드 표시 갱신. Presenter에서 호출.</summary>
    public void RefreshGold(int gold)
    {
        if (_goldText != null)
            _goldText.text = string.Format(_goldFormat, gold);
    }
}
