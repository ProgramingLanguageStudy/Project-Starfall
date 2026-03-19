using UnityEngine;

/// <summary>
/// 게임 전역에서 유일한 싱글톤. 씬 전환 후에도 유지되며, 다른 매니저를 보유합니다.
/// 접근: GameManager.Instance.SaveManager, GameManager.Instance.DataManager
/// </summary>
public class GameManager : MonoBehaviour
{
    private static GameManager _instance;

    /// <summary>전역 접근. 씬에 없으면 찾고, 없으면 런타임에 생성.</summary>
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<GameManager>();
                if (_instance == null && Application.isPlaying)
                {
                    var go = new GameObject(nameof(GameManager));
                    _instance = Util.GetOrAddComponent<GameManager>(go);
                }
            }
            return _instance;
        }
    }

    private FirebaseAuthManager _firebaseAuthManager;
    private SaveManager _saveManager;
    private DataManager _dataManager;
    private ResourceManager _resourceManager;
    private CurrencyManager _currencyManager;
    private SceneLoadManager _sceneLoadManager;
    private PoolManager _poolManager;
    private EffectManager _effectManager;
    private SoundManager _soundManager;
    private UIManager _uiManager;

    /// <summary>Firebase 인증 래퍼. 로그인/회원가입/로그아웃, IsLoggedIn, UserUID 등.</summary>
    public FirebaseAuthManager FirebaseAuthManager => _firebaseAuthManager;
    /// <summary>세이브 시점·API. ISaveContributor 등록·수집·적용, Firestore I/O.</summary>
    public SaveManager SaveManager => _saveManager;
    /// <summary>게임 데이터 preload·관리.</summary>
    public DataManager DataManager => _dataManager;
    /// <summary>프리팹 등 Addressables 로드.</summary>
    public ResourceManager ResourceManager => _resourceManager;
    /// <summary>씬 전환 로딩 (DataManager, ResourceManager, Play).</summary>
    public SceneLoadManager SceneLoadManager => _sceneLoadManager;
    /// <summary>계정 귀속 재화(골드) 관리.</summary>
    public CurrencyManager CurrencyManager => _currencyManager;
    /// <summary>오브젝트 풀링. 프리팹별 Pool 보유.</summary>
    public PoolManager PoolManager => _poolManager;
    /// <summary>이펙트 재생. Play, ShowDamageNumber. RM+Pool 연동.</summary>
    public EffectManager EffectManager => _effectManager;
    /// <summary>사운드 재생.</summary>
    public SoundManager SoundManager => _soundManager;
    /// <summary>전역 UI. ErrorPanel, SceneTransition 등.</summary>
    public UIManager UIManager => _uiManager;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);

            _firebaseAuthManager = this.GetOrAddComponentInChild<FirebaseAuthManager>("FirebaseAuthManager");
            _saveManager = this.GetOrAddComponentInChild<SaveManager>("SaveManager");
            SaveBackendProvider.BootCompleted = true;
            _saveManager.SetBackend(SaveBackendProvider.CreateBackend());
            _dataManager = this.GetOrAddComponentInChild<DataManager>("DataManager");
            _resourceManager = this.GetOrAddComponentInChild<ResourceManager>("ResourceManager");
            _sceneLoadManager = this.GetOrAddComponentInChild<SceneLoadManager>("SceneLoadManager");
            _currencyManager = this.GetOrAddComponentInChild<CurrencyManager>("CurrencyManager");
            _poolManager = this.GetOrAddComponentInChild<PoolManager>("PoolManager");
            _effectManager = this.GetOrAddComponentInChild<EffectManager>("EffectManager");
            _soundManager = this.GetOrAddComponentInChild<SoundManager>("SoundManager");
            _uiManager = this.GetOrAddComponentInChild<UIManager>("UIManager");
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (_instance != this) return;

        _currencyManager?.Initialize();
    }
}
