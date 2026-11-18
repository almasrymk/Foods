using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Foods.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CountryDialect : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CountryId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Dialect = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemCountries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CountryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemCountries", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Countries",
                columns: new[] { "Id", "Dialect", "IsDeleted", "Name", "Status" },
                values: new object[,]
                {
                    { new Guid("a1111111-1111-4111-8111-111111111101"), "مصري", false, "مصر", 0 },
                    { new Guid("a1111111-1111-4111-8111-111111111102"), "شامي", false, "سوريا", 0 },
                    { new Guid("a1111111-1111-4111-8111-111111111103"), "شامي", false, "لبنان", 0 },
                    { new Guid("a1111111-1111-4111-8111-111111111104"), "شامي", false, "فلسطين", 0 },
                    { new Guid("a1111111-1111-4111-8111-111111111105"), "شامي", false, "الأردن", 0 },
                    { new Guid("a1111111-1111-4111-8111-111111111106"), "خليجي", false, "السعودية", 0 },
                    { new Guid("a1111111-1111-4111-8111-111111111107"), "خليجي", false, "الإمارات", 0 },
                    { new Guid("a1111111-1111-4111-8111-111111111108"), "خليجي", false, "الكويت", 0 },
                    { new Guid("a1111111-1111-4111-8111-111111111109"), "مغربي", false, "المغرب", 0 },
                    { new Guid("a1111111-1111-4111-8111-11111111110a"), "تونسي", false, "تونس", 0 },
                    { new Guid("a1111111-1111-4111-8111-11111111110b"), "جزائري", false, "الجزائر", 0 },
                    { new Guid("a1111111-1111-4111-8111-11111111110c"), "عراقي", false, "العراق", 0 },
                    { new Guid("a1111111-1111-4111-8111-11111111110d"), "يمني", false, "اليمن", 0 },
                    { new Guid("a1111111-1111-4111-8111-11111111110e"), "خليجي", false, "عُمان", 0 },
                    { new Guid("a1111111-1111-4111-8111-11111111110f"), "خليجي", false, "قطر", 0 },
                    { new Guid("a1111111-1111-4111-8111-111111111111"), "خليجي", false, "البحرين", 0 },
                    { new Guid("a1111111-1111-4111-8111-111111111112"), "ليبي", false, "ليبيا", 0 },
                    { new Guid("a1111111-1111-4111-8111-111111111113"), "سوداني", false, "السودان", 0 },
                    { new Guid("a1111111-1111-4111-8111-111111111114"), "صومالي", false, "الصومال", 0 },
                    { new Guid("a1111111-1111-4111-8111-111111111115"), "جيبوتي", false, "جيبوتي", 0 },
                    { new Guid("a1111111-1111-4111-8111-111111111116"), "حساني", false, "موريتانيا", 0 },
                    { new Guid("a1111111-1111-4111-8111-111111111117"), "قمري", false, "جزر القمر", 0 }
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("111d7bfe-8dda-4af9-b651-f4a8d9008b94"),
                column: "CountryId",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Countries");

            migrationBuilder.DropTable(
                name: "ItemCountries");

            migrationBuilder.DropColumn(
                name: "CountryId",
                table: "Users");
        }
    }
}
