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

    #endregion

    #region Public API

    /// <summary>
    /// 인벤토리 참조를 주입하고 이벤트 구독을 설정합니다.
    /// </summary>
    public void Initialize(Inventory inventory)
    {
        if(inventory == null)
        {
            Debug.LogError("인벤토리가 주입되지 않았습니다.");
            return;
        }
        _inventory = inventory;
        _inventory.OnItemChangedWithId += HandleItemChangedWithId;
    }

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (_view == null)
        {
            Debug.LogError("[LevelUpAltarPresenter] View가 할당되지 않았습니다.");
            return;
        }
        
        if (_altar == null)
        {
            Debug.LogError("[LevelUpAltarPresenter] Altar가 할당되지 않았습니다.");
            return;
        }
        
        _view.Initialize();
        _altar.OnInteracted += HandleAltarInteracted;
    }

    private void OnEnable()
    {
        _view.OnSubmitRequested += HandleSubmitRequested;
        _view.OnCloseRequested += HandleCloseRequested;
    }

    private void OnDisable()
    {
        _view.OnSubmitRequested -= HandleSubmitRequested;
        _view.OnCloseRequested -= HandleCloseRequested;
        _inventory.OnItemChangedWithId -= HandleItemChangedWithId;
        _altar.OnInteracted -= HandleAltarInteracted;
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
        
        // 메시지 초기화 후 UI 열기
        _view.SetMessage("");
        _view.RequestOpen();
        RefreshView();
    }

    /// <summary>
    /// 닫기 버튼 클릭 시 호출. UI를 닫고 상태를 초기화합니다.
    /// </summary>
    private void HandleCloseRequested()
    {
        _currentAltar = null;
        _currentCharacter = null;
        _view.SetMessage("");
        _view.RequestClose();
    }

    /// <summary>
    /// 제출(레벨업) 버튼 클릭 시 호출. 레벨업을 시도합니다.
    /// </summary>
    private void HandleSubmitRequested()
    {
        if (_currentCharacter?.Model == null || _currentAltar?.RequiredItem?.Id == null)
        {
            _view.SetMessage("잘못된 상태입니다.");
            return;
        }
        
        CharacterModel model = _currentCharacter.Model;
        ItemData requiredItem = _currentAltar.RequiredItem;
        int need = model.Level;

        // 재료 부족 대비
        if (_inventory.GetTotalCount(requiredItem.Id) < need)
        {
            _view.SetMessage("재료가 부족합니다.");
            RefreshView();
            return;
        }

        // 재료 차감 시도
        if (!_inventory.RemoveItem(requiredItem.Id, need))
        {
            _view.SetMessage("재료 차감에 실패했습니다.");
            RefreshView();
            return;
        }

        // 레벨업 시도 (실패 시 재료 복구)
        bool success = model.TryLevelUp();
        if (!success)
            _inventory.AddItem(requiredItem, need);

        _view.SetMessage(success ? "레벨업!" : "레벨업에 실패했습니다.");
        RefreshView();
    }

    /// <summary>
    /// 인벤토리 아이템 변경 시 호출. UI가 열려있으면 현재 상태를 갱신합니다.
    /// </summary>
    private void HandleItemChangedWithId(string itemId, int _)
    {
        if (!_view.gameObject.activeSelf) return;
        if (_currentAltar?.RequiredItem?.Id != itemId) return;

        RefreshView();
    }

    #endregion

    #region View Refresh

    /// <summary>
    /// UI를 현재 상태에 맞게 갱신합니다.
    /// </summary>
    private void RefreshView()
    {
        if (_currentCharacter == null || _currentCharacter.Model == null)
        {
            ResetView();
            return;
        }
        
        CharacterModel model = _currentCharacter.Model;
        CharacterData data = model.Data;
        bool isMax = model.IsMaxLevel;
        
        _view.SetCharacterPortrait(data.portrait);

        _view.SetTitle("제단");
        _view.SetBody(BuildInfoText(model, data, isMax));
        UpdateSubmitButtonState(model, isMax);
        if(isMax)
        {
            _view.SetMessage("최대 레벨");
        }
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
        StringBuilder sb = new StringBuilder();
        
        sb.AppendLine(data.displayName);

        // 최대 레벨일 경우
        if (isMax)
        {
            sb.AppendLine($"Lv {model.Level} (최대)");
        }
        else
        {
            int nextLevel = model.Level + 1;
            sb.AppendLine($"Lv {model.Level} -> {nextLevel}");

            int hpPerLevel = data.maxHpPerLevel;
            int atkPerLevel = data.attackPowerPerLevel;
            sb.AppendLine($"HP  {model.MaxHp} -> {model.MaxHp + hpPerLevel}");
            sb.AppendLine($"ATK {model.AttackPower} -> {model.AttackPower + atkPerLevel}");
        }

        ItemData requiredItem = _currentAltar.RequiredItem;
        sb.AppendLine();
        if (requiredItem == null || string.IsNullOrEmpty(requiredItem.Id))
        {
            sb.AppendLine("재료 아이템 미지정");
        }
        else
        {
            int have = _inventory.GetTotalCount(requiredItem.Id);
            int need = model.Level;
            sb.AppendLine($"{requiredItem.ItemName} {have}/{need}");
        }

        return sb.ToString();
    }

    private void UpdateSubmitButtonState(CharacterModel model, bool isMax)
    {
        ItemData requiredItem = _currentAltar.RequiredItem;
        if (requiredItem == null || string.IsNullOrEmpty(requiredItem.Id) || isMax)
        {
            _view.SetSubmitButton("레벨업", false);
            return;
        }

        if (_inventory == null)
        {
            Debug.LogError("[LevelUpAltarPresenter] Inventory is null in UpdateSubmitButtonState");
            _view.SetSubmitButton("레벨업", false);
            return;
        }

        int have = _inventory.GetTotalCount(requiredItem.Id);
        int need = model.Level;
        _view.SetSubmitButton("레벨업", have >= need);
    }

    #endregion
}
