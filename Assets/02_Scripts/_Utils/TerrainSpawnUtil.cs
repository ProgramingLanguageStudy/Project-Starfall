using UnityEngine;

/// <summary>
/// Terrain 기반 월드 XZ 위치의 지면 높이. ItemZoneSpawner·EnemySpawner 등 공통.
/// </summary>
public static class TerrainSpawnUtil
{
    public static Terrain GetTerrainAtPosition(float worldX, float worldZ)
    {
        foreach (var t in Terrain.activeTerrains)
        {
            if (t == null || t.terrainData == null) continue;
            var bounds = t.terrainData.bounds;
            var worldMin = t.transform.TransformPoint(bounds.min);
            var worldMax = t.transform.TransformPoint(bounds.max);
            if (worldX >= worldMin.x && worldX <= worldMax.x && worldZ >= worldMin.z && worldZ <= worldMax.z)
                return t;
        }

        return null;
    }

    /// <summary>Terrain이 있으면 그 높이, 없으면 fallbackY.</summary>
    public static float GetTerrainHeight(float worldX, float worldZ, float fallbackY)
    {
        var terrain = GetTerrainAtPosition(worldX, worldZ);
        if (terrain == null)
            return fallbackY;

        float y = terrain.SampleHeight(new Vector3(worldX, 0f, worldZ));
        return terrain.transform.position.y + y;
    }
}
