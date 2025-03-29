using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeeTime.Migrations
{
    /// <inheritdoc />
    public partial class RemoveScheduledGolfTimeAndUpdateTeeTimeRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_ScheduledGolfTimes_ScheduledGolfTimeID",
                schema: "Beta",
                table: "Reservations");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduledGolfTimes_Events_EventID",
                schema: "Beta",
                table: "ScheduledGolfTimes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ScheduledGolfTimes",
                schema: "Beta",
                table: "ScheduledGolfTimes");

            migrationBuilder.DropColumn(
                name: "ReservationId",
                schema: "Beta",
                table: "TeeTimes");

            migrationBuilder.RenameTable(
                name: "ScheduledGolfTimes",
                schema: "Beta",
                newName: "ScheduledGolfTime",
                newSchema: "Beta");

            migrationBuilder.RenameIndex(
                name: "IX_ScheduledGolfTimes_EventID",
                schema: "Beta",
                table: "ScheduledGolfTime",
                newName: "IX_ScheduledGolfTime_EventID");

            migrationBuilder.AddColumn<int>(
                name: "TotalPlayersBooked",
                schema: "Beta",
                table: "TeeTimes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "ScheduledGolfTimeID",
                schema: "Beta",
                table: "Reservations",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "TeeTimeId",
                schema: "Beta",
                table: "Reservations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ScheduledGolfTime",
                schema: "Beta",
                table: "ScheduledGolfTime",
                column: "ScheduledGolfTimeID");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_TeeTimeId",
                schema: "Beta",
                table: "Reservations",
                column: "TeeTimeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_ScheduledGolfTime_ScheduledGolfTimeID",
                schema: "Beta",
                table: "Reservations",
                column: "ScheduledGolfTimeID",
                principalSchema: "Beta",
                principalTable: "ScheduledGolfTime",
                principalColumn: "ScheduledGolfTimeID");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_TeeTimes_TeeTimeId",
                schema: "Beta",
                table: "Reservations",
                column: "TeeTimeId",
                principalSchema: "Beta",
                principalTable: "TeeTimes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduledGolfTime_Events_EventID",
                schema: "Beta",
                table: "ScheduledGolfTime",
                column: "EventID",
                principalSchema: "Beta",
                principalTable: "Events",
                principalColumn: "EventID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_ScheduledGolfTime_ScheduledGolfTimeID",
                schema: "Beta",
                table: "Reservations");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_TeeTimes_TeeTimeId",
                schema: "Beta",
                table: "Reservations");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduledGolfTime_Events_EventID",
                schema: "Beta",
                table: "ScheduledGolfTime");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_TeeTimeId",
                schema: "Beta",
                table: "Reservations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ScheduledGolfTime",
                schema: "Beta",
                table: "ScheduledGolfTime");

            migrationBuilder.DropColumn(
                name: "TotalPlayersBooked",
                schema: "Beta",
                table: "TeeTimes");

            migrationBuilder.DropColumn(
                name: "TeeTimeId",
                schema: "Beta",
                table: "Reservations");

            migrationBuilder.RenameTable(
                name: "ScheduledGolfTime",
                schema: "Beta",
                newName: "ScheduledGolfTimes",
                newSchema: "Beta");

            migrationBuilder.RenameIndex(
                name: "IX_ScheduledGolfTime_EventID",
                schema: "Beta",
                table: "ScheduledGolfTimes",
                newName: "IX_ScheduledGolfTimes_EventID");

            migrationBuilder.AddColumn<int>(
                name: "ReservationId",
                schema: "Beta",
                table: "TeeTimes",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ScheduledGolfTimeID",
                schema: "Beta",
                table: "Reservations",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ScheduledGolfTimes",
                schema: "Beta",
                table: "ScheduledGolfTimes",
                column: "ScheduledGolfTimeID");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_ScheduledGolfTimes_ScheduledGolfTimeID",
                schema: "Beta",
                table: "Reservations",
                column: "ScheduledGolfTimeID",
                principalSchema: "Beta",
                principalTable: "ScheduledGolfTimes",
                principalColumn: "ScheduledGolfTimeID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduledGolfTimes_Events_EventID",
                schema: "Beta",
                table: "ScheduledGolfTimes",
                column: "EventID",
                principalSchema: "Beta",
                principalTable: "Events",
                principalColumn: "EventID",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
