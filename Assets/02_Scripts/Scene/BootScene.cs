using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Boot 씬 전용. 최소 초기화 후 바로 Intro로 전환. DM·RM 로드는 백그라운드로 진행되어 Intro 타이틀 연출부터 보임.
/// </summary>
public class BootScene : MonoBehaviour
{
    private void Start()
    {
        var gm = GameManager.Instance;
        gm.SceneLoadManager.ShowTransitionView();
        gm.SceneLoadManager.BeginLoad();
        SceneManager.LoadScene("Intro");
    }
}
