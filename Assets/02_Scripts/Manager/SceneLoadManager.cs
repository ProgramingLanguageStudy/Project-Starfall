using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Intro↔Play 씬 전환 단일 진입점. 로딩 UI는 Intro/Play의 OnSceneReady에서 숨김.
/// </summary>
public class SceneLoadManager : MonoBehaviour
{
    private const string IntroSceneName = "Intro";
    private const string PlaySceneName = "Play";

    #region Lifecycle

    private void Start()
    {
        IntroScene.OnSceneReady += HandleIntroSceneReady;
    }

    private void OnDestroy()
    {
        PlayScene.OnSceneReady -= HandlePlaySceneReady;
        IntroScene.OnSceneReady -= HandleIntroSceneReady;
    }

    private void HandleIntroSceneReady()
    {
        if (GameManager.Instance != null && GameManager.Instance.UIManager != null)
            GameManager.Instance.UIManager.HideSceneTransition();
    }

    #endregion

    /// <summary>시작 시 전환 뷰를 켜서 화면 가림. GameManager.Awake에서 호출. Intro 준비 시 OnSceneReady로 Hide.</summary>
    public void ShowTransitionView()
    {
        UIManager ui = GameManager.Instance != null ? GameManager.Instance.UIManager : null;
        if (ui == null)
        {
            Debug.LogError("[SceneLoadManager] UIManager is null. Cannot show transition view.");
            return;
        }
        ui.ShowSceneTransition();
    }

    #region Load Play

    /// <summary>GameManager 부트 완료 대기 후 Play 씬 로드. Data/Resource/세이브 로드는 GameManager 전용.</summary>
    public void LoadPlayScene()
    {
        StartCoroutine(LoadPlayRoutine());
    }

    private IEnumerator LoadPlayRoutine()
    {
        UIManager ui = GameManager.Instance != null ? GameManager.Instance.UIManager : null;
        if (ui == null)
            Debug.LogError("[SceneLoadManager] UIManager is null. Loading overlay unavailable.");

        if (ui != null)
        {
            ui.ShowSceneTransition();
            ui.UpdateSceneTransitionProgress(0f, "준비중...");
        }

        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("[SceneLoadManager] GameManager is null.");
            yield break;
        }

        // PlayScene 로드 전에 세이브 데이터 확인
        SaveManager saveMgr = gm.SaveManager;
        if (saveMgr != null)
        {
            SaveData loadedData = saveMgr.LoadedSaveData;
            if (loadedData != null)
            {
                int memberCount = (loadedData.squad != null && loadedData.squad.members != null) ? loadedData.squad.members.Count : 0;
                string playerId = (loadedData.squad != null) ? loadedData.squad.currentPlayerId : null;
                Debug.Log($"[SceneLoadManager] Squad members: {memberCount}, Player: {playerId}");
            }
            else
            {
                Debug.Log("[SceneLoadManager] No save data found - using default");
            }
        }
        else
        {
            Debug.LogError("[SceneLoadManager] SaveManager is null!");
        }

        const float bootWaitTimeoutSec = 120f;
        float bootWaitStart = Time.realtimeSinceStartup;
        while (!gm.BootServicesReady)
        {
            if (Time.realtimeSinceStartup - bootWaitStart > bootWaitTimeoutSec)
            {
                Debug.LogError("[SceneLoadManager] BootServicesReady 대기 시간 초과.");
                yield break;
            }
            yield return null;
        }

        if (ui != null)
            ui.UpdateSceneTransitionProgress(0.5f, "씬 로드중...");

        AsyncOperation loadOp = SceneManager.LoadSceneAsync(PlaySceneName);
        loadOp.allowSceneActivation = false;
        while (loadOp.progress < 0.9f)
        {
            if (GameManager.Instance != null && GameManager.Instance.UIManager != null)
                GameManager.Instance.UIManager.UpdateSceneTransitionProgress(0.5f + loadOp.progress / 0.9f * 0.5f, "씬 준비중...");
            yield return null;
        }
        if (ui != null)
            ui.UpdateSceneTransitionProgress(1f, "로드 완료");
        yield return new WaitForSeconds(0.3f);

        PlayScene.OnSceneReady += HandlePlaySceneReady;
        loadOp.allowSceneActivation = true;
    }

    private void HandlePlaySceneReady()
    {
        PlayScene.OnSceneReady -= HandlePlaySceneReady;
        if (GameManager.Instance != null && GameManager.Instance.UIManager != null)
            GameManager.Instance.UIManager.HideSceneTransition();
    }

    #endregion

    #region Load Intro

    /// <summary>Play에서 Intro로 나갈 때 사용. 저장 후 씬 로드까지 한 경로로 처리 (DontDestroyOnLoad에서 코루틴 실행).</summary>
    public void RequestLoadIntroFromPlay()
    {
        StartCoroutine(LoadIntroFromPlayRoutine());
    }

    private IEnumerator LoadIntroFromPlayRoutine()
    {
        var ui = GameManager.Instance?.UIManager;
        if (ui == null)
            Debug.LogError("[SceneLoadManager] UIManager is null. Loading UI unavailable.");
        else
            ui.ShowSceneTransition();

        var gm = GameManager.Instance;
        var saveMgr = gm?.SaveManager;
        if (saveMgr == null)
        {
            Debug.LogError("[SceneLoadManager] SaveManager is null. Aborting Intro load.");
            yield break;
        }

        yield return saveMgr.SaveAsync(null);

        var loadOp = SceneManager.LoadSceneAsync(IntroSceneName);
        while (loadOp != null && !loadOp.isDone)
            yield return null;
    }

    #endregion
}
