using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이 화면 전체 UI. 체력바, 분대 프로필 등.
/// PlayScene이 보유하고 Model.OnHpChanged / SquadController.OnPlayerChanged 구독 후 갱신 요청.
/// </summary>
public class PlaySceneView : MonoBehaviour
{
    [Header("----- 체력바 (현재 조종 캐릭터) -----")]
    [SerializeField] [Tooltip("Image Type = Filled, Fill Method = Horizontal 권장")]
    private Image _healthFillImage;

    [Header("----- 분대 프로필 (최대 4명) -----")]
    [SerializeField] [Tooltip("슬롯 4개가 붙을 부모. 비면 수동 할당 _profiles 사용")]
    private Transform _profileRoot;

    private CharacterProfileView[] _profiles;
    private const int ProfileCount = 4;

    private const string CharacterProfileCategory = "UI";
    private const string CharacterProfileName = "CharacterProfile";

    /// <summary>PlayScene에서 호출. 프로필 슬롯 생성.</summary>
    public void Initialize()
    {
        CreateProfileSlots();
    }

    private void CreateProfileSlots()
    {
        if (_profiles != null) return;

        var rm = GameManager.Instance?.ResourceManager;
        if (rm == null)
        {
            Debug.LogError("[PlaySceneView] ResourceManager 없음.");
            _profiles = Array.Empty<CharacterProfileView>();
            return;
        }

        var slotPrefab = rm.GetPrefab(CharacterProfileCategory, CharacterProfileName);
        if (slotPrefab == null)
        {
            Debug.LogError($"[PlaySceneView] 필수 리소스 없음: {CharacterProfileCategory}/{CharacterProfileName}");
            _profiles = Array.Empty<CharacterProfileView>();
            return;
        }

        if (_profileRoot == null)
        {
            Debug.LogError("[PlaySceneView] _profileRoot가 할당되지 않았습니다.");
            _profiles = Array.Empty<CharacterProfileView>();
            return;
        }

        var list = new List<CharacterProfileView>(ProfileCount);
        for (int i = 0; i < ProfileCount; i++)
        {
            var go = Instantiate(slotPrefab, _profileRoot);
            go.name = $"{slotPrefab.name}_{i}";
            var view = go.GetComponent<CharacterProfileView>();
            if (view != null)
                list.Add(view);
        }
        _profiles = list.ToArray();
    }

    /// <summary>PlayScene이 Model.OnHpChanged 구독 후 호출. 현재 조종 캐릭터 체력 표시.</summary>
    public void RefreshHealth(int currentHp, int maxHp)
    {
        if (_healthFillImage == null) return;
        _healthFillImage.fillAmount = maxHp > 0 ? (float)currentHp / maxHp : 0f;
    }

    /// <summary>해당 슬롯에 캐릭터 모델 설정. 편성 화면 등에서 단일 슬롯 갱신 시 사용.</summary>
    public void SetProfileSlot(int slotIndex, CharacterModel model)
    {
        if (_profiles == null || slotIndex < 0 || slotIndex >= _profiles.Length) return;
        var view = _profiles[slotIndex];
        if (view == null) return;
        view.Initialize(model);
    }

    /// <summary>분대 프로필 슬롯을 새 데이터로 갱신.</summary>
    public void RefreshSquadProfiles(IReadOnlyList<Character> members)
    {
        if (_profiles == null || _profiles.Length == 0) return;
        for (int i = 0; i < _profiles.Length; i++)
        {
            var model = (members != null && i < members.Count && members[i] != null)
                ? members[i].Model
                : null;
            SetProfileSlot(i, model);
        }
    }

    /// <summary>현재 조종 캐릭터 프로필 강조. 인덱스가 -1이면 전부 해제.</summary>
    public void SetSelectedProfileIndex(int index)
    {
        if (_profiles == null || _profiles.Length == 0) return;
        for (int i = 0; i < _profiles.Length; i++)
        {
            var view = _profiles[i];
            if (view == null) continue;
            view.SetSelected(i == index);
        }
    }
}

