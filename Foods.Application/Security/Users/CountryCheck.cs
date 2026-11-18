namespace Foods.Application.Security.Users
{
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.Security;
    using CountryEntity = Foods.Domain.Entity.MasterData.Country;

    internal static class CountryCheck
    {
        public static async Task<string?> Validate(IRepository<CountryEntity> countries, Guid roleId, Guid? countryId)
        {
            if (roleId == RoleIds.Customer && (countryId == null || countryId == Guid.Empty))
                return "البلد مطلوبة";
            if (countryId == null || countryId == Guid.Empty)
                return null;

            var country = await countries.GetByFilterAsync(e => e.Id == countryId);
            return country == null ? "البلد غير موجودة" : null;
        }
    }
}
