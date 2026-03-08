

using Microsoft.EntityFrameworkCore;
using ProductManagementSystem.Api.Data;
using ProductManagementSystem.Api.Endpoints;
using ProductManagementSystem.Api.Extensions;
using ProductManagementSystem.Api.Services;
using ProductManagementSystem.Api.Services.Contracts;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "log-.txt");

Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .Enrich.FromLogContext()
                    .WriteTo.File(logPath, Serilog.Events.LogEventLevel.Information, outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.ffff zzz}||{Level:u3}] || [{ClassName}].[{MethodName}] - {Message:lj}{NewLine}{Exception}{NewLine}", 
                                    fileSizeLimitBytes: 10_000_000, rollingInterval: RollingInterval.Day, buffered: false)
                    .CreateLogger();

builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<RepositoryContext>(opts =>
{
    opts.UseSqlServer(builder.Configuration.GetConnectionString("SqlDbConnection"))
            .EnableSensitiveDataLogging()
            .LogTo(Log.Information, new[] { DbLoggerCategory.Database.Command.Name, DbLoggerCategory.Model.Name }, LogLevel.Information, Microsoft.EntityFrameworkCore.Diagnostics.DbContextLoggerOptions.SingleLine);
});

builder.Services.AddScoped<IProductCategoryService, ProductCategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IOrderLineItemService, OrderLineItemService>();
builder.Services.AddScoped<IRoleService, RoleService>();

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseSwagger();
app.UseSwaggerUI(opts =>
{
    opts.RoutePrefix = string.Empty;
    opts.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Management System");
});

app.UseHttpsRedirection();

await app.MigrateDatabase();

app.MapWeatherEndpoints();

app.MapControllers();

//await app.MigrateDatabase();

app.Run();


