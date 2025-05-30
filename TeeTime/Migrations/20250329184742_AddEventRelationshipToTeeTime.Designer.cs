﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TeeTime.Data;

#nullable disable

namespace TeeTime.Migrations
{
    [DbContext(typeof(TeeTimeDbContext))]
    [Migration("20250329184742_AddEventRelationshipToTeeTime")]
    partial class AddEventRelationshipToTeeTime
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("Beta")
                .HasAnnotation("ProductVersion", "9.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("TeeTime.Models.Event", b =>
                {
                    b.Property<int>("EventID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("EventID"));

                    b.Property<TimeSpan>("EndTime")
                        .HasColumnType("time");

                    b.Property<string>("EventColor")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<DateTime>("EventDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("EventName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<TimeSpan>("StartTime")
                        .HasColumnType("time");

                    b.HasKey("EventID");

                    b.ToTable("Events", "Beta");
                });

            modelBuilder.Entity("TeeTime.Models.Member", b =>
                {
                    b.Property<int>("MemberID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("MemberID"));

                    b.Property<bool>("GoodStanding")
                        .HasColumnType("bit");

                    b.Property<DateTime>("JoinDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("MemberPhone")
                        .HasMaxLength(12)
                        .HasColumnType("nvarchar(12)");

                    b.Property<int>("MembershipCategoryID")
                        .HasColumnType("int");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(10)
                        .HasColumnType("nvarchar(10)");

                    b.Property<int>("UserID")
                        .HasColumnType("int");

                    b.HasKey("MemberID");

                    b.HasIndex("MembershipCategoryID");

                    b.HasIndex("UserID")
                        .IsUnique();

                    b.ToTable("Members", "Beta");
                });

            modelBuilder.Entity("TeeTime.Models.MemberUpgrade", b =>
                {
                    b.Property<int>("ApplicationID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ApplicationID"));

                    b.Property<string>("Address")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("AlternatePhone")
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<DateTime>("ApplicationDate")
                        .HasColumnType("datetime2");

                    b.Property<int?>("ApprovalByUserID")
                        .HasColumnType("int");

                    b.Property<DateTime?>("ApprovalDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("CompanyAddress")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("CompanyName")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("CompanyPhone")
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<string>("CompanyPostalCode")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<DateTime>("DateOfBirth")
                        .HasColumnType("datetime2");

                    b.Property<int>("DesiredMembershipCategoryID")
                        .HasColumnType("int");

                    b.Property<string>("Occupation")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Phone")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<string>("PostalCode")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<int?>("Sponsor1MemberID")
                        .HasColumnType("int");

                    b.Property<int?>("Sponsor2MemberID")
                        .HasColumnType("int");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(10)
                        .HasColumnType("nvarchar(10)");

                    b.Property<int>("UserID")
                        .HasColumnType("int");

                    b.HasKey("ApplicationID");

                    b.HasIndex("ApprovalByUserID");

                    b.HasIndex("DesiredMembershipCategoryID");

                    b.HasIndex("Sponsor1MemberID");

                    b.HasIndex("Sponsor2MemberID");

                    b.HasIndex("UserID");

                    b.ToTable("MemberUpgrades", "Beta");
                });

            modelBuilder.Entity("TeeTime.Models.MembershipCategory", b =>
                {
                    b.Property<int>("MembershipCategoryID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("MembershipCategoryID"));

                    b.Property<bool>("CanMakeStandingTeeTime")
                        .HasColumnType("bit");

                    b.Property<bool>("CanSponsor")
                        .HasColumnType("bit");

                    b.Property<string>("MembershipName")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.HasKey("MembershipCategoryID");

                    b.ToTable("MembershipCategories", "Beta");
                });

            modelBuilder.Entity("TeeTime.Models.Reservation", b =>
                {
                    b.Property<int>("ReservationID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ReservationID"));

                    b.Property<int>("MemberID")
                        .HasColumnType("int");

                    b.Property<int>("NumberOfCarts")
                        .HasColumnType("int");

                    b.Property<int>("NumberOfPlayers")
                        .HasColumnType("int");

                    b.Property<DateTime>("ReservationMadeDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("ReservationStatus")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<int>("TeeTimeId")
                        .HasColumnType("int");

                    b.HasKey("ReservationID");

                    b.HasIndex("MemberID");

                    b.HasIndex("TeeTimeId");

                    b.ToTable("Reservations", "Beta");
                });

            modelBuilder.Entity("TeeTime.Models.Role", b =>
                {
                    b.Property<int>("RoleID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("RoleID"));

                    b.Property<string>("RoleDescription")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.HasKey("RoleID");

                    b.ToTable("Roles", "Beta");
                });

            modelBuilder.Entity("TeeTime.Models.StandingTeeTimeRequest", b =>
                {
                    b.Property<int>("RequestID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("RequestID"));

                    b.Property<int?>("ApprovedByUserID")
                        .HasColumnType("int");

                    b.Property<DateTime?>("ApprovedDate")
                        .HasColumnType("datetime2");

                    b.Property<TimeSpan?>("ApprovedTeeTime")
                        .HasColumnType("time");

                    b.Property<string>("DayOfWeek")
                        .IsRequired()
                        .HasMaxLength(10)
                        .HasColumnType("nvarchar(10)");

                    b.Property<TimeSpan>("DesiredTeeTime")
                        .HasColumnType("time");

                    b.Property<DateTime>("EndDate")
                        .HasColumnType("datetime2");

                    b.Property<int>("MemberID")
                        .HasColumnType("int");

                    b.Property<int?>("PriorityNumber")
                        .HasColumnType("int");

                    b.Property<DateTime>("StartDate")
                        .HasColumnType("datetime2");

                    b.HasKey("RequestID");

                    b.HasIndex("ApprovedByUserID");

                    b.HasIndex("MemberID");

                    b.ToTable("StandingTeeTimeRequests", "Beta");
                });

            modelBuilder.Entity("TeeTime.Models.TeeSheet.TeeSheet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("Date")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("EndTime")
                        .HasColumnType("datetime2");

                    b.Property<int>("IntervalMinutes")
                        .HasColumnType("int");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<DateTime>("StartTime")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.ToTable("TeeSheets", "Beta");
                });

            modelBuilder.Entity("TeeTime.Models.TeeSheet.TeeTime", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int?>("EventID")
                        .HasColumnType("int");

                    b.Property<bool>("IsAvailable")
                        .HasColumnType("bit");

                    b.Property<string>("Notes")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("StartTime")
                        .HasColumnType("datetime2");

                    b.Property<int>("TeeSheetId")
                        .HasColumnType("int");

                    b.Property<int>("TotalPlayersBooked")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("EventID");

                    b.HasIndex("TeeSheetId");

                    b.ToTable("TeeTimes", "Beta");
                });

            modelBuilder.Entity("TeeTime.Models.User", b =>
                {
                    b.Property<int>("UserID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("UserID"));

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<int>("MembershipCategoryID")
                        .HasColumnType("int");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<int>("RoleID")
                        .HasColumnType("int");

                    b.HasKey("UserID");

                    b.HasIndex("MembershipCategoryID");

                    b.HasIndex("RoleID");

                    b.ToTable("Users", "Beta");
                });

            modelBuilder.Entity("TeeTime.Models.Member", b =>
                {
                    b.HasOne("TeeTime.Models.MembershipCategory", "MembershipCategory")
                        .WithMany("Members")
                        .HasForeignKey("MembershipCategoryID")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("TeeTime.Models.User", "User")
                        .WithOne("Member")
                        .HasForeignKey("TeeTime.Models.Member", "UserID")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("MembershipCategory");

                    b.Navigation("User");
                });

            modelBuilder.Entity("TeeTime.Models.MemberUpgrade", b =>
                {
                    b.HasOne("TeeTime.Models.User", "ApprovalBy")
                        .WithMany("ApprovedUpgrades")
                        .HasForeignKey("ApprovalByUserID")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("TeeTime.Models.MembershipCategory", "DesiredMembershipCategory")
                        .WithMany("MemberUpgrades")
                        .HasForeignKey("DesiredMembershipCategoryID")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("TeeTime.Models.Member", "Sponsor1")
                        .WithMany("SponsoredUpgrades1")
                        .HasForeignKey("Sponsor1MemberID")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("TeeTime.Models.Member", "Sponsor2")
                        .WithMany("SponsoredUpgrades2")
                        .HasForeignKey("Sponsor2MemberID")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("TeeTime.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("UserID")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("ApprovalBy");

                    b.Navigation("DesiredMembershipCategory");

                    b.Navigation("Sponsor1");

                    b.Navigation("Sponsor2");

                    b.Navigation("User");
                });

            modelBuilder.Entity("TeeTime.Models.Reservation", b =>
                {
                    b.HasOne("TeeTime.Models.Member", "Member")
                        .WithMany("Reservations")
                        .HasForeignKey("MemberID")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("TeeTime.Models.TeeSheet.TeeTime", "TeeTime")
                        .WithMany("Reservations")
                        .HasForeignKey("TeeTimeId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Member");

                    b.Navigation("TeeTime");
                });

            modelBuilder.Entity("TeeTime.Models.StandingTeeTimeRequest", b =>
                {
                    b.HasOne("TeeTime.Models.User", "ApprovedBy")
                        .WithMany()
                        .HasForeignKey("ApprovedByUserID")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("TeeTime.Models.Member", "Member")
                        .WithMany("StandingTeeTimeRequests")
                        .HasForeignKey("MemberID")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("ApprovedBy");

                    b.Navigation("Member");
                });

            modelBuilder.Entity("TeeTime.Models.TeeSheet.TeeTime", b =>
                {
                    b.HasOne("TeeTime.Models.Event", "Event")
                        .WithMany("TeeTimes")
                        .HasForeignKey("EventID");

                    b.HasOne("TeeTime.Models.TeeSheet.TeeSheet", "TeeSheet")
                        .WithMany("TeeTimes")
                        .HasForeignKey("TeeSheetId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Event");

                    b.Navigation("TeeSheet");
                });

            modelBuilder.Entity("TeeTime.Models.User", b =>
                {
                    b.HasOne("TeeTime.Models.MembershipCategory", "MembershipCategory")
                        .WithMany("Users")
                        .HasForeignKey("MembershipCategoryID")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("TeeTime.Models.Role", "Role")
                        .WithMany("Users")
                        .HasForeignKey("RoleID")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("MembershipCategory");

                    b.Navigation("Role");
                });

            modelBuilder.Entity("TeeTime.Models.Event", b =>
                {
                    b.Navigation("TeeTimes");
                });

            modelBuilder.Entity("TeeTime.Models.Member", b =>
                {
                    b.Navigation("Reservations");

                    b.Navigation("SponsoredUpgrades1");

                    b.Navigation("SponsoredUpgrades2");

                    b.Navigation("StandingTeeTimeRequests");
                });

            modelBuilder.Entity("TeeTime.Models.MembershipCategory", b =>
                {
                    b.Navigation("MemberUpgrades");

                    b.Navigation("Members");

                    b.Navigation("Users");
                });

            modelBuilder.Entity("TeeTime.Models.Role", b =>
                {
                    b.Navigation("Users");
                });

            modelBuilder.Entity("TeeTime.Models.TeeSheet.TeeSheet", b =>
                {
                    b.Navigation("TeeTimes");
                });

            modelBuilder.Entity("TeeTime.Models.TeeSheet.TeeTime", b =>
                {
                    b.Navigation("Reservations");
                });

            modelBuilder.Entity("TeeTime.Models.User", b =>
                {
                    b.Navigation("ApprovedUpgrades");

                    b.Navigation("Member");
                });
#pragma warning restore 612, 618
        }
    }
}
