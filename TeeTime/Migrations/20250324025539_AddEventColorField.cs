using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeeTime.Migrations
{
    /// <inheritdoc />
    public partial class AddEventColorField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EventColor",
                schema: "dbo",
                table: "Events",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EventColor",
                schema: "dbo",
                table: "Events");
        }
    }
}
