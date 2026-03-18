using UnityEngine;

/// <summary>
/// Data SO 공통 베이스. Id로 DM 캐시 키 생성. Category는 Get&lt;T&gt; 조회 시 사용. CSV 호환용 Id 통일.
/// </summary>
public abstract class BaseData : ScriptableObject
{
    /// <summary>고유 ID. DM 캐시 키에 사용. 세이브·조회·경로 컨벤션에 사용.</summary>
    public abstract string Id { get; }

    /// <summary>캐시 키용 카테고리. 기본: 타입명. ItemData 계열은 "ItemData"로 통일.</summary>
    public virtual string Category => GetType().Name;
}
