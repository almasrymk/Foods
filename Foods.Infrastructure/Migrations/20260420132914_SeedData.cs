using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Foods.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "IsDeleted", "Name", "Status" },
                values: new object[] { new Guid("92d19009-cb05-4645-a209-552c3e522a56"), false, "Admin", 0 });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "ImagePath", "IsDeleted", "Name", "Password", "RoleId", "Status", "Token", "UserName" },
                values: new object[] { new Guid("111d7bfe-8dda-4af9-b651-f4a8d9008b94"), null, false, "Admin", "1", new Guid("92d19009-cb05-4645-a209-552c3e522a56"), 0, null, "Admin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("92d19009-cb05-4645-a209-552c3e522a56"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("111d7bfe-8dda-4af9-b651-f4a8d9008b94"));
        }
    }
}
