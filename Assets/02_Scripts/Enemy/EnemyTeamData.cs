using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 팀 정의. Addressables Enemy/{id}·DM EnemyData/{id}와 동일 문자열. SO로 생성 후 Spawner에 할당.
/// </summary>
[CreateAssetMenu(fileName = "EnemyTeamData", menuName = "Enemy/Enemy Team Data")]
public class EnemyTeamData : ScriptableObject
{
    [Header("팀 구성")]
    [Tooltip("스폰 순서. RM GetPrefab(\"Enemy\", id), DM Get&lt;EnemyData&gt;(id)")]
    public List<string> enemyIds = new List<string>();

    [Header("배치")]
    [Tooltip("팀원 배치 반경")]
    public float spawnRadius = 2f;
}
