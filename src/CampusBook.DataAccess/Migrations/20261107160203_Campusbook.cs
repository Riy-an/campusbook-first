using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusBook.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Campusbook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StudentIdCardUrl",
                table: "StudentProfiles",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StudentIdCardUrl",
                table: "StudentProfiles");
        }
    }
}
