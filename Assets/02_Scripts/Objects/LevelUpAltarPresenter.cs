using System.Text;
using UnityEngine;

/// <summary>
/// 레벨업 제단의 Presenter. 제단 상호작용 시 UI 표시 및 레벨업 로직 처리.
/// </summary>
public class LevelUpAltarPresenter : MonoBehaviour
{
    #region Fields

    [Header("Dependencies")]
    [SerializeField] private LevelUpAltarView _view;
    [SerializeField] private LevelUpAltar _altar;

    private Inventory _inventory;
    private LevelUpAltar _currentAltar;
    private Character _currentCharacter;
    private bool _subscribedInventory;

    #endregion

    #region Public API

    /// <summary>
    /// 인벤토리 참조를 주입하고 이벤트 구독을 설정합니다.
    /// </summary>
    public void Initialize(Inventory inventory)
    {
        if (_inventory == null && inventory != null)
            _inventory = inventory;

        SubscribeInventoryIfNeeded();
        RefreshView();
    }

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        ValidateView();
        _view?.Initialize();
        SubscribeAltarEvents();
    }

    private void OnEnable()
    {
        SubscribeViewEvents();
        SubscribeInventoryIfNeeded();
    }

    private void OnDisable()
    {
        UnsubscribeViewEvents();
        UnsubscribeInventoryIfNeeded();
        UnsubscribeAltarEvents();
    }

    #endregion

    #region Event Subscriptions

    private void SubscribeAltarEvents()
    {
        if (_altar != null)
            _altar.OnInteracted += HandleAltarInteracted;
    }

    private void UnsubscribeAltarEvents()
    {
        if (_altar != null)
            _altar.OnInteracted -= HandleAltarInteracted;
    }

    private void SubscribeViewEvents()
    {
        if (_view == null) return;
        _view.OnSubmitRequested += HandleSubmitRequested;
        _view.OnCloseRequested += HandleCloseRequested;
    }

    private void UnsubscribeViewEvents()
    {
        if (_view == null) return;
        _view.OnSubmitRequested -= HandleSubmitRequested;
        _view.OnCloseRequested -= HandleCloseRequested;
    }

    private void SubscribeInventoryIfNeeded()
    {
        if (_subscribedInventory || _inventory == null) return;
        _inventory.OnItemChangedWithId += HandleItemChangedWithId;
        _subscribedInventory = true;
    }

    private void UnsubscribeInventoryIfNeeded()
    {
        if (!_subscribedInventory || _inventory == null) return;
        _inventory.OnItemChangedWithId -= HandleItemChangedWithId;
        _subscribedInventory = false;
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// 제단 상호작용 시 호출. UI를 열고 현재 상태를 표시합니다.
    /// </summary>
    private void HandleAltarInteracted(LevelUpAltar altar, Character character)
    {
        _currentAltar = altar;
        _currentCharacter = character;
        _view?.RequestOpen();
        RefreshView();
    }

    /// <summary>
    /// 닫기 버튼 클릭 시 호출. UI를 닫고 상태를 초기화합니다.
    /// </summary>
    private void HandleCloseRequested()
    {
        _currentAltar = null;
        _currentCharacter = null;
        _view?.SetMessage("");
        _view?.RequestClose();
        RefreshView();
    }

    /// <summary>
    /// 제출(레벨업) 버튼 클릭 시 호출. 검증 후 레벨업을 시도합니다.
    /// </summary>
    private void HandleSubmitRequested()
    {
        if (!ValidateSubmitPrerequisites(out string errorMessage))
        {
            _view?.SetMessage(errorMessage);
            RefreshView();
            return;
        }

        var model = _currentCharacter.Model;
        var requiredItem = _currentAltar.RequiredItem;
        int need = model.Level;

        // 재료 차감
        if (!_inventory.RemoveItem(requiredItem.Id, need))
        {
            _view?.SetMessage("재료 차감에 실패했습니다.");
            RefreshView();
            return;
        }

        // 레벨업 시도 (실패 시 재료 복구)
        bool leveled = model.TryLevelUp();
        if (!leveled)
            _inventory.AddItem(requiredItem, need);

        _view?.SetMessage(leveled ? "레벨업!" : "레벨업에 실패했습니다.");
        RefreshView();
    }

    /// <summary>
    /// 인벤토리 아이템 변경 시 호출. UI가 열려있으면 현재 상태를 갱신합니다.
    /// </summary>
    private void HandleItemChangedWithId(string itemId, int _)
    {
        if (_view == null || !_view.IsOpen) return;
        if (_currentAltar?.RequiredItem == null) return;
        if (_currentAltar.RequiredItem.Id != itemId) return;

        RefreshView();
    }

    #endregion

    #region Validation

    /// <summary>
    /// 레벨업 제출 전 모든 필수 조건을 검증합니다.
    /// </summary>
    private bool ValidateSubmitPrerequisites(out string errorMessage)
    {
        errorMessage = null;

        if (_view == null)
        {
            errorMessage = "View가 없습니다.";
            return false;
        }
        if (_inventory == null)
        {
            errorMessage = "인벤토리를 찾을 수 없습니다.";
            return false;
        }
        if (_currentAltar == null || _currentCharacter?.Model == null)
        {
            errorMessage = "대상이 없습니다.";
            return false;
        }

        var requiredItem = _currentAltar.RequiredItem;
        if (requiredItem == null || string.IsNullOrEmpty(requiredItem.Id))
        {
            errorMessage = "재료 아이템이 설정되지 않았습니다.";
            return false;
        }

        var model = _currentCharacter.Model;
        if (IsMaxLevel(model))
        {
            errorMessage = "이미 최대 레벨입니다.";
            return false;
        }

        int need = model.Level;
        int have = _inventory.GetTotalCount(requiredItem.Id);
        if (have < need)
        {
            errorMessage = "재료가 부족합니다.";
            return false;
        }

        return true;
    }

    private void ValidateView()
    {
        if (_view == null)
            Debug.LogWarning($"[LevelUpAltarPresenter] {gameObject.name}: View가 할당되지 않았습니다.");
    }

    #endregion

    #region View Refresh

    /// <summary>
    /// UI를 현재 상태에 맞게 갱신합니다.
    /// </summary>
    private void RefreshView()
    {
        if (_view == null) return;

        if (_currentAltar == null || _currentCharacter?.Model == null)
        {
            ResetView();
            return;
        }

        var model = _currentCharacter.Model;
        var data = model.Data;
        bool isMax = IsMaxLevel(model);

        if (data?.portrait != null)
            _view.SetCharacterPortrait(data.portrait);

        _view.SetTitle("제단");
        _view.SetBody(BuildInfoText(model, data, isMax));
        UpdateSubmitButtonState(model, isMax);
        UpdateMessageState(isMax);
    }

    private void ResetView()
    {
        _view.SetTitle("제단");
        _view.SetBody("");
        _view.SetSubmitButton("레벨업", false);
    }

    /// <summary>
    /// 캐릭터 정보와 재료 상태를 표시할 텍스트를 생성합니다.
    /// </summary>
    private string BuildInfoText(CharacterModel model, CharacterData data, bool isMax)
    {
        var sb = new StringBuilder();
        sb.AppendLine(data?.displayName ?? "캐릭터");

        if (isMax)
        {
            sb.AppendLine($"Lv {model.Level} (최대)");
        }
        else
        {
            int nextLevel = model.Level + 1;
            sb.AppendLine($"Lv {model.Level} -> {nextLevel}");

            int hpPerLevel = data?.maxHpPerLevel ?? 0;
            int atkPerLevel = data?.attackPowerPerLevel ?? 0;
            sb.AppendLine($"HP  {model.MaxHp} -> {model.MaxHp + hpPerLevel}");
            sb.AppendLine($"ATK {model.AttackPower} -> {model.AttackPower + atkPerLevel}");
        }

        var requiredItem = _currentAltar.RequiredItem;
        sb.AppendLine();
        if (requiredItem != null && !string.IsNullOrEmpty(requiredItem.Id))
        {
            int have = _inventory?.GetTotalCount(requiredItem.Id) ?? 0;
            int need = model.Level;
            sb.AppendLine($"{requiredItem.ItemName} {have}/{need}");
        }
        else
        {
            sb.AppendLine("재료 아이템 미지정");
        }

        return sb.ToString();
    }

    private void UpdateSubmitButtonState(CharacterModel model, bool isMax)
    {
        var requiredItem = _currentAltar.RequiredItem;
        if (requiredItem == null || string.IsNullOrEmpty(requiredItem.Id) || isMax || _inventory == null)
        {
            _view.SetSubmitButton("레벨업", false);
            return;
        }

        int have = _inventory.GetTotalCount(requiredItem.Id);
        int need = model.Level;
        _view.SetSubmitButton("레벨업", have >= need);
    }

    private void UpdateMessageState(bool isMax)
    {
        if (isMax)
            _view.SetMessage("최대 레벨입니다.");
    }

    #endregion

    #region Utility

    /// <summary>
    /// 캐릭터가 최대 레벨에 도달했는지 확인합니다.
    /// </summary>
    private static bool IsMaxLevel(CharacterModel model)
    {
        if (model?.Data == null) return false;
        int maxLevel = model.Data.maxLevel > 0 ? model.Data.maxLevel : int.MaxValue;
        return model.Level >= maxLevel;
    }

    #endregion
}
