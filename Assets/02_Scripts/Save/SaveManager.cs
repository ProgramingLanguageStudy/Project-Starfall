using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 세이브 시점 제어 및 API 제공. ISaveHandler 등록·수집·적용.
/// 백엔드: 로그인 시 Firestore, 미로그인/에러 시 로컬 파일(Application.persistentDataPath).
/// - Play 진입 시: LoadAsync 후 ApplySaveData.
/// - 앱 종료: Application.wantsToQuit으로 저장 완료 대기.
/// - 5분 주기 자동 저장.
/// </summary>
public class SaveManager : MonoBehaviour
{
    private const float PeriodicSaveIntervalSec = 300f; // 5분

    [Header("로컬 세이브 (Boot 미경유 시)")]
    [SerializeField] [Tooltip("에디터 Play 시에만 사용. 빌드에서는 항상 persistentDataPath. 예: 프로젝트/SaveData")]
    private string _localSaveFolder = "";

    private readonly List<ISaveHandler> _handlers = new List<ISaveHandler>();
    private FirestoreSaveBackend _firestoreBackend;
    private static readonly LocalSaveBackend _localBackend = new LocalSaveBackend();

    /// <summary>Boot 씬 경유 여부. Play 직접 진입 시 false → 로컬 사용.</summary>
    private static bool _bootCompleted;
    private static bool _backendLogged;

    /// <summary>Boot 씬에서 호출. Boot 경유 시 Firestore 사용 가능.</summary>
    public static void MarkBootCompleted() => _bootCompleted = true;

    private void Awake()
    {
#if UNITY_EDITOR
        if (!string.IsNullOrWhiteSpace(_localSaveFolder))
            LocalSaveBackend.CustomBasePath = _localSaveFolder.Trim();
        else
            LocalSaveBackend.CustomBasePath = null;
#else
        LocalSaveBackend.CustomBasePath = null; // 빌드에서는 항상 persistentDataPath
#endif
    }
    private Coroutine _periodicSaveCoroutine;
    private bool _isQuittingAfterSave;

    private ISaveBackend Backend
    {
        get
        {
            // Play 직접 진입(Boot 미경유) 시 로컬만 사용. Firebase Auth 유지와 무관.
            if (!_bootCompleted)
            {
                if (!_backendLogged) { _backendLogged = true; Debug.Log("[SaveManager] Boot 미경유 → 로컬"); }
                return _localBackend;
            }

            try
            {
                var user = FirebaseAuth.DefaultInstance?.CurrentUser;
                if (user != null)
                {
                    if (!_backendLogged) { _backendLogged = true; Debug.Log("[SaveManager] Boot 경유 + 로그인 → Firestore"); }
                    return _firestoreBackend ??= new FirestoreSaveBackend(user.UserId);
                }
            }
            catch (System.Exception e)
            {
                Debug.Log("[SaveManager] Firebase not ready, using local save: " + e.Message);
            }
            if (!_backendLogged) { _backendLogged = true; Debug.Log("[SaveManager] Boot 경유 + 미로그인 → 로컬"); }
            return _localBackend;
        }
    }

    /// <summary>세이브/로드에 참여할 핸들러 등록.</summary>
    public void Register(ISaveHandler handler)
    {
        if (handler == null || _handlers.Contains(handler)) return;
        _handlers.Add(handler);
    }

    /// <summary>등록 해제.</summary>
    public void Unregister(ISaveHandler handler)
    {
        if (handler == null) return;
        _handlers.Remove(handler);
    }

    /// <summary>등록된 핸들러들로부터 SaveData 수집.</summary>
    public SaveData GatherSaveData()
    {
        var data = new SaveData();
        for (int i = 0; i < _handlers.Count; i++)
            _handlers[i].Gather(data);
        return data;
    }

    /// <summary>로드한 SaveData를 핸들러들에게 적용.</summary>
    public void ApplySaveData(SaveData data)
    {
        if (data == null) return;
        for (int i = 0; i < _handlers.Count; i++)
            _handlers[i].Apply(data);
    }

    private void OnEnable()
    {
        Application.wantsToQuit += OnWantsToQuit;
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
    }

    private void OnDisable()
    {
        Application.wantsToQuit -= OnWantsToQuit;
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
#endif
        StopPeriodicSave();
    }

