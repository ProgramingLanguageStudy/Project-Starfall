using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "ChestData", menuName = "Data/Chest")]
public class ChestData : BaseData
{
    [SerializeField] private string _id;
    public override string Id => _id;

    public List<ChestRewardItem> rewards;
}

[System.Serializable]
public class ChestRewardItem
{
    public ItemData item;
    [FormerlySerializedAs("minAmount")]
    public int amount = 1;
}
