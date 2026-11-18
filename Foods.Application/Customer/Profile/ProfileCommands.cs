namespace Foods.Application.Customer.Profile
{
    using System.Net;
    using Microsoft.EntityFrameworkCore;
    using Foods.Application.Abstraction.Command;
    using Foods.Application.Abstraction.Query;
    using Foods.Application.Abstraction.Security;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.MasterData;
    using Foods.Domain.Entity.Security;
    using Foods.Domain.Shared;

    public sealed record CountryOption(Guid Id, string? Name, string? Dialect);
    public sealed record GetCountriesQuery() : IQuery<List<CountryOption>>;
    public sealed record ProfileDto(Guid? CountryId, string? CountryName, string? Dialect, string? Name = null, string? UserName = null, string? ImagePath = null);
    /// <summary>Sets (an uploaded /uploads/... path) or removes (null) the account photo.</summary>
    public sealed record SaveProfilePhotoCommand(string? ImagePath) : ICommand<ProfileDto>;
    public sealed record SaveProfileCommand(string? Name) : ICommand<ProfileDto>;
    public sealed record GetProfileQuery() : IQuery<ProfileDto>;
    public sealed record SaveCountryCommand(Guid CountryId) : ICommand<ProfileDto>;

    public sealed class GetCountriesQueryHandler(IFoodsDbContext db) : IQueryHandler<GetCountriesQuery, List<CountryOption>>
    {
        public async Task<Result<List<CountryOption>>> Handle(GetCountriesQuery request, CancellationToken cancellationToken)
        {
            var rows = await db.Set<Country>()
                .Where(e => !e.IsDeleted)
                .OrderBy(e => e.Name)
                .Select(e => new CountryOption(e.Id, e.Name, e.Dialect))
                .ToListAsync(cancellationToken);
            return new Result<List<CountryOption>>(HttpStatusCode.OK, rows, null);
        }
    }

    public sealed class GetProfileQueryHandler(IFoodsDbContext db, ICurrentUser currentUser) : IQueryHandler<GetProfileQuery, ProfileDto>
    {
        public async Task<Result<ProfileDto>> Handle(GetProfileQuery request, CancellationToken cancellationToken)
        {
            var user = await db.Set<User>().FirstOrDefaultAsync(e => e.Id == currentUser.UserId && !e.IsDeleted, cancellationToken);
            if (user == null)
                return new Result<ProfileDto>(HttpStatusCode.NotFound, null, new List<string> { "المستخدم غير موجود" });

            var country = user.CountryId == null
                ? null
                : await db.Set<Country>().FirstOrDefaultAsync(e => e.Id == user.CountryId && !e.IsDeleted, cancellationToken);
            return new Result<ProfileDto>(HttpStatusCode.OK, new ProfileDto(user.CountryId, country?.Name, country?.Dialect, user.Name, user.UserName, user.ImagePath), null);
        }
    }

    public sealed class SaveCountryCommandHandler(IFoodsDbContext db, ICurrentUser currentUser) : ICommandHandler<SaveCountryCommand, ProfileDto>
    {
        public async Task<Result<ProfileDto>> Handle(SaveCountryCommand request, CancellationToken cancellationToken)
        {
            var country = await db.Set<Country>().FirstOrDefaultAsync(e => e.Id == request.CountryId && !e.IsDeleted, cancellationToken);
            if (country == null)
                return new Result<ProfileDto>(HttpStatusCode.BadRequest, null, new List<string> { "البلد غير موجودة" });

            var user = await db.Set<User>().FirstOrDefaultAsync(e => e.Id == currentUser.UserId && !e.IsDeleted, cancellationToken);
            if (user == null)
                return new Result<ProfileDto>(HttpStatusCode.NotFound, null, new List<string> { "المستخدم غير موجود" });

            user.CountryId = country.Id;
            await db.SaveChangesAsync(cancellationToken);
            return new Result<ProfileDto>(HttpStatusCode.OK, new ProfileDto(country.Id, country.Name, country.Dialect, user.Name, user.UserName, user.ImagePath), null);
        }
    }
}

namespace Foods.Application.Customer.Profile
{
    using System.Net;
    using Microsoft.EntityFrameworkCore;
    using Foods.Application.Abstraction.Command;
    using Foods.Application.Abstraction.Security;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.MasterData;
    using Foods.Domain.Entity.Security;
    using Foods.Domain.Shared;

    public sealed class SaveProfileCommandHandler(IFoodsDbContext db, ICurrentUser currentUser) : ICommandHandler<SaveProfileCommand, ProfileDto>
    {
        public async Task<Result<ProfileDto>> Handle(SaveProfileCommand request, CancellationToken cancellationToken)
        {
            var name = (request.Name ?? "").Trim();
            if (name.Length < 2 || name.Length > 100)
                return new Result<ProfileDto>(HttpStatusCode.BadRequest, null, new List<string> { "الاسم يجب أن يكون من 2 إلى 100 حرف" });

            var user = await db.Set<User>().FirstOrDefaultAsync(e => e.Id == currentUser.UserId && !e.IsDeleted, cancellationToken);
            if (user == null)
                return new Result<ProfileDto>(HttpStatusCode.NotFound, null, new List<string> { "المستخدم غير موجود" });

            user.Name = name;
            await db.SaveChangesAsync(cancellationToken);
            var country = user.CountryId == null
                ? null
                : await db.Set<Country>().FirstOrDefaultAsync(e => e.Id == user.CountryId && !e.IsDeleted, cancellationToken);
            return new Result<ProfileDto>(HttpStatusCode.OK, new ProfileDto(user.CountryId, country?.Name, country?.Dialect, user.Name, user.UserName, user.ImagePath), null);
        }
    }
}

namespace Foods.Application.Customer.Profile
{
    using System.Net;
    using Microsoft.EntityFrameworkCore;
    using Foods.Application.Abstraction.Command;
    using Foods.Application.Abstraction.Security;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.MasterData;
    using Foods.Domain.Entity.Security;
    using Foods.Domain.Shared;

    public sealed class SaveProfilePhotoCommandHandler(IFoodsDbContext db, ICurrentUser currentUser) : ICommandHandler<SaveProfilePhotoCommand, ProfileDto>
    {
        public async Task<Result<ProfileDto>> Handle(SaveProfilePhotoCommand request, CancellationToken cancellationToken)
        {
            var path = string.IsNullOrWhiteSpace(request.ImagePath) ? null : request.ImagePath!.Trim();
            // Only files produced by our own upload endpoint are accepted.
            if (path != null && (!path.StartsWith("/uploads/", StringComparison.Ordinal) || path.Contains("..") || path.IndexOfAny(new[] { '\\', ':', '?', '#' }) >= 0 || path.Length > 400))
                return new Result<ProfileDto>(HttpStatusCode.BadRequest, null, new List<string> { "الصورة غير صحيحة" });

            var user = await db.Set<User>().FirstOrDefaultAsync(e => e.Id == currentUser.UserId && !e.IsDeleted, cancellationToken);
            if (user == null)
                return new Result<ProfileDto>(HttpStatusCode.NotFound, null, new List<string> { "المستخدم غير موجود" });

            user.ImagePath = path;
            await db.SaveChangesAsync(cancellationToken);
            var country = user.CountryId == null
                ? null
                : await db.Set<Country>().FirstOrDefaultAsync(e => e.Id == user.CountryId && !e.IsDeleted, cancellationToken);
            return new Result<ProfileDto>(HttpStatusCode.OK, new ProfileDto(user.CountryId, country?.Name, country?.Dialect, user.Name, user.UserName, user.ImagePath), null);
        }
    }
}
