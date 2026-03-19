using ProductManagementSystem.Api.Data;
using ProductManagementSystem.Api.Helpers;
using ProductManagementSystem.Api.Utilities.Contracts;
using Serilog;

namespace ProductManagementSystem.Api.Extensions;

public static class MigrateAndSeedDefaultExtension
{
    private static string _methodName = "MethodName";
    private static string _className = "ClassName";
    public static async Task MigrateAndSeedAsync(this WebApplication app, IConfiguration configuration)
    {
        using var scope = app.Services.CreateScope();

        var repositoryContext = scope.ServiceProvider.GetRequiredService<RepositoryContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

		try
		{
			await RepositoryContextSeedDatabase.SeedDatabaseAsync(repositoryContext, configuration, passwordHasher);
		}
		catch (Exception ex)
		{
			Log.ForContext(_className, nameof(MigrateAndSeedDefaultExtension)).ForContext(_methodName, nameof(MigrateAndSeedAsync)).Error(ex, "An Error occurred seeding database default values....");
		}
    }
}
