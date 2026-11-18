using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Foods.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnsureAdminPassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE [Users]
                SET [Password] = N'Admin1234',
                    [IsDeleted] = 0,
                    [RoleId] = '92d19009-cb05-4645-a209-552c3e522a56'
                WHERE [UserName] = N'Admin';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
