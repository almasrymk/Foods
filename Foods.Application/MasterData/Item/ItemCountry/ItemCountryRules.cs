namespace Foods.Application.MasterData.Item.ItemCountry
{
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.MasterData.Item;

    internal static class ItemCountryRules
    {
        public static async Task<(string? Error, bool IsPrimary)> Prepare(
            IRepository<ItemCountry> repository,
            Guid itemId,
            Guid countryId,
            Guid? currentId,
            bool requestedPrimary)
        {
            var rows = (await repository.GetListByFilterAsync(e => e.ItemId == itemId))?.ToList() ?? new List<ItemCountry>();
            if (rows.Any(e => e.CountryId == countryId && e.Id != currentId))
                return ("البلد مضافة على الأكلة دي", false);

            var others = rows.Where(e => e.Id != currentId).ToList();
            var isPrimary = requestedPrimary || others.All(e => !e.IsPrimary);
            if (!isPrimary && others.All(e => !e.IsPrimary))
                return ("اختار البلد الرئيسية للأكلة", false);

            if (isPrimary)
            {
                foreach (var other in others.Where(e => e.IsPrimary))
                {
                    other.IsPrimary = false;
                    await repository.UpdateAsync(other);
                }
            }

            return (null, isPrimary);
        }
    }
}
