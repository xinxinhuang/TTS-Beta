using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeeTime.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelsAfterStandingTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add a new temporary column to hold the integer representation
            migrationBuilder.AddColumn<int>(
                name: "DayOfWeek_Temp",
                schema: "Beta",
                table: "StandingTeeTimeRequests",
                type: "int",
                nullable: false,
                defaultValue: 0); // Default value is needed temporarily

            // Step 2: Update the new temporary column based on the old string column
            // IMPORTANT: Ensure your string values match exactly (e.g., 'Monday', 'Tuesday')
            migrationBuilder.Sql(
                @"UPDATE [Beta].[StandingTeeTimeRequests]
                  SET DayOfWeek_Temp = CASE LTRIM(RTRIM([DayOfWeek])) -- Trim whitespace just in case
                    WHEN 'Sunday' THEN 0
                    WHEN 'Monday' THEN 1
                    WHEN 'Tuesday' THEN 2
                    WHEN 'Wednesday' THEN 3
                    WHEN 'Thursday' THEN 4
                    WHEN 'Friday' THEN 5
                    WHEN 'Saturday' THEN 6
                    ELSE 0 -- Or handle potential invalid strings appropriately
                  END");

            // Step 3: Drop the old string DayOfWeek column
            migrationBuilder.DropColumn(
                name: "DayOfWeek",
                schema: "Beta",
                table: "StandingTeeTimeRequests");

            // Step 4: Rename the new temporary column to DayOfWeek
            migrationBuilder.RenameColumn(
                name: "DayOfWeek_Temp",
                schema: "Beta",
                table: "StandingTeeTimeRequests",
                newName: "DayOfWeek");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "Beta",
                table: "StandingTeeTimeRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                schema: "Beta",
                table: "Reservations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StandingRequestID",
                schema: "Beta",
                table: "Reservations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                schema: "Beta",
                table: "Reservations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_StandingRequestID",
                schema: "Beta",
                table: "Reservations",
                column: "StandingRequestID");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_StandingTeeTimeRequests_StandingRequestID",
                schema: "Beta",
                table: "Reservations",
                column: "StandingRequestID",
                principalSchema: "Beta",
                principalTable: "StandingTeeTimeRequests",
                principalColumn: "RequestID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse the process for rollback

            // Step 1: Add a temporary string column
            migrationBuilder.AddColumn<string>(
                name: "DayOfWeek_Temp",
                schema: "Beta",
                table: "StandingTeeTimeRequests",
                type: "nvarchar(50)", // Use a reasonable string length
                nullable: false,
                defaultValue: "");

            // Step 2: Update the temp string column based on the integer values
            migrationBuilder.Sql(
                @"UPDATE [Beta].[StandingTeeTimeRequests]
                  SET DayOfWeek_Temp = CASE [DayOfWeek]
                    WHEN 0 THEN 'Sunday'
                    WHEN 1 THEN 'Monday'
                    WHEN 2 THEN 'Tuesday'
                    WHEN 3 THEN 'Wednesday'
                    WHEN 4 THEN 'Thursday'
                    WHEN 5 THEN 'Friday'
                    WHEN 6 THEN 'Saturday'
                    ELSE '' -- Default fallback
                  END");

            // Step 3: Drop the integer DayOfWeek column
            migrationBuilder.DropColumn(
                name: "DayOfWeek",
                schema: "Beta",
                table: "StandingTeeTimeRequests");

            // Step 4: Rename the temporary string column back to DayOfWeek
            migrationBuilder.RenameColumn(
                name: "DayOfWeek_Temp",
                schema: "Beta",
                table: "StandingTeeTimeRequests",
                newName: "DayOfWeek");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_StandingTeeTimeRequests_StandingRequestID",
                schema: "Beta",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_StandingRequestID",
                schema: "Beta",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "Beta",
                table: "StandingTeeTimeRequests");

            migrationBuilder.DropColumn(
                name: "Notes",
                schema: "Beta",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "StandingRequestID",
                schema: "Beta",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "Type",
                schema: "Beta",
                table: "Reservations");
        }
    }
}
