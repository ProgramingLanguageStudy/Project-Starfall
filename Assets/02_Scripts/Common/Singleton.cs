using UnityEngine;

/// <summary>
/// MonoBehaviour 전용 제네릭 싱글톤. 씬에 동일 타입이 하나만 존재하도록 보장한다.
/// Instance 접근 시 씬에서 먼저 찾고, 없으면 새 GameObject에 컴포넌트를 붙여 생성한다.
/// 씬 전환 후에도 유지할 필요가 있으면, 자식 클래스 Awake에서 DontDestroyOnLoad(gameObject)를 호출하면 된다 (필수 아님).
/// </summary>
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<T>();
                if (_instance == null && Application.isPlaying)
                {
                    var obj = new GameObject(typeof(T).Name);
                    _instance = obj.AddComponent<T>();
                }
            }
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            // 씬 전환 후에도 유지하려면 자식에서 DontDestroyOnLoad(gameObject) 호출. 필요 없으면 호출 안 해도 됨.
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
}
