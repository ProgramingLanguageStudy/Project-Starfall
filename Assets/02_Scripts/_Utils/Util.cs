using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 유틸리티 클래스
/// </summary>
public static class Util
{
    /// <summary>
    /// 레이어 마스크에 특정 레이어가 포함되어 있는지 확인합니다.
    /// </summary>
    /// <param name="layerMask"></param>
    /// <param name="layer"></param>
    /// <returns></returns>
    public static bool Contains(LayerMask layerMask, int layer)
    {
        return ((1 << layer) & layerMask.value) != 0;
    }

    public const float Epsilon = 0.01f; // 부동소수점 비교를 위한 작은 값

    /// <summary>
    /// 게임오브젝트가 오브젝트 풀링을 사용하는 경우 풀에 반환하고
    /// 풀링을 하지 않는 게임오브젝트면 파괴한다.
    /// </summary>
    /// <param name="go"></param>
    public static void DestroyORReturnToPool(GameObject go)
    {
        Poolable poolable = go.GetComponent<Poolable>();
        if (poolable != null)
        {
            poolable.ReturnToPool();
        }
        else
        {
            Object.Destroy(go);
        }
    }

    // IReadOnlyList는 리스트에서 읽기 전용 기능만 제공하는 인터페이스

    /// <summary>
    /// 확률 배열에서 랜덤하게 인덱스를 선택하는 함수
    /// </summary>
    /// <param name="probs"></param>
    /// <returns></returns>
    public static int Choose(IReadOnlyList<float> probs)
    {
        float total = 0;

        foreach(float prob in probs)
            if (prob > 0)
            {
                total += prob;
            }

        float randomValue = Random.value * total;

        for (int i = 0; i < probs.Count; i++)
        {
            if (probs[i] <= 0) continue;

            if (randomValue <= probs[i])
            {
                return i;
            }
            else
            {
                randomValue -= probs[i];
            }
        }
        return 0;
    }
    
    /// <summary>
    /// 요소들 중 랜덤한 한 요소를 선택해 반환하는 함수
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <returns></returns>
    public static T ChooseRandom<T>(IReadOnlyList<T> list)
    {
        if (list == null || list.Count == 0)
        {
            return default(T);
        }

        int index = Random.Range(0, list.Count);
        return list[index];
    }

    /// <summary>
    /// 리스트를 랜덤한 순서로 셔플하는 함수
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    public static void Shuffle<T>(IList<T> list)
    {
        int count = list.Count;
        for(int i = 0; i < count; i++)
        {
            int k = Random.Range(i, count);
            T temp = list[k];
            list[k] = list[i];
            list[i] = temp;
        }
    }

    /// <summary>
    /// 게임오브젝트에서 T 타입인 컴포넌트를 찾아서 반환하거나,
    /// 없으면 새로 추가해서 반환하는 함수
    /// </summary>
    /// <typeparam name="T">찾거나 추가할 컴포넌트의 타입</typeparam>
    /// <param name="go">대상 게임오브젝트</param>
    /// <returns></returns>
    public static T GetOrAddComponent<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        if (component == null)
        {
            component = go.AddComponent<T>();
        }
        return component;
    }

    /// <summary>
    /// 컴포넌트에서 T 타입인 컴포넌트를 찾아서 반환하거나,
    /// 없으면 새로 추가해서 반환하는 함수
    /// </summary>
    /// <typeparam name="T">찾거나 추가할 컴포넌트의 타입</typeparam>
    /// <param name="target">대상 컴포넌트</param>
    /// <returns></returns>
    public static T GetOrAddComponent<T>(Component target) where T : Component
    {
        T component = target.GetComponent<T>();
        if(component == null)
        {
            component = target.gameObject.AddComponent<T>();
        }
        return component;
    }

    /// <summary>
    /// 자식에서 이름이 childName인 T를 찾거나, 없으면 childName으로 자식 생성 후 컴포넌트 추가.
    /// </summary>
    public static T GetOrAddComponentInChild<T>(Component parent, string childName) where T : MonoBehaviour
    {
        if (parent == null) return null;
        foreach (var c in parent.GetComponentsInChildren<T>(true))
        {
            if (c.gameObject.name == childName) return c;
        }
        var go = new GameObject(childName);
        go.transform.SetParent(parent.transform);
        return go.AddComponent<T>();
    }

    // 인스펙터뷰에서 컴포넌트 참조를 통해

    /// <summary>
    /// 주어진 GameObject의 자식 중에서 특정 이름을 가진 T 타입의 컴포넌트를 찾습니다.
    /// 직계 자식 게임오브젝트들만 탐색하거나, 재귀적으로 모든 하위 자식까지 탐색할 수 있습니다.
    /// </summary>
    /// <typeparam name="T">찾으려는 컴포넌트의 타입</typeparam>
    /// <param name="target">탐색할 부모 GameObject</param>
    /// <param name="name">찾으려는 자식의 이름 (null일 경우 이름과 상관없이 찾음)</param>
    /// <param name="recursive">true일 경우 재귀적으로 모든 하위 자식까지 탐색, false일 경우 직계 자식들만 탐색</param>
    /// <returns>찾은 T 타입의 컴포넌트. 찾지 못하면 null을 반환</returns>
    public static T FindChild<T>(GameObject target, string name = null, bool recursive = false) where T : Object
    {
        if (target == null)
        {
            Debug.LogError($"대상 게임오브젝트가 null이라서 자식 컴포넌트를 찾을 수 없습니다.");
            return null;
        }

        // 모든 자식게임오브젝트 중에서 찾으려는 경우
        if (recursive == true)
        {
            // 모든 T 타입 자식 컴포넌트 찾기
            T[] components = target.GetComponentsInChildren<T>(true);

            foreach (T child in components)
            {
                if (string.IsNullOrEmpty(name) || child.name == name)
                {
                    return child;
                }
            }
        }
        // 직계 자식 게임오브젝트 중에서 찾으려는 경우
        else
        {
            Transform transform = target.transform;
            
            // 직계 자식 
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);

                if (string.IsNullOrEmpty(name) || child.name == name)
                {
                    T component = child.GetComponent<T>();
                    if (component != null)
                    {
                        return component;
                    }
                }
            }
        }

        if (string.IsNullOrEmpty(name) == true)
        {
            Debug.LogError($"{typeof(T).Name} 형식인 컴포넌트를 {target.name} 게임오브젝트의 자식에서 찾지 못했습니다.");
        }
        else
        {
            Debug.LogError($"{typeof(T).Name} 형식이고 이름이 {name}인 컴포넌트를 {target.name} 게임오브젝트의 자식에서 찾지 못했습니다.");
        }

        return null;
    }

    /// <summary>
    /// 참조가 없으면 호스트 오브젝트 이름·타입명으로 에러 로그 후 false. (인스펙터 할당·GetComponent 등 공통)
    /// </summary>
    public static bool CheckComponent<T>(MonoBehaviour host, T component) where T : Object
    {
        if (component != null)
            return true;

        string typeName = typeof(T).Name;
        string contextName = host != null ? host.gameObject.name : "?";
        Debug.LogError($"[{contextName}] {typeName} 참조가 할당되지 않았습니다.");
        return false;
    }

    /// <summary>
    /// Task가 완료될 때까지 대기하는 코루틴 로직의 원본
    /// </summary>
    public static IEnumerator WaitUntilComplete(Task task)
    {
        while (!task.IsCompleted)
        {
            yield return null;
        }

        if (task.IsFaulted)
        {
            var exception = task.Exception?.InnerException ?? task.Exception;
            Debug.LogError($"[Task Error] {exception}");
        }
    }
}
