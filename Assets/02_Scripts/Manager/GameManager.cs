using UnityEngine;

/// <summary>
/// 게임 전역에서 유일한 싱글톤. 씬 전환 후에도 유지되며, 다른 매니저를 보유합니다.
/// 접근: GameManager.Instance.SaveManager, GameManager.Instance.DataManager
/// </summary>
public class GameManager : Singleton<GameManager>
{
    [Header("Managers (선택: 인스펙터 할당. 없으면 런타임 생성)")]
    [SerializeField] private SaveManager _saveManager;
    [SerializeField] private DataManager _dataManager;
    [SerializeField] private CurrencyManager _currencyManager;
    [SerializeField] private GlobalSaveCoordinator _globalSaveCoordinator;

    /// <summary>세이브 시점·API. ISaveHandler 등록·수집·적용, SaveSystem I/O.</summary>
    public SaveManager SaveManager => GetOrCreate(ref _saveManager, "SaveManager");
    /// <summary>게임 데이터 preload·관리.</summary>
    public DataManager DataManager => GetOrCreate(ref _dataManager, "DataManager");
    /// <summary>계정 귀속 재화(골드) 관리.</summary>
    public CurrencyManager CurrencyManager => GetOrCreate(ref _currencyManager, "CurrencyManager");
    /// <summary>계정 귀속 데이터(골드 등) 세이브 조율.</summary>
    public GlobalSaveCoordinator GlobalSaveCoordinator => GetOrCreate(ref _globalSaveCoordinator, "GlobalSaveCoordinator");

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
        var _ = SaveManager;
        var __ = DataManager;
        var ___ = CurrencyManager;
        var ____ = GlobalSaveCoordinator;
        DataManager.Initialize();
    }

    /// <summary>매니저가 없으면 자식 오브젝트에서 찾거나, 없으면 생성해서 붙임.</summary>
    private T GetOrCreate<T>(ref T field, string childName) where T : MonoBehaviour
    {
        if (field != null) return field;

        var existing = GetComponentInChildren<T>(true);
        if (existing != null)
        {
            field = existing;
            return field;
        }

        var go = new GameObject(childName);
        go.transform.SetParent(transform);
        field = go.AddComponent<T>();
        return field;
    }
}
