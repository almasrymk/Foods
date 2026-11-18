namespace Foods.Application.Customer.Family
{
    using System.Net;
    using Microsoft.EntityFrameworkCore;
    using Foods.Domain.Shared;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.Security;
    using Foods.Application.Abstraction.Query;
    using Foods.Application.Abstraction.Command;
    using Foods.Application.Abstraction.Security;
    using Foods.Application.Security.Queries.Create;

    /// <summary>Age is always derived (birth date first, legacy stored age as fallback); Avatar is the automatic picture when ImagePath is empty.</summary>
    public sealed record FamilyMemberDto(Guid Id, string? Name, int Age, DateTime? BirthDate = null, string? Relation = null, string? ImagePath = null, string? Avatar = null);
    public sealed record GetFamilyQuery() : IQuery<List<FamilyMemberDto>>;
    public sealed record SaveFamilyCommand(IReadOnlyList<FamilyMemberInput> Members) : ICommand<List<FamilyMemberDto>>;

    /// <summary>Automatic avatar (client asset under assets/family) from relation gender and age band.</summary>
    public static class FamilyAvatar
    {
        static readonly HashSet<string> Male = new() { "أب", "زوج", "ابن", "أخ", "جد" };
        static readonly HashSet<string> Female = new() { "أم", "زوجة", "بنت", "أخت", "جدة" };

        public static string For(string? relation, int age)
        {
            var r = (relation ?? "").Trim();
            if (r == "جد") return "assets/family/grandpa.svg";
            if (r == "جدة") return "assets/family/grandma.svg";
            var male = Male.Contains(r);
            var female = Female.Contains(r);
            if (!male && !female) return "assets/family/person.svg";
            var band = age < 4 ? "baby" : age <= 15 ? "kid" : age <= 25 ? "young" : "adult";
            return (male, band) switch
            {
                (true, "baby") => "assets/family/baby-boy.svg",
                (true, "kid") => "assets/family/boy.svg",
                (true, "young") => "assets/family/young-man.svg",
                (true, _) => "assets/family/man.svg",
                (false, "baby") => "assets/family/baby-girl.svg",
                (false, "kid") => "assets/family/girl.svg",
                (false, "young") => "assets/family/young-woman.svg",
                _ => "assets/family/woman.svg"
            };
        }

        public static string PictureOf(FamilyMember m, DateTime today) =>
            !string.IsNullOrWhiteSpace(m.ImagePath) ? m.ImagePath! : For(m.Relation, m.AgeOn(today));

        public static FamilyMemberDto ToDto(FamilyMember m, DateTime today) =>
            new(m.Id, m.Name, m.AgeOn(today), m.BirthDate, m.Relation, m.ImagePath, For(m.Relation, m.AgeOn(today)));
    }

    public sealed class GetFamilyQueryHandler(IFoodsDbContext db, ICurrentUser currentUser) : IQueryHandler<GetFamilyQuery, List<FamilyMemberDto>>
    {
        public async Task<Result<List<FamilyMemberDto>>> Handle(GetFamilyQuery request, CancellationToken cancellationToken)
        {
            var today = DateTime.UtcNow.Date;
            var members = await db.Set<FamilyMember>()
                .Where(e => e.UserId == currentUser.UserId && !e.IsDeleted)
                .ToListAsync(cancellationToken);

            var rows = members
                .OrderByDescending(e => e.AgeOn(today))
                .ThenBy(e => e.Name)
                .Select(e => FamilyAvatar.ToDto(e, today))
                .ToList();
            return new Result<List<FamilyMemberDto>>(HttpStatusCode.OK, rows, null);
        }
    }

    public sealed class SaveFamilyCommandHandler(IFoodsDbContext db, ICurrentUser currentUser) : ICommandHandler<SaveFamilyCommand, List<FamilyMemberDto>>
    {
        public async Task<Result<List<FamilyMemberDto>>> Handle(SaveFamilyCommand request, CancellationToken cancellationToken)
        {
            Result<List<FamilyMemberDto>> Bad(string message) => new(HttpStatusCode.BadRequest, null, new List<string> { message });
            var today = DateTime.UtcNow.Date;

            if (request.Members == null || request.Members.Count == 0)
                return Bad("أدخل فرداً واحداً من الأسرة على الأقل");
            if (request.Members.Count > 20)
                return Bad("الحد الأقصى 20 فرداً");
            if (request.Members.Any(e => string.IsNullOrWhiteSpace(e.Name)))
                return Bad("اسم كل فرد مطلوب");
            if (request.Members.Any(e => e.Name!.Trim().Length > 60))
                return Bad("الاسم طويل جداً");
            if (request.Members.Any(e => e.BirthDate is DateTime d && (d.Date > today.AddDays(1) || d.Year < 1900)))
                return Bad("تاريخ الميلاد لا يمكن أن يكون في المستقبل");
            if (request.Members.Any(e => e.BirthDate == null && (e.Age < 0 || e.Age > 120)))
                return Bad("العمر يجب أن يكون بين 0 و 120");
            if (request.Members.Any(e => (e.Relation ?? "").Trim().Length > 30))
                return Bad("صلة القرابة طويلة جداً");
            if (request.Members.Any(e => !string.IsNullOrWhiteSpace(e.ImagePath) && !IsUpload(e.ImagePath!)))
                return Bad("الصورة غير صحيحة");

            var current = await db.Set<FamilyMember>().Where(e => e.UserId == currentUser.UserId && !e.IsDeleted).ToListAsync(cancellationToken);
            foreach (var member in current)
                member.IsDeleted = true;

            var saved = new List<FamilyMember>();
            foreach (var member in request.Members)
            {
                var birth = member.BirthDate?.Date;
                var entity = new FamilyMember
                {
                    Id = Guid.NewGuid(),
                    UserId = currentUser.UserId,
                    Name = member.Name!.Trim(),
                    BirthDate = birth,
                    // Age is not entered any more; it is derived from the birth date (kept for members without one).
                    Age = birth is DateTime b ? FamilyMember.AgeFrom(b, today) : member.Age,
                    Relation = string.IsNullOrWhiteSpace(member.Relation) ? null : member.Relation!.Trim(),
                    ImagePath = string.IsNullOrWhiteSpace(member.ImagePath) ? null : member.ImagePath!.Trim(),
                    IsDeleted = false,
                    Status = 0
                };
                db.Set<FamilyMember>().Add(entity);
                saved.Add(entity);
            }

            await db.SaveChangesAsync(cancellationToken);
            return new Result<List<FamilyMemberDto>>(HttpStatusCode.OK, saved.Select(e => FamilyAvatar.ToDto(e, today)).ToList(), null);
        }

        /// <summary>Only files from our own upload endpoint (/uploads/name.ext) are accepted.</summary>
        static bool IsUpload(string path) =>
            path.StartsWith("/uploads/", StringComparison.Ordinal) && !path.Contains("..") && path.IndexOfAny(new[] { '\\', ':', '?', '#' }) < 0;
    }
}
