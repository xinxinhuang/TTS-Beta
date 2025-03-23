using Microsoft.EntityFrameworkCore;
using TeeTime.Models;

namespace TeeTime.Data
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new TeeTimeDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<TeeTimeDbContext>>()))
            {
                // Add Roles if they don't exist
                if (!context.Role.Any())
                {
                    context.Role.AddRange(
                        new Role { RoleDescription = "Member" },
                        new Role { RoleDescription = "Clerk" },
                        new Role { RoleDescription = "Pro Shop Staff" },
                        new Role { RoleDescription = "Committee Member" }
                    );
                    context.SaveChanges();
                }

                // Add Membership Categories if they don't exist
                if (!context.MembershipCategory.Any())
                {
                    context.MembershipCategory.AddRange(
                        new MembershipCategory
                        {
                            MembershipName = "Gold",
                            CanSponsor = false,
                            CanMakeStandingTeeTime = false
                        },
                        new MembershipCategory
                        {
                            MembershipName = "Gold Shareholder",
                            CanSponsor = true,
                            CanMakeStandingTeeTime = true
                        },
                        new MembershipCategory
                        {
                            MembershipName = "Gold Associate",
                            CanSponsor = true,
                            CanMakeStandingTeeTime = true
                        },
                        new MembershipCategory
                        {
                            MembershipName = "Silver",
                            CanSponsor = false,
                            CanMakeStandingTeeTime = false
                        },
                        new MembershipCategory
                        {
                            MembershipName = "Bronze",
                            CanSponsor = false,
                            CanMakeStandingTeeTime = false
                        },
                        new MembershipCategory
                        {
                            MembershipName = "Copper",
                            CanSponsor = false,
                            CanMakeStandingTeeTime = false
                        }
                    );
                    context.SaveChanges();
                }
            }
        }
    }
}
