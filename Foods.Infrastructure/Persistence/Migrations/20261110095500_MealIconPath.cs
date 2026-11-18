using Foods.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Foods.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(FoodsDbContext))]
    [Migration("20261110095500_MealIconPath")]
    public partial class MealIconPath : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IconPath",
                table: "Meals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Meals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TextColor",
                table: "Meals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Sort",
                table: "Meals",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IconPath", table: "Meals");
            migrationBuilder.DropColumn(name: "Color", table: "Meals");
            migrationBuilder.DropColumn(name: "TextColor", table: "Meals");
            migrationBuilder.DropColumn(name: "Sort", table: "Meals");
        }
    }
}
