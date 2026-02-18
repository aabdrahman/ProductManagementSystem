using Microsoft.EntityFrameworkCore;
using ProductManagementSystem.Api.Data;

namespace ProductManagementSystem.Api.Extensions;

public static class DatabaseMigrationExtension
{
    public static async Task MigrateDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RepositoryContext>();
        
        await dbContext.Database.MigrateAsync();
    }
}
