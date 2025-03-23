using Microsoft.EntityFrameworkCore;
using TeeTime.Models;
using System;
using System.Text;
using System.Security.Cryptography;
using System.Linq;

namespace TeeTime.Data
{
    public static class SeedData
    {
        // Helper function to hash passwords
        private static string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // Convert the input string to a byte array and compute the hash
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

                // Convert byte array to a string
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new TeeTimeDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<TeeTimeDbContext>>()))
            {
                // Add Roles if they don't exist
                if (!context.Roles.Any())
                {
                    context.Roles.AddRange(
                        new Role { RoleDescription = "Member" },
                        new Role { RoleDescription = "Clerk" },
                        new Role { RoleDescription = "Pro Shop Staff" },
                        new Role { RoleDescription = "Committee Member" }
                    );
                    context.SaveChanges();
                }

                // Add Membership Categories if they don't exist
                if (!context.MembershipCategories.Any())
                {
                    context.MembershipCategories.AddRange(
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

                // Add sponsor users if they don't exist
                if (!context.Users.Any(u => u.Email.Contains("sponsor")))
                {
                    // Get membership category IDs
                    var goldShareholderCategory = context.MembershipCategories
                        .FirstOrDefault(m => m.MembershipName == "Gold Shareholder");

                    var goldAssociateCategory = context.MembershipCategories
                        .FirstOrDefault(m => m.MembershipName == "Gold Associate");

                    var memberRole = context.Roles
                        .FirstOrDefault(r => r.RoleDescription == "Member");

                    // Check if the categories and role exist
                    if (goldShareholderCategory == null || goldAssociateCategory == null || memberRole == null)
                    {
                        // Log error or throw exception
                        throw new InvalidOperationException("Required membership categories or roles not found in database");
                    }

                    var goldShareholderCategoryId = goldShareholderCategory.MembershipCategoryID;
                    var goldAssociateCategoryId = goldAssociateCategory.MembershipCategoryID;
                    var memberRoleId = memberRole.RoleID;

                    try
                    {
                        // Create Gold Shareholder sponsors
                        var sponsor1 = new User
                        {
                            FirstName = "John",
                            LastName = "Smith",
                            Email = "sponsor1@teetimeclub.com",
                            PasswordHash = HashPassword("Password123!"),
                            RoleID = memberRoleId,
                            MembershipCategoryID = goldShareholderCategoryId
                        };
                        context.Users.Add(sponsor1);

                        var sponsor2 = new User
                        {
                            FirstName = "Emily",
                            LastName = "Johnson",
                            Email = "sponsor2@teetimeclub.com",
                            PasswordHash = HashPassword("Password123!"),
                            RoleID = memberRoleId,
                            MembershipCategoryID = goldShareholderCategoryId
                        };
                        context.Users.Add(sponsor2);

                        // Create Gold Associate sponsors
                        var sponsor3 = new User
                        {
                            FirstName = "Michael",
                            LastName = "Brown",
                            Email = "sponsor3@teetimeclub.com",
                            PasswordHash = HashPassword("Password123!"),
                            RoleID = memberRoleId,
                            MembershipCategoryID = goldAssociateCategoryId
                        };
                        context.Users.Add(sponsor3);

                        var sponsor4 = new User
                        {
                            FirstName = "Sarah",
                            LastName = "Davis",
                            Email = "sponsor4@teetimeclub.com",
                            PasswordHash = HashPassword("Password123!"),
                            RoleID = memberRoleId,
                            MembershipCategoryID = goldAssociateCategoryId
                        };
                        context.Users.Add(sponsor4);
                        
                        context.SaveChanges();

                        // Create Member records for the sponsors
                        context.Members.AddRange(
                            new Member 
                            { 
                                UserID = sponsor1.UserID, 
                                MembershipCategoryID = goldShareholderCategoryId,
                                JoinDate = DateTime.Now.AddYears(-3),
                                Status = "Active",
                                MemberPhone = "555-123-4567",
                                GoodStanding = true
                            },
                            new Member 
                            { 
                                UserID = sponsor2.UserID, 
                                MembershipCategoryID = goldShareholderCategoryId,
                                JoinDate = DateTime.Now.AddYears(-2),
                                Status = "Active",
                                MemberPhone = "555-234-5678",
                                GoodStanding = true
                            },
                            new Member 
                            { 
                                UserID = sponsor3.UserID, 
                                MembershipCategoryID = goldAssociateCategoryId,
                                JoinDate = DateTime.Now.AddYears(-4),
                                Status = "Active",
                                MemberPhone = "555-345-6789",
                                GoodStanding = true
                            },
                            new Member 
                            { 
                                UserID = sponsor4.UserID, 
                                MembershipCategoryID = goldAssociateCategoryId,
                                JoinDate = DateTime.Now.AddYears(-1),
                                Status = "Active",
                                MemberPhone = "555-456-7890",
                                GoodStanding = true
                            }
                        );
                        
                        context.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        // This provides more detailed error information
                        throw new InvalidOperationException($"Error seeding user data: {ex.Message}", ex);
                    }
                }

                // Add a Golf Committee member if one doesn't exist
                if (!context.Users.Any(u => u.Email.Contains("committee")))
                {
                    // Get the committee member role
                    var committeeRole = context.Roles
                        .FirstOrDefault(r => r.RoleDescription == "Committee Member");

                    if (committeeRole == null)
                    {
                        // Log error or throw exception
                        throw new InvalidOperationException("Committee Member role not found in database");
                    }

                    var committeeRoleId = committeeRole.RoleID;

                    try
                    {
                        // Create Golf Committee member
                        var committeeUser = new User
                        {
                            FirstName = "Robert",
                            LastName = "Johnson",
                            Email = "committee@teetimeclub.com",
                            PasswordHash = HashPassword("Password123!"),
                            RoleID = committeeRoleId,
                            // Committee members can have Gold membership
                            MembershipCategoryID = context.MembershipCategories
                                .FirstOrDefault(m => m.MembershipName == "Gold")?.MembershipCategoryID ?? throw new InvalidOperationException("Gold membership category not found")
                        };
                        context.Users.Add(committeeUser);
                        context.SaveChanges();

                        // Create Member record for the committee member
                        context.Members.Add(
                            new Member
                            {
                                UserID = committeeUser.UserID,
                                MembershipCategoryID = committeeUser.MembershipCategoryID,
                                JoinDate = DateTime.Now.AddYears(-5),
                                Status = "Active",
                                MemberPhone = "555-987-6543",
                                GoodStanding = true
                            }
                        );
                        context.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        // This provides more detailed error information
                        throw new InvalidOperationException($"Error seeding committee member data: {ex.Message}", ex);
                    }
                }
            }
        }
    }
}
