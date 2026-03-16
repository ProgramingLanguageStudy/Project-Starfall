using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 전환 로딩. GameManager 하위에서 OnSceneReady 후 로딩 UI 숨김.
/// </summary>
public class SceneLoadManager : MonoBehaviour
{
    [SerializeField] private SceneTransitionLoadingView _loadingView;

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
        _loadingView?.Hide();
    }

    #endregion

    #region Load (백그라운드)

    /// <summary>Boot에서 호출. DM·RM을 백그라운드로 로드. Intro 진입 후에도 계속 진행.</summary>
    public void BeginLoad()
    {
        StartCoroutine(LoadRoutine());
    }

    private IEnumerator LoadRoutine()
    {
        var gm = GameManager.Instance;
        var dm = gm?.DataManager;
        var rm = gm?.ResourceManager;
        if (dm != null)
            yield return dm.LoadAsync(null);
        if (rm != null)
            yield return rm.LoadAsync(null);
    }

    #endregion

    /// <summary>Boot→Intro 시 전환 뷰를 켜서 화면 가림. Intro 준비 시 OnSceneReady로 자동 Hide.</summary>
    public void ShowTransitionView()
    {
        _loadingView?.Show(false);
    }

    #region Load Play

    /// <summary>씬 로드 (Data·Resource 준비 + Play 씬). 로딩 UI는 OnSceneReady 후 숨김. 이미 Boot에서 로드된 항목은 스킵.</summary>
    public void LoadPlayScene()
    {
        StartCoroutine(LoadPlayRoutine());
    }

    private IEnumerator LoadPlayRoutine()
    {
        _loadingView?.Show(true);
        _loadingView?.UpdateProgress(0f, "준비중...");

        var gm = GameManager.Instance;
        var dm = gm?.DataManager;
        var rm = gm?.ResourceManager;

        if (dm != null && !dm.IsLoaded)
        {
            yield return dm.LoadAsync((progress, status) =>
            {
                _loadingView?.UpdateProgress(progress * 0.5f, status);
            });
        }
        else
        {
            _loadingView?.UpdateProgress(0.5f, "Data 준비됨");
            yield return null;
        }

        _loadingView?.UpdateProgress(0.5f, rm != null && rm.IsLoaded() ? "Resource 준비됨" : "Resource 로드중...");

        if (rm != null && !rm.IsLoaded())
        {
            yield return rm.LoadAsync((progress, status) =>
            {
                _loadingView?.UpdateProgress(0.5f + progress * 0.5f, status);
            });
        }
        else if (rm == null)
        {
            _loadingView?.UpdateProgress(1f, "ResourceManager 없음");
            yield return null;
        }

        var loadOp = SceneManager.LoadSceneAsync("Play");
        loadOp.allowSceneActivation = false;
        while (loadOp.progress < 0.9f)
        {
            _loadingView?.UpdateProgress(0.5f + loadOp.progress / 0.9f * 0.5f, "씬 준비중...");
            yield return null;
        }
        _loadingView?.UpdateProgress(1f, "로드 완료");
        yield return new WaitForSeconds(0.3f);

        PlayScene.OnSceneReady += HandlePlaySceneReady;
        loadOp.allowSceneActivation = true;
    }

    private void HandlePlaySceneReady()
    {
        PlayScene.OnSceneReady -= HandlePlaySceneReady;
        _loadingView?.Hide();
    }

    #endregion
}
