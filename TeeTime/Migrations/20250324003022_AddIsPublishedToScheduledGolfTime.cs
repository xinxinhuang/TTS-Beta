using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeeTime.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPublishedToScheduledGolfTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "ScheduledGolfTimes",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "ScheduledGolfTimes");
        }
    }
}
