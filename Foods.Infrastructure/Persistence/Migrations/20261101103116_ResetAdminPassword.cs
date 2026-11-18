using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Foods.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ResetAdminPassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("111d7bfe-8dda-4af9-b651-f4a8d9008b94"),
                column: "Password",
                value: "Admin1234");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("111d7bfe-8dda-4af9-b651-f4a8d9008b94"),
                column: "Password",
                value: "1");
        }
    }
}
