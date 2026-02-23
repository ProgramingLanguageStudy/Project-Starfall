using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 팀 정의. Enemy 프리팹 목록·배치 반경 등. SO로 생성 후 Spawner에 할당.
/// </summary>
[CreateAssetMenu(fileName = "EnemyTeamData", menuName = "Enemy/Enemy Team Data")]
public class EnemyTeamData : ScriptableObject
{
    [Header("팀 구성")]
    [Tooltip("팀에 넣을 Enemy 프리팹들 (순서대로 스폰)")]
    public List<GameObject> enemyPrefabs = new List<GameObject>();

    [Header("배치")]
    [Tooltip("팀원 배치 반경")]
    public float spawnRadius = 2f;
}
