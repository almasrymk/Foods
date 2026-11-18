namespace Foods.Domain.Entity.Security
{
    public static class RoleIds
    {
        public static readonly Guid Admin = Guid.Parse("92d19009-cb05-4645-a209-552c3e522a56");
        public static readonly Guid Customer = Guid.Parse("42997ac6-a9c6-4e97-f108-3a1a18b2ba01");
    }

    public static class RoleNames
    {
        public const string Admin = "Admin";
        public const string Customer = "Customer";
    }
}
