using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusBook.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class campusbook_2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdCardChangeCount",
                table: "StudentProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdCardChangeCount",
                table: "StudentProfiles");
        }
    }
}
