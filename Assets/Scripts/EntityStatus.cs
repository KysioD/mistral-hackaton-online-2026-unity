using System;
using UnityEngine;

public abstract class EntityStatus
{
    public enum EType
    {
        Wanted, Ill
    };

    public abstract string Serialize();

    public abstract string GetIdentifier();

    public abstract EType GetType();

    public override int GetHashCode()
    {
        return HashCode.Combine(GetType());
    }

    public class Wanted : EntityStatus
    {
        public float Bounty { get; set; }
        public EViolation Violation { get; set; }

        public override string Serialize()
        {
            return "{  }";
        }

        public override string GetIdentifier()
        {
            return "wanted";
        }

        public override EType GetType()
        {
            return EType.Wanted;
        }

        public enum EViolation
        {
            None = 0,
            Murder,
            Theft
        }
    }

    public class Ill : EntityStatus
    {
        public EIllness Illness { get; set; }

        public override string Serialize()
        {
            throw new NotImplementedException();
        }

        public override string GetIdentifier()
        {
            return "ill";
        }

        public override EType GetType()
        {
            return EType.Ill;
        }

        public enum EIllness
        {
            Cold = 0,
            Poison
        }
    }
}
