using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supply.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class NameArabicRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NameArablic",
                table: "AspNetRoles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NameArablic",
                table: "AspNetRoles");
        }
    }
}
