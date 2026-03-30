using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 세이브 시점 제어 및 API 제공. ISaveContributor 등록·수집·적용.
/// ISaveBackend만 보유. SaveAccessPhase + 인증·에디터 옵션에 따라 백엔드를 고른다(WireFirebaseAuth 구독).
/// Save/Load/Delete는 Firestore 대기를 위해 <see cref="IEnumerator"/> + <c>yield return</c> 패턴(호출부에서 순서·완료 제어).
/// </summary>
public class SaveManager : MonoBehaviour
{
    /// <summary>ResolveBackend 결과를 묶음. Key로 동일 백엔드인지 비교해 불필요한 전환을 막는다.</summary>
    private readonly struct BackendResolution
    {
        public readonly string Key;
        public readonly SaveAccessPhase Phase;
        public readonly ISaveBackend Backend;

        public BackendResolution(string key, SaveAccessPhase phase, ISaveBackend backend)
        {
            Key = key;
            Phase = phase;
            Backend = backend;
        }
    }

    #region Fields

    private static string _lastLoggedBackendKey;

    private ISaveBackend _backend;

    /// <summary>마지막으로 적용한 백엔드 식별 문자열. 변경 없으면 <see cref="ApplySaveBackend"/>에서 조기 return.</summary>
    private string _appliedBackendKey;

    private SaveAccessPhase _accessPhase = SaveAccessPhase.Pending;
    private FirebaseAuthManager _wiredAuth;
    private SaveData _loadedSaveData;
    private readonly List<ISaveContributor> _contributors = new List<ISaveContributor>();

    /// <summary>종료 직전 저장 루틴 통과 여부. true면 <see cref="OnWantsToQuit"/>에서 즉시 종료 허용.</summary>
    private bool _isQuittingAfterSave;

    #endregion

    #region Public API

    /// <summary>로드 완료된 SaveData. LoadAsync 완료 후 사용.</summary>
    public SaveData LoadedSaveData => _loadedSaveData;

    /// <summary><see cref="LoadAsync"/>가 한 번 이상 끝났는지(기본 세이브로 종료 포함). 부트 집계용.</summary>
    public bool IsLoadComplete { get; private set; }

    /// <summary>세이브가 디스크/클라우드에 닿기 전 대기인지, 게스트 로컬인지, 클라우드인지.</summary>
    public SaveAccessPhase AccessPhase => _accessPhase;

    /// <summary>백엔드 주입. ApplySaveBackend 없이 직접 바꿀 때만 사용.</summary>
    public void SetBackend(ISaveBackend backend)
    {
        _backend = backend;
    }

    /// <summary>현재 인증·에디터 옵션에 맞는 백엔드로 갱신. 에디터 강제 로컬 토글 등에서 호출.</summary>
    public void ApplySaveBackend(FirebaseAuthManager auth)
    {
        var r = ResolveBackend(SaveDevSettings.ForceLocalSave, auth);
        // 로그인 전환 등으로 백엔드가 실제로 바뀔 때만 갱신(같은 키면 스킵).
        if (r.Key == _appliedBackendKey) return;
        var previousPhase = _accessPhase;
        _appliedBackendKey = r.Key;
        _accessPhase = r.Phase;
        SetBackend(r.Backend);

        // 부트 시 LoadAsync는 게스트 기준으로 이미 끝난 뒤일 수 있음. 이후 로그인하면 백엔드만 Firestore로 바뀌고
        // _loadedSaveData는 로컬/기본값에 머물러 Play 씬에 클라우드 데이터가 안 들어간다 → 전환 직후 재로드.
        if (r.Phase == SaveAccessPhase.Cloud && previousPhase == SaveAccessPhase.LocalGuest && _backend != null)
            StartCoroutine(ReloadSaveAfterCloudLoginRoutine());
    }

    /// <summary>
    /// Firebase 인증 참조를 연결하고 SessionChanged마다 ApplySaveBackend를 호출한다.
    /// GameManager Awake에서 한 번 호출. auth가 null이면 구독 해제 후 LocalGuest(로컬 파일)로 맞춘다.
    /// </summary>
    public void WireFirebaseAuth(FirebaseAuthManager auth)
    {
        UnwireFirebaseAuth();
        _wiredAuth = auth;
        if (_wiredAuth != null)
            _wiredAuth.SessionChanged += OnWiredSessionChanged;
        ApplySaveBackend(_wiredAuth);
    }

