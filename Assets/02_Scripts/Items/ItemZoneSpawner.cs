using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 직사각형 구역 내 그리드 기반 아이템 스폰. 행/열 개수로 균등 분할.
/// Terrain.activeTerrains 순회 + SampleHeight로 높이 계산 (다중 Terrain·경계 대응).
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
    [SerializeField] [Tooltip("true면 Start에서 자동 스폰, false면 Spawn() 수동 호출")]
    private bool _spawnOnStart = true;

    private void Start()
    {
        if (_spawnOnStart)
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
            float x = minX + ix * cellSizeX + Random.Range(0f, cellSizeX);
            float z = minZ + iz * cellSizeZ + Random.Range(0f, cellSizeZ);

            var terrain = GetTerrainAtPosition(x, z);
            if (terrain == null) continue;

            float y = terrain.SampleHeight(new Vector3(x, 0f, z));
            var pos = new Vector3(x, terrain.transform.position.y + y, z);

            var go = Instantiate(prefab, pos, Quaternion.identity, transform);
            var itemObj = go.GetComponent<ItemObject>();
            if (itemObj != null)
                itemObj.Initialize(_itemData, 1);
        }
    }

    private static Terrain GetTerrainAtPosition(float worldX, float worldZ)
    {
        foreach (var t in Terrain.activeTerrains)
        {
            var bounds = t.terrainData.bounds;
            var worldMin = t.transform.TransformPoint(bounds.min);
            var worldMax = t.transform.TransformPoint(bounds.max);
            if (worldX >= worldMin.x && worldX <= worldMax.x && worldZ >= worldMin.z && worldZ <= worldMax.z)
                return t;
        }
        return null;
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
