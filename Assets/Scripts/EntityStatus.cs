using System;

public abstract class EntityStatus
{
    public enum EType
    {
        HealthProblem
    };

    public abstract string Serialize();

    public abstract string GetIdentifier();

    public abstract EType GetType();

    public override int GetHashCode()
    {
        return HashCode.Combine(GetType());
    }

    public class HealthProblem : EntityStatus
    {
        public EProblem Problem { get; set; }

        public override string Serialize()
        {
            throw new NotImplementedException();
        }

        public override string GetIdentifier()
        {
            return "healthProblem";
        }

        public override EType GetType()
        {
            return EType.HealthProblem;
        }

        public enum EProblem
        {
            Cold = 0,
            Poison,
            Wound,
            Allergy,
            Urticaria
        }
    }
}
