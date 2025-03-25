using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Migrations;
using TeeTime.Models;
using TeeTime.Models.TeeSheet;

namespace TeeTime.Data
{
    public class TeeTimeDbContext : DbContext
    {
        public TeeTimeDbContext(DbContextOptions<TeeTimeDbContext> options)
            : base(options)
        {
            // Configure migration history table in the constructor if needed
        }

        public DbSet<Role> Roles { get; set; }
        public DbSet<MembershipCategory> MembershipCategories { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<MemberUpgrade> MemberUpgrades { get; set; }
        public DbSet<ScheduledGolfTime> ScheduledGolfTimes { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<StandingTeeTimeRequest> StandingTeeTimeRequests { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Models.TeeSheet.TeeSheet> TeeSheets { get; set; }
        public DbSet<Models.TeeSheet.TeeTime> TeeTimes { get; set; }

        // Configuration is handled in Program.cs

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("Beta");

            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasOne(u => u.MembershipCategory)
                .WithMany(mc => mc.Users)
                .HasForeignKey(u => u.MembershipCategoryID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Member)
                .WithOne(m => m.User)
                .HasForeignKey<Member>(m => m.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Member>()
                .HasOne(m => m.MembershipCategory)
                .WithMany(mc => mc.Members)
                .HasForeignKey(m => m.MembershipCategoryID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MemberUpgrade>()
                .HasOne(mu => mu.User)
                .WithMany()
                .HasForeignKey(mu => mu.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MemberUpgrade>()
                .HasOne(mu => mu.DesiredMembershipCategory)
                .WithMany(mc => mc.MemberUpgrades)
                .HasForeignKey(mu => mu.DesiredMembershipCategoryID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MemberUpgrade>()
                .HasOne(mu => mu.Sponsor1)
                .WithMany(m => m.SponsoredUpgrades1)
                .HasForeignKey(mu => mu.Sponsor1MemberID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MemberUpgrade>()
                .HasOne(mu => mu.Sponsor2)
                .WithMany(m => m.SponsoredUpgrades2)
                .HasForeignKey(mu => mu.Sponsor2MemberID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MemberUpgrade>()
                .HasOne(mu => mu.ApprovalBy)
                .WithMany(u => u.ApprovedUpgrades)
                .HasForeignKey(mu => mu.ApprovalByUserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Member)
                .WithMany(m => m.Reservations)
                .HasForeignKey(r => r.MemberID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.ScheduledGolfTime)
                .WithMany(sgt => sgt.Reservations)
                .HasForeignKey(r => r.ScheduledGolfTimeID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StandingTeeTimeRequest>()
                .HasOne(sttr => sttr.Member)
                .WithMany(m => m.StandingTeeTimeRequests)
                .HasForeignKey(sttr => sttr.MemberID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StandingTeeTimeRequest>()
                .HasOne(sttr => sttr.ApprovedBy)
                .WithMany()
                .HasForeignKey(sttr => sttr.ApprovedByUserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ScheduledGolfTime>()
                .HasOne(s => s.Event)
                .WithMany(e => e.ScheduledGolfTimes)
                .HasForeignKey(s => s.EventID)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.TeeSheet.TeeTime>()
                .HasOne(tt => tt.TeeSheet)
                .WithMany(ts => ts.TeeTimes)
                .HasForeignKey(tt => tt.TeeSheetId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