    private bool OnWantsToQuit()
    {
        if (_isQuittingAfterSave) return true;

        var task = SaveAsyncInternal();
        StartCoroutine(QuitAfterSave(task));
        return false; // 저장 완료까지 종료 보류
    }

    private IEnumerator QuitAfterSave(Task<bool> saveTask)
    {
        var timeout = 5f;
        var elapsed = 0f;
        while (!saveTask.IsCompleted && elapsed < timeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!saveTask.IsCompleted)
            Debug.LogWarning("[SaveManager] Save timeout on quit.");

        _isQuittingAfterSave = true;
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

#if UNITY_EDITOR
    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            // 베스트 에포트 (완료 보장 어려움)
            _ = SaveAsyncInternal();
        }
    }
#endif

    /// <summary>5분 주기 저장 시작. Play 진입 시 호출 권장.</summary>
    public void StartPeriodicSave()
    {
        StopPeriodicSave();
        _periodicSaveCoroutine = StartCoroutine(PeriodicSaveRoutine());
    }

    /// <summary>주기 저장 중지.</summary>
    public void StopPeriodicSave()
    {
        if (_periodicSaveCoroutine != null)
        {
            StopCoroutine(_periodicSaveCoroutine);
            _periodicSaveCoroutine = null;
        }
    }

    private IEnumerator PeriodicSaveRoutine()
    {
        var wait = new WaitForSecondsRealtime(PeriodicSaveIntervalSec);
        while (true)
        {
            yield return wait;
            _ = SaveAsyncInternal();
        }
    }

    /// <summary>비동기 저장. 로그인 시 Firestore, 아니면 로컬.</summary>
    public Task<bool> SaveAsync()
    {
        return SaveAsyncInternal();
    }

    private Task<bool> SaveAsyncInternal()
    {
        var backend = Backend;
        var data = GatherSaveData();
        if (data == null) return Task.FromResult(false);

        var isLocal = backend is LocalSaveBackend;
        return backend.SaveAsync(data)
            .ContinueWithOnMainThread(task =>
            {
                var success = !task.IsFaulted && task.Result;
                if (success)
                    Debug.Log("[SaveManager] Saved to " + (isLocal ? "local" : "Firestore") + ".");
                return success;
            });
    }

    /// <summary>비동기 로드. 없으면 null(신규 플레이).</summary>
    public Task<SaveData> LoadAsync()
    {
        var backend = Backend;
        var isLocal = backend is LocalSaveBackend;

        return backend.LoadAsync()
            .ContinueWithOnMainThread(task =>
            {
                var data = task.IsFaulted ? null : task.Result;
                if (data != null)
                {
                    // 분대원이 비어 있으면 손상/구버전 데이터. 기본값으로 복구 (악순환 방지)
                    if (data.squad?.members == null || data.squad.members.Count == 0)
                    {
                        Debug.Log("[SaveManager] Loaded data has empty squad. Migrating to default.");
                        data.squad = CreateDefaultSaveData().squad;
                    }
                    Debug.Log("[SaveManager] Loaded from " + (isLocal ? "local" : "Firestore") + ".");
                    return data;
                }

                // 저장 데이터가 전혀 없을 때: 디폴트 SaveData 생성 (로컬 신규 플레이용).
                // 기본 분대: Celeste 한 명, 슬롯 0, 위치 (63, 3, 63), 회전 0.
                var defaultData = CreateDefaultSaveData();
                Debug.Log("[SaveManager] No save found. Using default SaveData (local).");
                return defaultData;
            });
    }

    /// <summary>세이브 삭제. 디버그/테스트용.</summary>
    public Task<bool> TryDeleteSaveAsync()
    {
        return Backend.DeleteAsync();
    }

    /// <summary>저장 데이터가 없을 때 사용할 디폴트 SaveData 생성.</summary>
    private SaveData CreateDefaultSaveData()
    {
        var data = new SaveData();

        // Squad 기본값: character_celeste 한 명
        data.squad.currentPlayerId = "character_celeste";
        data.squad.playerPosition = new Vector3(63f, 3f, 63f);
        data.squad.playerRotationY = 0f;

        var member = new CharacterMemberData
        {
            id = "character_celeste",
            currentHp = 100,    // HP는 로드 후 CharacterModel 기본값/로직에 맡김
            slotIndex = 0
        };
        data.squad.members.Add(member);

        // 다른 섹션(flags, quests, inventory, gold)은 SaveData 생성자 기본값 사용
        return data;
    }
}
