using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeeTime.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MembershipCategories",
                columns: table => new
                {
                    MembershipCategoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MembershipName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CanSponsor = table.Column<bool>(type: "bit", nullable: false),
                    CanMakeStandingTeeTime = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembershipCategories", x => x.MembershipCategoryID);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleDescription = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleID);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledGolfTimes",
                columns: table => new
                {
                    ScheduledGolfTimeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScheduledDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScheduledTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    GolfSessionInterval = table.Column<int>(type: "int", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledGolfTimes", x => x.ScheduledGolfTimeID);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    RoleID = table.Column<int>(type: "int", nullable: false),
                    MembershipCategoryID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserID);
                    table.ForeignKey(
                        name: "FK_Users_MembershipCategories_MembershipCategoryID",
                        column: x => x.MembershipCategoryID,
                        principalTable: "MembershipCategories",
                        principalColumn: "MembershipCategoryID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleID",
                        column: x => x.RoleID,
                        principalTable: "Roles",
                        principalColumn: "RoleID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Members",
                columns: table => new
                {
                    MemberID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    MembershipCategoryID = table.Column<int>(type: "int", nullable: false),
                    JoinDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    MemberPhone = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: true),
                    GoodStanding = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Members", x => x.MemberID);
                    table.ForeignKey(
                        name: "FK_Members_MembershipCategories_MembershipCategoryID",
                        column: x => x.MembershipCategoryID,
                        principalTable: "MembershipCategories",
                        principalColumn: "MembershipCategoryID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Members_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MemberUpgrades",
                columns: table => new
                {
                    ApplicationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AlternatePhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Occupation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CompanyAddress = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CompanyPostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CompanyPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Sponsor1MemberID = table.Column<int>(type: "int", nullable: true),
                    Sponsor2MemberID = table.Column<int>(type: "int", nullable: true),
                    DesiredMembershipCategoryID = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovalByUserID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberUpgrades", x => x.ApplicationID);
                    table.ForeignKey(
                        name: "FK_MemberUpgrades_Members_Sponsor1MemberID",
                        column: x => x.Sponsor1MemberID,
                        principalTable: "Members",
                        principalColumn: "MemberID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberUpgrades_Members_Sponsor2MemberID",
                        column: x => x.Sponsor2MemberID,
                        principalTable: "Members",
                        principalColumn: "MemberID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberUpgrades_MembershipCategories_DesiredMembershipCategoryID",
                        column: x => x.DesiredMembershipCategoryID,
                        principalTable: "MembershipCategories",
                        principalColumn: "MembershipCategoryID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberUpgrades_Users_ApprovalByUserID",
                        column: x => x.ApprovalByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberUpgrades_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reservations",
                columns: table => new
                {
                    ReservationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberID = table.Column<int>(type: "int", nullable: false),
                    ScheduledGolfTimeID = table.Column<int>(type: "int", nullable: false),
                    ReservationMadeDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReservationStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NumberOfPlayers = table.Column<int>(type: "int", nullable: false),
                    NumberOfCarts = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.ReservationID);
                    table.ForeignKey(
                        name: "FK_Reservations_Members_MemberID",
                        column: x => x.MemberID,
                        principalTable: "Members",
                        principalColumn: "MemberID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservations_ScheduledGolfTimes_ScheduledGolfTimeID",
                        column: x => x.ScheduledGolfTimeID,
                        principalTable: "ScheduledGolfTimes",
                        principalColumn: "ScheduledGolfTimeID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StandingTeeTimeRequests",
                columns: table => new
                {
                    RequestID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberID = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DesiredTeeTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    PriorityNumber = table.Column<int>(type: "int", nullable: true),
                    ApprovedTeeTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    ApprovedByUserID = table.Column<int>(type: "int", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StandingTeeTimeRequests", x => x.RequestID);
                    table.ForeignKey(
                        name: "FK_StandingTeeTimeRequests_Members_MemberID",
                        column: x => x.MemberID,
                        principalTable: "Members",
                        principalColumn: "MemberID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StandingTeeTimeRequests_Users_ApprovedByUserID",
                        column: x => x.ApprovedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Members_MembershipCategoryID",
                table: "Members",
                column: "MembershipCategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_Members_UserID",
                table: "Members",
                column: "UserID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemberUpgrades_ApprovalByUserID",
                table: "MemberUpgrades",
                column: "ApprovalByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_MemberUpgrades_DesiredMembershipCategoryID",
                table: "MemberUpgrades",
                column: "DesiredMembershipCategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_MemberUpgrades_Sponsor1MemberID",
                table: "MemberUpgrades",
                column: "Sponsor1MemberID");

            migrationBuilder.CreateIndex(
                name: "IX_MemberUpgrades_Sponsor2MemberID",
                table: "MemberUpgrades",
                column: "Sponsor2MemberID");

            migrationBuilder.CreateIndex(
                name: "IX_MemberUpgrades_UserID",
                table: "MemberUpgrades",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_MemberID",
                table: "Reservations",
                column: "MemberID");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_ScheduledGolfTimeID",
                table: "Reservations",
                column: "ScheduledGolfTimeID");

            migrationBuilder.CreateIndex(
                name: "IX_StandingTeeTimeRequests_ApprovedByUserID",
                table: "StandingTeeTimeRequests",
                column: "ApprovedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_StandingTeeTimeRequests_MemberID",
                table: "StandingTeeTimeRequests",
                column: "MemberID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_MembershipCategoryID",
                table: "Users",
                column: "MembershipCategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleID",
                table: "Users",
                column: "RoleID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemberUpgrades");

            migrationBuilder.DropTable(
                name: "Reservations");

            migrationBuilder.DropTable(
                name: "StandingTeeTimeRequests");

            migrationBuilder.DropTable(
                name: "ScheduledGolfTimes");

            migrationBuilder.DropTable(
                name: "Members");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "MembershipCategories");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
