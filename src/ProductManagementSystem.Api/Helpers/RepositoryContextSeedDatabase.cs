using Microsoft.EntityFrameworkCore;
using ProductManagementSystem.Api.Data;
using ProductManagementSystem.Api.Entities.Models;
using ProductManagementSystem.Api.Utilities.Contracts;
using Serilog;

namespace ProductManagementSystem.Api.Helpers;

public static class RepositoryContextSeedDatabase
{
    private static string _methodName = "MethodName";
    private static string _className = "ClassName";


    public static async Task SeedDatabaseAsync(RepositoryContext repositoryContext, IConfiguration configuration, IPasswordHasher passwordHasher)
    {
		try
		{
            Log.ForContext(_className, nameof(RepositoryContextSeedDatabase)).ForContext(_methodName, nameof(SeedDatabaseAsync)).Information("Begin Default Database Seeding.......");

            string systemDefaultEmail = configuration.GetSection("DefaultUserDetails")["Email"] ?? throw new ArgumentNullException("Error Occurred. System Default email is not yet defined.");
            string systemDefaultUserName = configuration.GetSection("DefaultUserDetails")["Name"] ?? "System";

            if (string.IsNullOrEmpty(systemDefaultEmail) || !ValidEmailHelper.IsValidEmailUsingRegex(systemDefaultEmail))
            {
                Log.ForContext(_className, nameof(RepositoryContextSeedDatabase)).ForContext(_methodName, nameof(SeedDatabaseAsync)).Warning("System Default Email from settings is empty or provided email is invalid format...");
                throw new ArgumentException("System Default Email from settings is empty or has an invalid format.", nameof(systemDefaultEmail));
            }

            string systemUserDefaultPassword = Environment.GetEnvironmentVariable("SystemDefaultPassword") ?? throw new ArgumentNullException("User system default password is not yet defined.");

            //Ensure Database is already created
            await repositoryContext.Database.EnsureCreatedAsync();

            //Check if any roles exists. This ensure idempotency.
            Role? existingSystemRole = await repositoryContext.Roles.FirstOrDefaultAsync(x => x.NormalizedName == "SYSTEM");
            Role? existingAdminRole = await repositoryContext.Roles.FirstOrDefaultAsync(x => x.NormalizedName == "ADMIN");
            Role? existingCustomerRole = await repositoryContext.Roles.FirstOrDefaultAsync(x => x.NormalizedName == "USER");


            User? existingSytemUser = await repositoryContext.Users.FirstOrDefaultAsync(x => x.UserEmailAddress == systemDefaultEmail.ToUpper());

            if (existingSytemUser is null)
            {
                Log.ForContext(_className, nameof(RepositoryContextSeedDatabase)).ForContext(_methodName, nameof(SeedDatabaseAsync)).Information("No System User curently exists. Begin creating system user.");

                existingSytemUser = new User()
                {
                    UserEmailAddress = systemDefaultEmail.ToUpper(),
                    PasswordHash = passwordHasher.HashPassword(systemUserDefaultPassword),
                    Address = "",
                    FirstName = systemDefaultUserName,
                    LastName = systemDefaultUserName,
                    IsActive = true,
                    IsUserConfirmed = true,
                    ConfirmedAt = DateTime.UtcNow,
                    PhoneNumber = ""
                };

                if (existingSystemRole is null)
                {
                    Log.ForContext(_className, nameof(RepositoryContextSeedDatabase)).ForContext(_methodName, nameof(SeedDatabaseAsync)).Information("Seeding a default system role.....");
                    existingSystemRole = new Role()
                    {
                        //CreatedByUser = existingSytemUser, --DISABLED TO PREVENT CIRCULAR DEPENDENCY AFTER SETTING TO OPTIONAL
                        Name = "System",
                        NormalizedName = "SYSTEM",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                }

                existingSytemUser.AssignedRole = existingSystemRole;

                await repositoryContext.AddAsync(existingSytemUser);
                await repositoryContext.AddAsync(existingSystemRole);

                if(existingAdminRole is null)
                {
                    Log.ForContext(_className, nameof(RepositoryContextSeedDatabase)).ForContext(_methodName, nameof(SeedDatabaseAsync)).Information("Seeding a default admin role.....");
                    existingAdminRole = new Role()
                    {
                        Name = "Admin",
                        NormalizedName = "ADMIN",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UserId = existingSytemUser.Id
                    };

                    await repositoryContext.AddAsync(existingAdminRole);
                }

                if(existingCustomerRole is null)
                {
                    Log.ForContext(_className, nameof(RepositoryContextSeedDatabase)).ForContext(_methodName, nameof(SeedDatabaseAsync)).Information("Seeding a default customer role.....");
                    existingCustomerRole = new Role()
                    {
                        Name = "User",
                        NormalizedName = "USER",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UserId = existingSytemUser.Id
                    };
                    await repositoryContext.AddAsync(existingCustomerRole);
                }

                try
                {
                    await repositoryContext.SaveChangesAsync();
                    Log.ForContext(_className, nameof(RepositoryContextSeedDatabase)).ForContext(_methodName, nameof(SeedDatabaseAsync)).Information("System User and Roles seeded successfully.....");
                }
                catch (DbUpdateException ex)
                {
                    Log.ForContext(_className, nameof(RepositoryContextSeedDatabase)).ForContext(_methodName, nameof(SeedDatabaseAsync)).Error(ex, "A database Error Occurred seeding system default to database.");
                    return;
                }
            }
            else
            {
                Log.ForContext(_className, nameof(RepositoryContextSeedDatabase)).ForContext(_methodName, nameof(SeedDatabaseAsync)).Information("System user already seeded.");
            }
        }
		catch (Exception ex)
		{
            Log.ForContext(_className, nameof(RepositoryContextSeedDatabase)).ForContext(_methodName, nameof(SeedDatabaseAsync)).Error(ex, "An Error Occurred seeding system default to database.");
        }
    }
}
