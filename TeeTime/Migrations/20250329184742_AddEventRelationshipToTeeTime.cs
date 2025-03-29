using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeeTime.Migrations
{
    /// <inheritdoc />
    public partial class AddEventRelationshipToTeeTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_ScheduledGolfTime_ScheduledGolfTimeID",
                schema: "Beta",
                table: "Reservations");

            migrationBuilder.DropTable(
                name: "ScheduledGolfTime",
                schema: "Beta");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_ScheduledGolfTimeID",
                schema: "Beta",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "ScheduledGolfTimeID",
                schema: "Beta",
                table: "Reservations");

            migrationBuilder.AddColumn<int>(
                name: "EventID",
                schema: "Beta",
                table: "TeeTimes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeeTimes_EventID",
                schema: "Beta",
                table: "TeeTimes",
                column: "EventID");

            migrationBuilder.AddForeignKey(
                name: "FK_TeeTimes_Events_EventID",
                schema: "Beta",
                table: "TeeTimes",
                column: "EventID",
                principalSchema: "Beta",
                principalTable: "Events",
                principalColumn: "EventID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TeeTimes_Events_EventID",
                schema: "Beta",
                table: "TeeTimes");

            migrationBuilder.DropIndex(
                name: "IX_TeeTimes_EventID",
                schema: "Beta",
                table: "TeeTimes");

            migrationBuilder.DropColumn(
                name: "EventID",
                schema: "Beta",
                table: "TeeTimes");

            migrationBuilder.AddColumn<int>(
                name: "ScheduledGolfTimeID",
                schema: "Beta",
                table: "Reservations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ScheduledGolfTime",
                schema: "Beta",
                columns: table => new
                {
                    ScheduledGolfTimeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventID = table.Column<int>(type: "int", nullable: true),
                    GolfSessionInterval = table.Column<int>(type: "int", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    ScheduledDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScheduledTime = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledGolfTime", x => x.ScheduledGolfTimeID);
                    table.ForeignKey(
                        name: "FK_ScheduledGolfTime_Events_EventID",
                        column: x => x.EventID,
                        principalSchema: "Beta",
                        principalTable: "Events",
                        principalColumn: "EventID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_ScheduledGolfTimeID",
                schema: "Beta",
                table: "Reservations",
                column: "ScheduledGolfTimeID");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledGolfTime_EventID",
                schema: "Beta",
                table: "ScheduledGolfTime",
                column: "EventID");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_ScheduledGolfTime_ScheduledGolfTimeID",
                schema: "Beta",
                table: "Reservations",
                column: "ScheduledGolfTimeID",
                principalSchema: "Beta",
                principalTable: "ScheduledGolfTime",
                principalColumn: "ScheduledGolfTimeID");
        }
    }
}
