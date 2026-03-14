using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProductManagementSystem.Api.Data;
using ProductManagementSystem.Api.Endpoints;
using ProductManagementSystem.Api.Extensions;
using ProductManagementSystem.Api.Services;
using ProductManagementSystem.Api.Services.Contracts;
using ProductManagementSystem.Api.Utilities;
using ProductManagementSystem.Api.Utilities.Contracts;
using Serilog;
using Microsoft.OpenApi.Models;
using System.Text;
using ProductManagementSystem.Api.Entities.ConfigurationModels;
using ProductManagementSystem.Api;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "log-.txt");

Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .Enrich.FromLogContext()
                    .WriteTo.File(logPath, Serilog.Events.LogEventLevel.Information, outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.ffff zzz}||{Level:u3}] || [{ClassName}].[{MethodName}] - {Message:lj}{NewLine}{Exception}{NewLine}", 
                                    fileSizeLimitBytes: 10_000_000, rollingInterval: RollingInterval.Day, buffered: false)
                    .CreateLogger();

builder.Services.AddSwaggerGen(opts =>
{
    opts.SwaggerDoc("v1", new OpenApiInfo() { Version = "V1", Description = "ProductManagementSystemAPI", Title = "Product Management System API" });

    var securityScheme = new OpenApiSecurityScheme()
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Description = "Enter your bearer token here",
        Scheme = "Bearer",
        Type = SecuritySchemeType.ApiKey
    };

    opts.AddSecurityDefinition("Bearer", securityScheme);

    var securityRequirement = new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference()
                {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                },
                Name = "Bearer"
            },
            new List<string>()
        }

    };

    opts.AddSecurityRequirement(securityRequirement);

});

builder.Services.AddDbContext<RepositoryContext>(opts =>
{
    opts.UseSqlServer(builder.Configuration.GetConnectionString("SqlDbConnection"))
            .EnableSensitiveDataLogging()
            .LogTo(Log.Information, new[] { DbLoggerCategory.Database.Command.Name, DbLoggerCategory.Model.Name }, LogLevel.Information, Microsoft.EntityFrameworkCore.Diagnostics.DbContextLoggerOptions.SingleLine);
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IProductCategoryService, ProductCategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IOrderLineItemService, OrderLineItemService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();

builder.Services.Configure<JwtSettingConfig>(builder.Configuration.GetSection("JwtSettings"));

builder.Services.AddAuthentication(opts =>
{
    opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opts.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

}).AddJwtBearer(opts =>
{
    var jwtSettingsConfiguration = builder.Configuration.GetSection("JwtSettings");
    string secretKey = Environment.GetEnvironmentVariable("PmsSECRET") ?? throw new ArgumentNullException("Cannot proceed as secret key could not be fetched.");

    TokenValidationParameters tokenValidationParamter = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,

        ValidAudiences = jwtSettingsConfiguration["ValidAudience"]?.Split(";", StringSplitOptions.RemoveEmptyEntries) ?? [],
        ValidIssuer = jwtSettingsConfiguration["ValidIssuer"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };

    opts.TokenValidationParameters = tokenValidationParamter;   

});

builder.Services.AddAuthorization();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseSwagger();
app.UseSwaggerUI(opts =>
{
    opts.RoutePrefix = string.Empty;
    opts.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Management System");
});

app.UseExceptionHandler(opts =>
{

});

app.UseAuthentication();

app.UseAuthorization();

app.UseHttpsRedirection();

await app.MigrateDatabase();

app.MapWeatherEndpoints();

app.MapControllers();

//await app.MigrateDatabase();

app.Run();