    private void UnwireFirebaseAuth()
    {
        if (_wiredAuth == null) return;
        _wiredAuth.SessionChanged -= OnWiredSessionChanged;
        _wiredAuth = null;
    }

    private void OnWiredSessionChanged(FirebaseAuthSessionSnapshot _)
    {
        // Initializing → ReadyLoggedIn 등 단계가 바뀌면 Pending/로컬/클라우드 백엔드가 달라질 수 있음.
        ApplySaveBackend(_wiredAuth);
    }

    #endregion

    #region Unity Lifecycle

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
    }

    private void OnDestroy()
    {
        UnwireFirebaseAuth();
    }

    #endregion

    #region Quit / Play Mode - Save on Exit

    /// <summary>
    /// 앱 종료 시 호출. 빌드(창 닫기)·에디터 전체 종료 시에만 동작.
    /// Play 모드 Stop 시에는 호출되지 않음 → OnPlayModeStateChanged 사용.
    /// </summary>
    private bool OnWantsToQuit()
    {
        if (_isQuittingAfterSave) return true;
        // false면 Unity가 종료를 잠시 보류하고, 코루틴에서 저장 끝난 뒤 다시 Quit을 유도한다.
        StartCoroutine(QuitAfterSaveRoutine());
        return false;
    }

    /// <summary>저장 완료 후 종료. wantsToQuit에서 false 반환 시 Unity가 재호출.</summary>
    private IEnumerator QuitAfterSaveRoutine()
    {
        var done = false;
        var success = false;
        // SaveAsync는 IEnumerator라 별도 코루틴으로 돌리고, 콜백으로 완료만 감지.
        StartCoroutine(SaveAsync(b => { done = true; success = b; }));

        var elapsed = 0f;
        // Firestore 지연 시 무한 대기 방지(타임아웃 후에도 종료 진행).
        while (!done && elapsed < SaveConfig.QuitSaveTimeoutSec)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!done)
            Debug.LogWarning("[SaveManager] Save timeout on quit.");

        _isQuittingAfterSave = true;
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// 에디터 전용. Play 모드 Stop 시 호출. wantsToQuit은 Play 모드 종료 시 호출되지 않으므로 별도 처리.
    /// </summary>
#if UNITY_EDITOR
    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
            // wantsToQuit이 Play Stop 시에는 안 오므로, 여기서만 비동기 저장 시도(완료 대기 없음).
            StartCoroutine(SaveAsync(null));
    }
