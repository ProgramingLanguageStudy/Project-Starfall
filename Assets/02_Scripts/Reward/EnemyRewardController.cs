using UnityEngine;

/// <summary>
/// 적 처치 시 보상 처리. 골드(즉시) + 확률 아이템 드롭(월드 생성).
/// PlaySceneEventHub.OnEnemyKilled 구독. Play 씬에 배치.
/// PoolManager 있으면 풀 사용, 없으면 Instantiate.
/// </summary>
public class EnemyRewardController : MonoBehaviour
{
    [SerializeField] [Tooltip("드롭 아이템 월드 오브젝트 프리팹. ItemObject 컴포넌트 필요")]
    private ItemObject _itemObjectPrefab;
    [SerializeField] [Tooltip("비면 Instantiate. 있으면 풀에서 Pop")]
    private PoolManager _poolManager;
    
    private Inventory _inventory;

    /// <summary>PlayScene에서 의존성 주입용.</summary>
    public void Initialize(Inventory inventory)
    {
        _inventory = inventory;
    }

    private void OnEnable()
    {
        PlaySceneEventHub.OnEnemyKilled += HandleEnemyKilled;
    }

    private void OnDisable()
    {
        PlaySceneEventHub.OnEnemyKilled -= HandleEnemyKilled;
    }

    private void HandleEnemyKilled(Enemy enemy)
    {
        if (enemy?.Model?.Data == null) return;

        var data = enemy.Model.Data;
        var deathPos = enemy.transform.position + Vector3.up * 0.3f;

        // 골드: 즉시 지급 + 팝업
        if (data.goldDrop > 0)
        {
            _inventory?.AddGold(data.goldDrop);
            GameEvents.OnGoldAcquired?.Invoke(data.goldDrop);
        }

        // 아이템: 확률 계산 후 월드 생성
        if (data.dropTable != null && data.dropTable.Length > 0 && _itemObjectPrefab != null)
        {
            foreach (var entry in data.dropTable)
            {
                if (entry.itemData == null || entry.amount <= 0) continue;
                if (entry.probability <= 0f) continue;
                if (entry.probability < 1f && Random.value > entry.probability) continue;

                var offset = Random.insideUnitCircle * 0.5f;
                var pos = deathPos + new Vector3(offset.x, 0f, offset.y);

                GameObject go;
                if (_poolManager != null)
                    go = _poolManager.Pop(_itemObjectPrefab.gameObject);
                else
                    go = Instantiate(_itemObjectPrefab.gameObject);

                go.transform.position = pos;
                go.transform.rotation = Quaternion.identity;

                var itemObj = go.GetComponent<ItemObject>();
                if (itemObj != null)
                    itemObj.Initialize(entry.itemData, entry.amount);
            }
        }
    }
}
