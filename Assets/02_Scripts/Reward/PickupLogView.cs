using UnityEngine;

/// <summary>
/// 아이템 획득 시 왼쪽 중간에 아이콘+x수량 알림. 새 항목이 위에 쌓이고, 2~3초 후 밀려나며 사라짐.
/// PlaySceneOverlayController가 이벤트 구독 후 AddEntry/AddGoldEntry 호출.
/// PoolManager는 GM에서, 프리팹은 RM에서 UI/PickUpEntry 로드. GM.PoolManager 없으면 Instantiate.
/// </summary>
public class PickupLogView : MonoBehaviour
{
    [SerializeField] private Transform _container;

    private const string PrefabCategory = "UI";
    private const string PrefabName = "PickUpEntry";

    private GameObject _entryPrefab;

    private GameObject GetEntryPrefab()
    {
        if (_entryPrefab != null) return _entryPrefab;
        var rm = GameManager.Instance?.ResourceManager;
        if (rm == null)
        {
            Debug.LogError("[PickupLogView] ResourceManager 없음.");
            return null;
        }
        _entryPrefab = rm.GetPrefab(PrefabCategory, PrefabName);
        if (_entryPrefab == null)
            Debug.LogError($"[PickupLogView] 프리팹 로드 실패: {PrefabCategory}/{PrefabName}");
        return _entryPrefab;
    }

    /// <summary>아이템 획득 알림 추가.</summary>
    public void AddEntry(ItemData itemData, int amount)
    {
        if (itemData == null || _container == null) return;

        var prefab = GetEntryPrefab();
        if (prefab == null) return;

        var go = GetOrCreateEntry(prefab);
        var entry = go.GetComponent<PickupLogEntry>();
        if (entry != null)
        {
            entry.Show(itemData, amount);
            go.transform.SetAsFirstSibling();
        }
    }

    /// <summary>골드 획득 알림 추가.</summary>
    public void AddGoldEntry(int amount)
    {
        if (_container == null) return;

        var prefab = GetEntryPrefab();
        if (prefab == null) return;

        var go = GetOrCreateEntry(prefab);
        var entry = go.GetComponent<PickupLogEntry>();
        if (entry != null)
        {
            entry.ShowGold(amount);
            go.transform.SetAsFirstSibling();
        }
    }

    private GameObject GetOrCreateEntry(GameObject prefab)
    {
        var pm = GameManager.Instance?.PoolManager;
        GameObject go;
        if (pm != null)
        {
            go = pm.Pop(prefab);
            go.transform.SetParent(_container);
        }
        else
        {
            go = Instantiate(prefab, _container);
        }
        return go;
    }
}
