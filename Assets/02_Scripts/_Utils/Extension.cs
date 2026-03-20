using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public static class Extension
{
    /// <summary>
    /// 레이어 마스크에 특정 레이어가 포함되어 있는지 확인합니다.
    /// </summary>
    /// <param name="layerMask"></param>
    /// <param name="layer"></param>
    /// <returns></returns>
    public static bool Contains(this LayerMask layerMask, int layer)
    {
        return Util.Contains(layerMask, layer);
    }

    /// <summary>
    /// 게임오브젝트가 오브젝트 풀링을 사용하는 경우 풀에 반환하고
    /// 풀링을 하지 않는 게임오브젝트면 파괴한다.
    /// </summary>
    /// <param name="go"></param>
    public static void DestroyORReturnToPool(this GameObject go)
    {
        Util.DestroyORReturnToPool(go);
    }

    /// <summary>
    /// 확률에 따라 
    /// </summary>
    /// <param name="probs"></param>
    /// <returns></returns>
    public static int Choose(this IReadOnlyList<float> probs)
    {
        return Util.Choose(probs);
    }

    /// <summary>
    /// 요소들 중 랜덤한 한 요소를 선택해 반환하는 함수
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <returns></returns>
    public static T ChooseRandom<T>(this IReadOnlyList<T> list)
    {
        return Util.ChooseRandom<T>(list);
    }

    /// <summary>
    /// 리스트를 랜덤한 순서로 셔플하는 함수
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    public static void Shuffle<T>(IList<T> list)
    {
        Util.Shuffle(list);
    }

    /// <summary>
    /// 게임오브젝트에서 T 타입인 컴포넌트를 찾아서 반환하거나,
    /// 없으면 새로 추가해서 반환하는 함수
    /// </summary>
    /// <typeparam name="T">찾거나 추가할 컴포넌트의 타입</typeparam>
    /// <param name="go">대상 게임오브젝트</param>
    /// <returns></returns>
    public static T GetOrAddComponent<T>(this GameObject go) where T : Component
    {
        return Util.GetOrAddComponent<T>(go);
    }

    /// <summary>
    /// 컴포넌트에서 T 타입인 컴포넌트를 찾아서 반환하거나,
    /// 없으면 새로 추가해서 반환하는 함수
    /// </summary>
    /// <typeparam name="T">찾거나 추가할 컴포넌트의 타입</typeparam>
    /// <param name="target">대상 컴포넌트</param>
    /// <returns></returns>
    public static T GetOrAddComponent<T>(this Component target) where T : Component
    {
        return Util.GetOrAddComponent<T>(target.gameObject);
    }

    /// <summary>
    /// 주어진 GameObject의 자식 중에서 특정 이름을 가진 T 타입의 컴포넌트를 찾습니다.
    /// 직계 자식 게임오브젝트들만 탐색하거나, 재귀적으로 모든 하위 자식까지 탐색할 수 있습니다.
    /// </summary>
    /// <typeparam name="T">찾으려는 컴포넌트의 타입</typeparam>
    /// <param name="target">탐색할 부모 GameObject</param>
    /// <param name="name">찾으려는 자식의 이름 (null일 경우 이름과 상관없이 찾음)</param>
    /// <param name="recursive">true일 경우 재귀적으로 모든 하위 자식까지 탐색, false일 경우 직계 자식들만 탐색</param>
    /// <returns>찾은 T 타입의 컴포넌트. 찾지 못하면 null을 반환</returns>
    public static T FindChild<T>(this GameObject target, string name = null, bool recursive = false) where T : Object
    {
        return Util.FindChild<T>(target, name, recursive);
    }

    /// <summary>
    /// 자식에서 T를 찾거나, childName으로 자식 생성 후 컴포넌트 추가.
    /// </summary>
    public static T GetOrAddComponentInChild<T>(this Component parent, string childName) where T : MonoBehaviour
    {
        return Util.GetOrAddComponentInChild<T>(parent, childName);
    }

    /// <summary>
    /// <see cref="Util.CheckComponent{T}(MonoBehaviour, T)"/> 단축. 인스턴스 메서드 안에서 <c>CheckComponent(_field)</c>처럼 호출.
    /// </summary>
    public static bool CheckComponent<T>(this MonoBehaviour host, T component) where T : Object
    {
        return Util.CheckComponent(host, component);
    }

    /// <summary>
    /// Task 객체에서 바로 .Wait()를 찍어서 쓸 수 있게 연결
    /// </summary>
    public static IEnumerator WaitUntilComplete(this Task task)
    {
        // 실제 로직은 Util에 있는 것을 호출만 함
        return Util.WaitUntilComplete(task);
    }
}