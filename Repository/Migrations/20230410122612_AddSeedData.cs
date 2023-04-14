using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Active", "Email", "FullName", "GenderId", "ImagePath", "JobId", "NumberFamilyMembers", "Password", "ReligionId", "Status" },
                values: new object[] { 1L, false, "memo_mad2007@hotmail.com", "Admen", 0L, null, 0L, 0, "2C7SY4BTJAW8CaJoivJAUA==", 0L, 0 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1L);
        }
    }
}
