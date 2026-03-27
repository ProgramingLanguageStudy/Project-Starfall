using System;
using System.Collections;
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
                    GameObject go = new GameObject(nameof(GameManager));
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
    private SceneLoadManager _sceneLoadManager;
    private PoolManager _poolManager;
    private EffectManager _effectManager;
    private BuffManager _buffManager;
    private SoundManager _soundManager;
    private UIManager _uiManager;

    /// <summary>Firebase 인증 래퍼. 로그인/회원가입/로그아웃, LastSnapshot·SessionChanged.</summary>
    public FirebaseAuthManager FirebaseAuthManager => _firebaseAuthManager;
    /// <summary>세이브 시점·API. ISaveContributor 등록·수집·적용, Firestore I/O.</summary>
    public SaveManager SaveManager => _saveManager;
    /// <summary>게임 데이터 preload·관리.</summary>
    public DataManager DataManager => _dataManager;
    /// <summary>프리팹 등 Addressables 로드.</summary>
    public ResourceManager ResourceManager => _resourceManager;
    /// <summary>씬 전환 (Play/Intro). Data·Resource 로드는 부트 코루틴 전용.</summary>
    public SceneLoadManager SceneLoadManager => _sceneLoadManager;
    /// <summary>오브젝트 풀링. 프리팹별 Pool 보유.</summary>
    public PoolManager PoolManager => _poolManager;
    /// <summary>이펙트 재생. Play, ShowDamageNumber. RM+Pool 연동.</summary>
    public EffectManager EffectManager => _effectManager;
    /// <summary>중앙 집중식 버프 관리.</summary>
    public BuffManager BuffManager => _buffManager;
    /// <summary>사운드 재생.</summary>
    public SoundManager SoundManager => _soundManager;
    /// <summary>전역 UI. ErrorPanel, SceneTransition 등.</summary>
    public UIManager UIManager => _uiManager;

    /// <summary>Auth 초기화·세이브 로드·DataManager·ResourceManager가 모두 끝난 뒤 true(병렬 부트 집계).</summary>
    public bool BootServicesReady { get; private set; }

    /// <summary>BootServicesReady가 true가 된 직후 한 번만 호출. Play 씬 등이 구독 가능.</summary>
    public static event Action OnBootServicesReady;

    private void Awake()
    {
        // Instance getter가 Awake보다 먼저 돌면 Find로 _instance만 채워지고, 예전 if (_instance == null) 분기에
        // 들어가지 않아 매니저가 전부 null로 남을 수 있음 → 중복만 막고 항상 자식 연결.
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        _firebaseAuthManager = this.GetOrAddComponentInChild<FirebaseAuthManager>("FirebaseAuthManager");
        _saveManager = this.GetOrAddComponentInChild<SaveManager>("SaveManager");
        _saveManager.WireFirebaseAuth(_firebaseAuthManager);
        _dataManager = this.GetOrAddComponentInChild<DataManager>("DataManager");
        _resourceManager = this.GetOrAddComponentInChild<ResourceManager>("ResourceManager");
        _sceneLoadManager = this.GetOrAddComponentInChild<SceneLoadManager>("SceneLoadManager");
        _poolManager = this.GetOrAddComponentInChild<PoolManager>("PoolManager");
        _effectManager = this.GetOrAddComponentInChild<EffectManager>("EffectManager");
        _buffManager = this.GetOrAddComponentInChild<BuffManager>("BuffManager");
        _soundManager = this.GetOrAddComponentInChild<SoundManager>("SoundManager");
        _uiManager = this.GetOrAddComponentInChild<UIManager>("UIManager");
    }

    private void Start()
    {
        if (_instance != this) return;

        StartCoroutine(AuthThenSaveBootRoutine());
        StartCoroutine(DataBootRoutine());
        StartCoroutine(ResourceBootRoutine());
        StartCoroutine(BootWatchRoutine());
    }

    /// <summary>Firebase Auth 완료 후 세이브 로드. DM·RM과 병렬로 독립 코루틴.</summary>
    private IEnumerator AuthThenSaveBootRoutine()
    {
        yield return _firebaseAuthManager.InitializeAsync();
        yield return _saveManager.LoadAsync(null);
        
        // 로드된 세이브 데이터 적용
        var loadedData = _saveManager.LoadedSaveData;
        if (loadedData != null)
        {
            _saveManager.ApplySaveData(loadedData);
            Debug.Log("[GameManager] Save data applied successfully.");
        }
        else
        {
            Debug.LogWarning("[GameManager] No save data to apply.");
        }
    }

    private IEnumerator DataBootRoutine()
    {
        yield return _dataManager.LoadAsync(null);
    }

    private IEnumerator ResourceBootRoutine()
    {
        yield return _resourceManager.LoadAsync(null);
    }

    /// <summary>네 서비스 완료 플래그를 매 프레임 검사해 한 번만 BootServicesReady 통지.</summary>
    private IEnumerator BootWatchRoutine()
    {
        while (!BootServicesReady)
        {
            bool authOk = _firebaseAuthManager == null || _firebaseAuthManager.IsInitializeComplete;
            bool saveOk = _saveManager == null || _saveManager.IsLoadComplete;
            bool dataOk = _dataManager == null || _dataManager.IsLoaded;
            bool resOk = _resourceManager == null || _resourceManager.IsLoaded();

            if (authOk && saveOk && dataOk && resOk)
            {
                BootServicesReady = true;
                OnBootServicesReady?.Invoke();
                yield break;
            }

            yield return null;
        }
    }
}
