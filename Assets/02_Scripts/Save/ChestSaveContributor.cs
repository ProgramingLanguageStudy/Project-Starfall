using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Chest의 열림 상태를 저장/로드하는 Contributor.
/// Apply는 "지금 당장 오브젝트가 존재한다"는 가정을 하지 않고,
/// SaveData를 pending으로 보관한 뒤 Chest가 등록되는 시점에 증분 적용한다.
/// </summary>
public class ChestSaveContributor : SaveContributorBehaviour
{
    private HashSet<string> _pendingOpenedSaveIds;
    private bool _subscribed;

    public override int SaveOrder => 4; // 다른 Contributor들과 겹치지 않는 순서

    private void OnEnable()
    {
        EnsureSubscribed();
    }

    private void OnDisable()
    {
        if (!_subscribed) return;
        PlaySceneRegistry.Chests.OnRegistered -= HandleChestRegistered;
        _subscribed = false;
    }

    public override void Gather(SaveData data)
    {
        if (data == null) return;
        
        data.openedChestSaveIds.Clear();
        foreach (var chest in PlaySceneRegistry.Chests.Items)
        {
            if (chest == null) continue;
            if (!chest.IsOpened) continue;
            if (string.IsNullOrEmpty(chest.SaveId)) continue;
            data.openedChestSaveIds.Add(chest.SaveId);
        }
    }

    public override void Apply(SaveData data)
    {
        if (data == null) return;
        EnsureSubscribed();

        _pendingOpenedSaveIds = data.openedChestSaveIds != null
            ? new HashSet<string>(data.openedChestSaveIds)
            : null;

        foreach (var chest in PlaySceneRegistry.Chests.Items)
            ApplyToChestIfNeeded(chest);
    }

    private void EnsureSubscribed()
    {
        if (_subscribed) return;
        PlaySceneRegistry.Chests.OnRegistered += HandleChestRegistered;
        _subscribed = true;
    }

    private void HandleChestRegistered(Chest chest)
    {
        ApplyToChestIfNeeded(chest);
    }

    private void ApplyToChestIfNeeded(Chest chest)
    {
        if (chest == null) return;
        if (_pendingOpenedSaveIds == null || _pendingOpenedSaveIds.Count == 0) return;
        if (string.IsNullOrEmpty(chest.SaveId)) return;
        if (!_pendingOpenedSaveIds.Contains(chest.SaveId)) return;
        chest.SetOpened();
    }
}
