using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeeTime.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerColumnsToStandingTeeTimeRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Player2ID",
                schema: "Beta",
                table: "StandingTeeTimeRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Player3ID",
                schema: "Beta",
                table: "StandingTeeTimeRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Player4ID",
                schema: "Beta",
                table: "StandingTeeTimeRequests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StandingTeeTimeRequests_Player2ID",
                schema: "Beta",
                table: "StandingTeeTimeRequests",
                column: "Player2ID");

            migrationBuilder.CreateIndex(
                name: "IX_StandingTeeTimeRequests_Player3ID",
                schema: "Beta",
                table: "StandingTeeTimeRequests",
                column: "Player3ID");

            migrationBuilder.CreateIndex(
                name: "IX_StandingTeeTimeRequests_Player4ID",
                schema: "Beta",
                table: "StandingTeeTimeRequests",
                column: "Player4ID");

            migrationBuilder.AddForeignKey(
                name: "FK_StandingTeeTimeRequests_Members_Player2ID",
                schema: "Beta",
                table: "StandingTeeTimeRequests",
                column: "Player2ID",
                principalSchema: "Beta",
                principalTable: "Members",
                principalColumn: "MemberID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StandingTeeTimeRequests_Members_Player3ID",
                schema: "Beta",
                table: "StandingTeeTimeRequests",
                column: "Player3ID",
                principalSchema: "Beta",
                principalTable: "Members",
                principalColumn: "MemberID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StandingTeeTimeRequests_Members_Player4ID",
                schema: "Beta",
                table: "StandingTeeTimeRequests",
                column: "Player4ID",
                principalSchema: "Beta",
                principalTable: "Members",
                principalColumn: "MemberID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StandingTeeTimeRequests_Members_Player2ID",
                schema: "Beta",
                table: "StandingTeeTimeRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_StandingTeeTimeRequests_Members_Player3ID",
                schema: "Beta",
                table: "StandingTeeTimeRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_StandingTeeTimeRequests_Members_Player4ID",
                schema: "Beta",
                table: "StandingTeeTimeRequests");

            migrationBuilder.DropIndex(
                name: "IX_StandingTeeTimeRequests_Player2ID",
                schema: "Beta",
                table: "StandingTeeTimeRequests");

            migrationBuilder.DropIndex(
                name: "IX_StandingTeeTimeRequests_Player3ID",
                schema: "Beta",
                table: "StandingTeeTimeRequests");

            migrationBuilder.DropIndex(
                name: "IX_StandingTeeTimeRequests_Player4ID",
                schema: "Beta",
                table: "StandingTeeTimeRequests");

            migrationBuilder.DropColumn(
                name: "Player2ID",
                schema: "Beta",
                table: "StandingTeeTimeRequests");

            migrationBuilder.DropColumn(
                name: "Player3ID",
                schema: "Beta",
                table: "StandingTeeTimeRequests");

            migrationBuilder.DropColumn(
                name: "Player4ID",
                schema: "Beta",
                table: "StandingTeeTimeRequests");
        }
    }
}
