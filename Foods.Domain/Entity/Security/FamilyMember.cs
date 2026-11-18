namespace Foods.Domain.Entity.Security
{
    public class FamilyMember : BaseEntity
    {
        public Guid UserId { get; set; }
        public string? Name { get; set; }
        /// <summary>Legacy/fallback age. When BirthDate is set the age is always derived from it.</summary>
        public int Age { get; set; }
        public DateTime? BirthDate { get; set; }
        /// <summary>Relation to the account owner: أب، أم، زوج، زوجة، ابن، بنت، أخ، أخت، جد، جدة or free text.</summary>
        public string? Relation { get; set; }
        public string? ImagePath { get; set; }

        /// <summary>Age in whole years on the given day (birth date first, stored age as fallback).</summary>
        public int AgeOn(DateTime today) => BirthDate is DateTime b ? AgeFrom(b, today) : Age;

        public static int AgeFrom(DateTime birthDate, DateTime today)
        {
            var age = today.Year - birthDate.Year;
            if (today.Month < birthDate.Month || (today.Month == birthDate.Month && today.Day < birthDate.Day))
                age--;
            return Math.Clamp(age, 0, 120);
        }
    }
}