#endif

    #endregion

    #region ISaveContributor

    public void Register(ISaveContributor contributor)
    {
        if (contributor == null || _contributors.Contains(contributor)) return;
        _contributors.Add(contributor);
    }

    public void Unregister(ISaveContributor contributor)
    {
        if (contributor == null) return;
        _contributors.Remove(contributor);
    }

    public SaveData GatherSaveData()
    {
        var data = new SaveData();
        foreach (var c in _contributors.Where(x => x != null).OrderBy(x => x.SaveOrder))
            c.Gather(data);
        return data;
    }

    public void ApplySaveData(SaveData data)
    {
        if (data == null) return;
        foreach (var c in _contributors.Where(x => x != null).OrderBy(x => x.SaveOrder))
            c.Apply(data);
    }

    #endregion

    #region Save / Load / Delete

    /// <summary>비동기 저장. onComplete(성공여부).</summary>
    public IEnumerator SaveAsync(Action<bool> onComplete)
    {
        // Pending 또는 백엔드 미할당 시 디스크/클라우드에 쓰지 않음.
        if (_backend == null)
        {
            if (_accessPhase == SaveAccessPhase.Pending)
                Debug.LogWarning("[SaveManager] Save skipped: access phase is Pending.");
            else
                Debug.LogWarning("[SaveManager] Save skipped: no backend.");
            onComplete?.Invoke(false);
            yield break;
        }

        var data = GatherSaveData();
        if (data == null) { onComplete?.Invoke(false); yield break; }

        if (SaveDevSettings.LogSaveDiagnostics)
        {
            var ordered = _contributors.Where(x => x != null).OrderBy(x => x.SaveOrder).ToList();
            var names = string.Join(", ", ordered.Select(x => x.GetType().Name));
            Debug.Log($"[SaveDiag] GatherSaveData → count={ordered.Count} [{names}] → gold={data.inventory.gold}");
        }

        var success = false;
        // 로컬·Firestore 공통: 백엔드 코루틴이 끝날 때까지 프레임 양보(메인 스레드 블로킹 없음).
        yield return _backend.SaveAsync(data, b => success = b);

        if (success)
            Debug.Log("[SaveManager] Saved.");
        onComplete?.Invoke(success);
    }

    /// <summary>비동기 로드. 완료 시 _loadedSaveData에 저장. 없으면 디폴트. Pending이면 백엔드 확정까지 대기(<see cref="SaveConfig.LoadWaitForBackendTimeoutSec"/>).</summary>
    public IEnumerator LoadAsync(Action<float, string> onProgress = null)
    {
        // Auth InitializeAsync가 Load보다 늦게 끝나거나, SessionChanged 직후에야 백엔드가 잡히는 경우 대비.
        if (_accessPhase == SaveAccessPhase.Pending)
        {
            var waitElapsed = 0f;
            while (_accessPhase == SaveAccessPhase.Pending && waitElapsed < SaveConfig.LoadWaitForBackendTimeoutSec)
            {
                onProgress?.Invoke(
                    Mathf.Min(0.25f, waitElapsed / SaveConfig.LoadWaitForBackendTimeoutSec * 0.25f),
                    "세이브 백엔드 준비 중...");
                waitElapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (_backend == null)
            {
                Debug.LogWarning(
                    "[SaveManager] Load: backend still Pending after wait (Auth 미연결 또는 초기화 지연). Using default save.");
                _loadedSaveData = CreateDefaultSaveData();
                onProgress?.Invoke(1f, "Save 로드 완료");
                yield break;
            }
        }
        else if (_backend == null)
        {
            Debug.LogWarning("[SaveManager] Load: no backend but phase is not Pending. Using default save.");
            _loadedSaveData = CreateDefaultSaveData();
            onProgress?.Invoke(1f, "Save 로드 완료");
            yield break;
        }

        onProgress?.Invoke(0f, "Save 로드중...");
        SaveData data = null;
        yield return _backend.LoadAsync(d => data = d);

        if (data != null)
        {
            // 구버전/깨진 데이터 대비: 스쿼드 비어 있으면 기본 멤버로 보정.
            if (data.squad?.members == null || data.squad.members.Count == 0)
            {
                Debug.Log("[SaveManager] Loaded data has empty squad. Migrating to default.");
                data.squad = CreateDefaultSaveData().squad;
            }
            _loadedSaveData = data;
            Debug.Log("[SaveManager] Loaded.");
            if (SaveDevSettings.LogSaveDiagnostics)
                Debug.Log($"[SaveDiag] LoadedSaveData.gold={data.inventory.gold}");
        }
        else
        {
            _loadedSaveData = CreateDefaultSaveData();
            Debug.Log("[SaveManager] No save found. Using default.");
            if (SaveDevSettings.LogSaveDiagnostics)
                Debug.Log($"[SaveDiag] default LoadedSaveData.gold={_loadedSaveData.inventory.gold}");
        }

        onProgress?.Invoke(1f, "Save 로드 완료");
        IsLoadComplete = true;
    }

    /// <summary>
    /// 게스트로 부트 로드가 끝난 뒤 로그인해 Firestore로 전환된 경우, 클라우드에서 다시 읽어 메모리에 반영한다.
    /// Contributor가 아직 없으면 Apply는 스킵되고, Play 씬에서 <see cref="LoadedSaveData"/>로 적용된다.
    /// </summary>
    private IEnumerator ReloadSaveAfterCloudLoginRoutine()
    {
        Debug.Log("[SaveManager] LocalGuest → Cloud 전환: Firestore에서 세이브 재로드.");
        yield return LoadAsync(null);
        if (_loadedSaveData != null)
            ApplySaveData(_loadedSaveData);
    }

    /// <summary>세이브 삭제. 디버그/테스트용.</summary>
    public IEnumerator TryDeleteSaveAsync(Action<bool> onComplete = null)
    {
        // SaveAsync와 동일한 백엔드 null/Pending 규칙.
        if (_backend == null)
        {
            if (_accessPhase == SaveAccessPhase.Pending)
                Debug.LogWarning("[SaveManager] Delete skipped: access phase is Pending.");
            else
                Debug.LogWarning("[SaveManager] Delete skipped: no backend.");
            onComplete?.Invoke(false);
            yield break;
        }

        var success = false;
        yield return _backend.DeleteAsync(b => success = b); // Save/Load와 동일하게 백엔드 완료까지 대기.
        onComplete?.Invoke(success);
    }

    #endregion

    #region Private

    /// <summary>Pending / LocalGuest / Cloud 분기. 규칙 변경 시 이 메서드만 보면 된다.</summary>
    private static BackendResolution ResolveBackend(bool forceLocal, FirebaseAuthManager auth)
    {
        if (forceLocal)
        {
            LogBackendOnce("force_local", "[SaveManager] 개발 강제 → LocalGuest");
            return new BackendResolution("force_local", SaveAccessPhase.LocalGuest, new LocalSaveBackend());
        }

        if (auth == null)
        {
            LogBackendOnce("auth_null", "[SaveManager] FirebaseAuth 없음 → LocalGuest");
            return new BackendResolution("auth_null", SaveAccessPhase.LocalGuest, new LocalSaveBackend());
        }

        // 스냅샷 Phase 한 축으로 로컬 vs 클라우드 vs I/O 보류를 결정.
        switch (auth.LastSnapshot.Phase)
        {
            case FirebaseAuthLifecyclePhase.Initializing:
                LogBackendOnce("pending", "[SaveManager] Auth 첫 부트 전 → Pending (세이브 I/O 없음)");
                return new BackendResolution("pending", SaveAccessPhase.Pending, null);
            case FirebaseAuthLifecyclePhase.InitFailed:
                LogBackendOnce("auth_init_failed", "[SaveManager] Firebase 초기화 실패 → LocalGuest(로컬만)");
                return new BackendResolution("auth_init_failed", SaveAccessPhase.LocalGuest, new LocalSaveBackend());
            case FirebaseAuthLifecyclePhase.ReadyGuest:
                LogBackendOnce("local_guest", "[SaveManager] 미로그인 → LocalGuest");
                return new BackendResolution("local_guest", SaveAccessPhase.LocalGuest, new LocalSaveBackend());
            case FirebaseAuthLifecyclePhase.ReadyLoggedIn:
                var uid = auth.LastSnapshot.UserId;
                if (string.IsNullOrEmpty(uid))
                {
                    Debug.LogError("[SaveManager] ReadyLoggedIn but UserId empty. Using LocalGuest.");
                    return new BackendResolution("uid_empty", SaveAccessPhase.LocalGuest, new LocalSaveBackend());
                }
                LogBackendOnce("firestore", "[SaveManager] 로그인됨 → Cloud (Firestore)");
                return new BackendResolution("firestore:" + uid, SaveAccessPhase.Cloud, new FirestoreSaveBackend(uid));
            default:
                Debug.LogError(
                    "[SaveManager] ResolveBackend: unknown FirebaseAuthLifecyclePhase " + auth.LastSnapshot.Phase + ". Using LocalGuest.");
                return new BackendResolution("phase_unknown", SaveAccessPhase.LocalGuest, new LocalSaveBackend());
        }
    }

    private static void LogBackendOnce(string key, string message)
    {
        if (_lastLoggedBackendKey == key) return;
        _lastLoggedBackendKey = key;
        Debug.Log(message);
    }

    private SaveData CreateDefaultSaveData()
    {
        var data = new SaveData();
        data.squad.currentPlayerId = "character_celeste";
        data.squad.playerPosition = new Vector3(63f, 3f, 63f);
        data.squad.playerRotationY = 0f;
        data.squad.members.Add(new CharacterMemberData
        {
            id = "character_celeste",
            level = 1,
            currentHp = 100,
            slotIndex = 0
        });
        return data;
    }

    #endregion
}
