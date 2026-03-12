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

    private readonly List<ISaveHandler> _handlers = new List<ISaveHandler>();
    private FirestoreSaveBackend _firestoreBackend;
    private static readonly LocalSaveBackend _localBackend = new LocalSaveBackend();
    private Coroutine _periodicSaveCoroutine;
    private bool _isQuittingAfterSave;

    private ISaveBackend Backend
    {
        get
        {
            try
            {
                var user = FirebaseAuth.DefaultInstance?.CurrentUser;
                if (user != null)
                    return _firestoreBackend ??= new FirestoreSaveBackend(user.UserId);
            }
            catch (System.Exception e)
            {
                Debug.Log("[SaveManager] Firebase not ready, using local save: " + e.Message);
            }
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
                    Debug.Log("[SaveManager] Loaded from " + (isLocal ? "local" : "Firestore") + ".");
                return data;
            });
    }

    /// <summary>세이브 삭제. 디버그/테스트용.</summary>
    public Task<bool> TryDeleteSaveAsync()
    {
        return Backend.DeleteAsync();
    }
}
