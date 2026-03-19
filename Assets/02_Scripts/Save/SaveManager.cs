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
/// ISaveBackend만 보유. 백엔드 선택은 SaveBackendProvider에서.
/// </summary>
public class SaveManager : MonoBehaviour
{
    #region Fields

    private ISaveBackend _backend;
    private SaveData _loadedSaveData;
    private readonly List<ISaveContributor> _contributors = new List<ISaveContributor>();
    private Coroutine _periodicSaveCoroutine;
    private bool _isQuittingAfterSave;

    #endregion

    #region Public API

    /// <summary>로드 완료된 SaveData. LoadAsync 완료 후 사용.</summary>
    public SaveData LoadedSaveData => _loadedSaveData;

    /// <summary>백엔드 주입. GameManager 등에서 SaveBackendProvider.CreateBackend()로 생성 후 호출.</summary>
    public void SetBackend(ISaveBackend backend)
    {
        _backend = backend;
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
        StopPeriodicSave();
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
        StartCoroutine(QuitAfterSaveRoutine());
        return false;
    }

    /// <summary>저장 완료 후 종료. wantsToQuit에서 false 반환 시 Unity가 재호출.</summary>
    private IEnumerator QuitAfterSaveRoutine()
    {
        var done = false;
        var success = false;
        StartCoroutine(SaveAsync(b => { done = true; success = b; }));

        var elapsed = 0f;
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

    #region Periodic Save

    public void StartPeriodicSave()
    {
        StopPeriodicSave();
        _periodicSaveCoroutine = StartCoroutine(PeriodicSaveRoutine());
    }

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
        var wait = new WaitForSecondsRealtime(SaveConfig.PeriodicSaveIntervalSec);
        while (true)
        {
            yield return wait;
            StartCoroutine(SaveAsync(null));
        }
    }

    #endregion

    #region Save / Load / Delete

    /// <summary>비동기 저장. onComplete(성공여부).</summary>
    public IEnumerator SaveAsync(Action<bool> onComplete)
    {
        if (_backend == null) { onComplete?.Invoke(false); yield break; }

        var data = GatherSaveData();
        if (data == null) { onComplete?.Invoke(false); yield break; }

        var success = false;
        yield return _backend.SaveAsync(data, b => success = b);

        if (success)
            Debug.Log("[SaveManager] Saved.");
        onComplete?.Invoke(success);
    }

    /// <summary>씬 언로드 전 호출. Unregister 직전에.</summary>
    public void SaveBeforeUnload()
    {
        var data = GatherSaveData();
        if (data != null && _backend != null)
            StartCoroutine(_backend.SaveAsync(data, _ => { }));
    }

    /// <summary>비동기 로드. 완료 시 _loadedSaveData에 저장. 없으면 디폴트.</summary>
    public IEnumerator LoadAsync(Action<float, string> onProgress = null)
    {
        if (_backend == null)
        {
            _loadedSaveData = CreateDefaultSaveData();
            onProgress?.Invoke(1f, "Save 로드 완료");
            yield break;
        }

        onProgress?.Invoke(0f, "Save 로드중...");
        SaveData data = null;
        yield return _backend.LoadAsync(d => data = d);

        if (data != null)
        {
            if (data.squad?.members == null || data.squad.members.Count == 0)
            {
                Debug.Log("[SaveManager] Loaded data has empty squad. Migrating to default.");
                data.squad = CreateDefaultSaveData().squad;
            }
            _loadedSaveData = data;
            Debug.Log("[SaveManager] Loaded.");
        }
        else
        {
            _loadedSaveData = CreateDefaultSaveData();
            Debug.Log("[SaveManager] No save found. Using default.");
        }

        onProgress?.Invoke(1f, "Save 로드 완료");
    }

    /// <summary>세이브 삭제. 디버그/테스트용.</summary>
    public IEnumerator TryDeleteSaveAsync(Action<bool> onComplete = null)
    {
        if (_backend == null) { onComplete?.Invoke(false); yield break; }

        var success = false;
        yield return _backend.DeleteAsync(b => success = b);
        onComplete?.Invoke(success);
    }

    #endregion

    #region Private

    private SaveData CreateDefaultSaveData()
    {
        var data = new SaveData();
        data.squad.currentPlayerId = "character_celeste";
        data.squad.playerPosition = new Vector3(63f, 3f, 63f);
        data.squad.playerRotationY = 0f;
        data.squad.members.Add(new CharacterMemberData
        {
            id = "character_celeste",
            currentHp = 100,
            slotIndex = 0
        });
        return data;
    }

    #endregion
}
