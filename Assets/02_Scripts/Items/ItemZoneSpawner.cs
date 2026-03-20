using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 직사각형 구역 내 그리드 기반 아이템 스폰. 행/열 개수로 균등 분할.
/// Terrain.activeTerrains 순회 + SampleHeight로 높이 계산 (다중 Terrain·경계 대응).
/// 자동 스폰은 <see cref="PlayScene.IsSceneReady"/> 이후(ResourceManager 등 부트 완료 뒤).
/// </summary>
public class ItemZoneSpawner : MonoBehaviour
{
    [Header("아이템")]
    [SerializeField] private ItemData _itemData;

    [Header("구역")]
    [SerializeField] [Tooltip("구역 크기 (가로, 세로). transform.position 기준 중심")]
    private Vector2 _zoneSize = new Vector2(10f, 10f);
    [SerializeField] [Tooltip("가로 셀 개수. zoneSize.x를 이 수로 나눔")]
    private int _gridCols = 3;
    [SerializeField] [Tooltip("세로 셀 개수. zoneSize.y를 이 수로 나눔")]
    private int _gridRows = 3;

    [Header("스폰")]
    [SerializeField] [Tooltip("스폰할 개수. 셀 개수 초과 시 셀 개수만큼만 스폰")]
    private int _amount = 9;

    [Header("스폰 시점")]
    [SerializeField] [Tooltip("true면 PlayScene 준비 완료 후 자동 Spawn, false면 Spawn() 수동 호출")]
    private bool _spawnOnStart = true;

    private bool _didAutoSpawn;

    // EnemySpawner와 동일: IsSceneReady 이후에만 자동 스폰(부트·RM 보장)
    private void OnEnable()
    {
        if (!_spawnOnStart)
            return;

        if (PlayScene.IsSceneReady)
            TryAutoSpawnOnce();
        else
            PlayScene.OnSceneReady += OnPlaySceneReadyForAutoSpawn;
    }

    private void OnDisable()
    {
        PlayScene.OnSceneReady -= OnPlaySceneReadyForAutoSpawn;
    }

    private void OnPlaySceneReadyForAutoSpawn()
    {
        PlayScene.OnSceneReady -= OnPlaySceneReadyForAutoSpawn;
        TryAutoSpawnOnce();
    }

    private void TryAutoSpawnOnce()
    {
        if (_didAutoSpawn)
            return;
        _didAutoSpawn = true;
        Spawn();
    }

    /// <summary>구역 내 그리드 셀에 아이템 스폰.</summary>
    public void Spawn()
    {
        if (_itemData == null)
        {
            Debug.LogWarning("[ItemZoneSpawner] ItemData가 비어 있습니다.", this);
            return;
        }

        var rm = GameManager.Instance?.ResourceManager;
        if (rm == null)
        {
            Debug.LogWarning("[ItemZoneSpawner] ResourceManager 없음. Prefab 라벨 로드 여부 확인.", this);
            return;
        }

        var prefab = rm.GetPrefab("Item", _itemData.Id);
        if (prefab == null)
        {
            Debug.LogWarning($"[ItemZoneSpawner] 프리팹 없음: Item/{_itemData.Id}. Addressables 확인.", this);
            return;
        }

        if (_gridCols <= 0 || _gridRows <= 0)
        {
            Debug.LogWarning("[ItemZoneSpawner] gridCols, gridRows는 1 이상이어야 합니다.", this);
            return;
        }

        // 셀 목록을 섞은 뒤 앞에서 amount개만 사용 → 구역 내 랜덤 분포
        float cellSizeX = _zoneSize.x / _gridCols;
        float cellSizeZ = _zoneSize.y / _gridRows;

        var cells = new List<(int ix, int iz)>();
        for (int iz = 0; iz < _gridRows; iz++)
        for (int ix = 0; ix < _gridCols; ix++)
            cells.Add((ix, iz));

        Shuffle(cells);

        int spawnCount = Mathf.Min(_amount, cells.Count);
        float centerX = transform.position.x;
        float centerZ = transform.position.z;
        float minX = centerX - _zoneSize.x * 0.5f;
        float minZ = centerZ - _zoneSize.y * 0.5f;

        for (int i = 0; i < spawnCount; i++)
        {
            var (ix, iz) = cells[i];
            // 셀 안에서 또 한 번 랜덤 오프셋
            float x = minX + ix * cellSizeX + Random.Range(0f, cellSizeX);
            float z = minZ + iz * cellSizeZ + Random.Range(0f, cellSizeZ);

            float y = TerrainSpawnUtil.GetTerrainHeight(x, z, transform.position.y);
            var pos = new Vector3(x, y, z);

            GameObject go;
            var pm = GameManager.Instance?.PoolManager;
            // PM 있으면 풀(픽업 시 ItemObject가 ReturnToPool), 없으면 구형 Instantiate
            if (pm != null)
                go = pm.Pop(prefab);
            else
                go = Instantiate(prefab);

            if (go == null)
                continue;

            go.transform.SetParent(transform);
            go.transform.SetPositionAndRotation(pos, Quaternion.identity);
            var itemObj = go.GetComponent<ItemObject>();
            if (itemObj != null)
                itemObj.Initialize(_itemData, 1);
        }
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        var center = transform.position;
        var size = new Vector3(_zoneSize.x, 0.1f, _zoneSize.y);
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawCube(center, size);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, size);

        if (_gridCols > 0 && _gridRows > 0)
        {
            float cellSizeX = _zoneSize.x / _gridCols;
            float cellSizeZ = _zoneSize.y / _gridRows;
            float minX = center.x - _zoneSize.x * 0.5f;
            float minZ = center.z - _zoneSize.y * 0.5f;
            float y = center.y;

            Gizmos.color = new Color(0f, 1f, 0f, 0.6f);
            for (int i = 0; i <= _gridCols; i++)
            {
                float x = minX + i * cellSizeX;
                Gizmos.DrawLine(new Vector3(x, y, minZ), new Vector3(x, y, minZ + _zoneSize.y));
            }
            for (int i = 0; i <= _gridRows; i++)
            {
                float z = minZ + i * cellSizeZ;
                Gizmos.DrawLine(new Vector3(minX, y, z), new Vector3(minX + _zoneSize.x, y, z));
            }
        }
    }
#endif
}
