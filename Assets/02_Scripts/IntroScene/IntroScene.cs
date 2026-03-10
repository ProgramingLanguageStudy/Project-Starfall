using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroScene : MonoBehaviour
{
    [SerializeField] IntroSceneView _introSceneView;
    [SerializeField] [Tooltip("ResourceManager Preload에 쓸 라벨")]
    private string _prefabLabel = "Prefab";

    private void Start()
    {
        _introSceneView.Initialize();
        _introSceneView.OnPlayRequested += HandlePlayRequested;
    }

    private void OnDestroy()
    {
        _introSceneView.OnPlayRequested -= HandlePlayRequested;
    }

    private void HandlePlayRequested()
    {
        StartCoroutine(LoadAndTransition());
    }

    private IEnumerator LoadAndTransition()
    {
        _introSceneView.ShowLoading();
        _introSceneView.UpdateProgress(0f, "준비중...");

        var gm = GameManager.Instance;
        var dm = gm?.DataManager;
        var rm = FindFirstObjectByType<ResourceManager>();

        if (dm != null)
        {
            yield return dm.InitializeAsync((progress, status) =>
            {
                _introSceneView.UpdateProgress(progress * 0.5f, status);
            });
        }
        else
        {
            yield return null;
        }

        _introSceneView.UpdateProgress(0.5f, "ResourceManager 로드중...");

        if (rm != null)
        {
            yield return rm.PreloadByLabelAsync(_prefabLabel, (progress, status) =>
            {
                _introSceneView.UpdateProgress(0.5f + progress * 0.5f, status);
            });
        }
        else
        {
            _introSceneView.UpdateProgress(1f, "ResourceManager 없음");
            yield return null;
        }

        _introSceneView.UpdateProgress(1f, "로드 완료");
        yield return new WaitForSeconds(0.3f);

        SceneManager.LoadScene("Play");
    }
}
