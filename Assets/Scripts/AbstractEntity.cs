using System;
using System.Collections.Generic;
using Unity.Mathematics;

public abstract class AbstractEntity
{
    public float Health { get; set; } = 100.0f;
    public int Gold { get; set; } = UnityEngine.Random.Range(10, 1000);
    public Dictionary<EntityStatus.EType, EntityStatus> entityStatuses;

    public void Heal(int health)
    {
        this.Health += health;
    }

    public void Damage(int health)
    {
        this.Health -= health;
    }

    public void TransferGoldTo(AbstractEntity entityTo, int goldAmount)
    {
        entityTo.Gold += goldAmount;
        this.Gold -= goldAmount;
    }

    public void AddStatus(EntityStatus status)
    {
        entityStatuses.Add(status.GetType(), status);
    }

    public void RemoveStatus(EntityStatus.EType statusType)
    {
        entityStatuses.Remove(statusType);
    }

    public bool HasStatus(EntityStatus.EType status)
    {
        return entityStatuses.ContainsKey(status);
    }

    public Dictionary<EntityStatus.EType, EntityStatus> GetStatuses()
    {
        return entityStatuses ?? new Dictionary<EntityStatus.EType, EntityStatus>();
    }
}