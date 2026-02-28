using System;
using System.Collections.Generic;

public abstract class AbstractEntity
{
    public float Health { get; set; } = 100.0f;
    public int Gold { get; set; } = 0;
    public Dictionary<EntityStatus.EType, EntityStatus> entityStatuses;

    //TODO: inventory+held item

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
        return entityStatuses;
    }
}