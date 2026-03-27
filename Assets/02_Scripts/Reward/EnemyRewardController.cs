using UnityEngine;

/// <summary>
/// 적 처치 시 보상 처리. 골드(즉시) + 확률 아이템(인벤토리 직접 추가).
/// PlaySceneEventHub.OnEnemyKilled 구독. Play 씬에 배치.
/// </summary>
public class EnemyRewardController : MonoBehaviour
{
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

        // 골드: 즉시 지급 + 팝업
        if (data.goldDrop > 0)
        {
            _inventory?.AddGold(data.goldDrop);
            GameEvents.OnGoldAcquired?.Invoke(data.goldDrop);
        }

        // 아이템: 확률 계산 후 인벤토리 직접 추가
        if (data.dropTable != null && data.dropTable.Length > 0)
        {
            foreach (var entry in data.dropTable)
            {
                if (entry.itemData == null || entry.amount <= 0) continue;
                if (entry.probability <= 0f) continue;
                if (entry.probability < 1f && Random.value > entry.probability) continue;

                // 인벤토리에 직접 추가
                _inventory?.AddItem(entry.itemData, entry.amount);
                GameEvents.OnItemPickedUp?.Invoke(entry.itemData, entry.amount);
            }
        }
    }
}
