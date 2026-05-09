using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ProductManagementSystem.Api;
using ProductManagementSystem.Api.BackgroundWorker;
using ProductManagementSystem.Api.Controllers.AuthRequirements;
using ProductManagementSystem.Api.Controllers.ServiceFilters;
using ProductManagementSystem.Api.CustomHealthChecks;
using ProductManagementSystem.Api.Data;
using ProductManagementSystem.Api.Endpoints;
using ProductManagementSystem.Api.Entities.ChannelBrokers;
using ProductManagementSystem.Api.Entities.ConfigurationModels;
using ProductManagementSystem.Api.Extensions;
using ProductManagementSystem.Api.Services;
using ProductManagementSystem.Api.Services.Contracts;
using ProductManagementSystem.Api.Utilities;
using ProductManagementSystem.Api.Utilities.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using Serilog;
using StackExchange.Redis;
using System.Security.Claims;
using System.Text;
using System.Threading.Channels;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "log-.txt");

Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .Enrich.FromLogContext()
                    .WriteTo.File(logPath, Serilog.Events.LogEventLevel.Information, outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.ffff zzz}||{Level:u3}] || [{ClassName}].[{MethodName}] - {Message:lj}{NewLine}{Exception}{NewLine}", 
                                    fileSizeLimitBytes: 10_000_000, rollingInterval: RollingInterval.Day, buffered: false)
                    .CreateLogger();

builder.Services.AddCors(opts =>
{

    opts.AddDefaultPolicy(setup =>
    {
        setup.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });

    opts.AddPolicy("FrontEndPolicy", setup =>
    {
        var config = builder.Configuration.GetSection("CorsPolicy");
        setup.AllowAnyHeader()
            .WithExposedHeaders(config["ExposedHeaders"].Split(",", StringSplitOptions.TrimEntries))
            .WithMethods(config["AllowedMethods"].Split(",", StringSplitOptions.TrimEntries))
            .WithOrigins(config["AllowedOrigins"].Split(",", StringSplitOptions.TrimEntries));
    });
});

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

builder.Services.AddSingleton(Channel.CreateUnbounded<CacheItemProcessor<CacheItem>>());
builder.Services.AddSingleton(Channel.CreateBounded<SendPriorityMailEvent>(new BoundedChannelOptions(100)
{
    SingleReader = true,
    SingleWriter = false,
    FullMode = BoundedChannelFullMode.Wait
}));

builder.Services.AddSingleton(Channel.CreateBounded<SendConfirmedOrderNotificationEvent>(new BoundedChannelOptions(100)
{
    FullMode = BoundedChannelFullMode.Wait,
    SingleReader = true,
    SingleWriter = false
}));

builder.Services.AddDbContext<RepositoryContext>(opts =>
{
    opts.UseSqlServer(builder.Configuration.GetConnectionString("SqlDbConnection"))
            .EnableSensitiveDataLogging()
            .LogTo(Log.Information, new[] { DbLoggerCategory.Database.Command.Name, DbLoggerCategory.Model.Name }, LogLevel.Information, DbContextLoggerOptions.SingleLine);
});

builder.Services.AddSingleton<IConnectionMultiplexer>(opts =>
{
    var connection = ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("RedisConnection") ?? throw new ArgumentNullException("Redis connection is not set"));

    return connection;
});

builder.Services.AddHealthChecks()
    .AddCheck<FileStoragePathHealthCheck>(name: "File Storage Path Health", failureStatus: HealthStatus.Degraded)
    .AddCheck<MailProcessorHealthCheck>(name: "Mail Processor Health", failureStatus: HealthStatus.Degraded)
    .AddCheck<D_DriveStorageHealthCheck>(name: " Drive Disk space Health", failureStatus: HealthStatus.Degraded)
    .AddCheck<C_DriveStorageHealthCheck>(name: "C Drive Disk space Health", failureStatus: HealthStatus.Unhealthy)
    .AddCheck<RedisHealthCheck>(name: "Custom Redis Health", failureStatus: HealthStatus.Unhealthy)
    .AddSqlServer(connectionString: builder.Configuration.GetConnectionString("SqlDbConnection"),
                    name: "SQL Database Health",
                    failureStatus: HealthStatus.Unhealthy)
    .AddRedis(redisConnectionString: builder.Configuration.GetConnectionString("RedisConnection"),
              name: "Redis Health",
              failureStatus: HealthStatus.Unhealthy)
    .AddSmtpHealthCheck(opts =>
    {
        opts.Port = builder.Configuration.GetValue<int>("EmailSettings:Port");
        opts.Host = builder.Configuration.GetValue<string>("EmailSettings:Host");
        opts.ConnectionType = HealthChecks.Network.Core.SmtpConnectionType.PLAIN;
        opts.AllowInvalidRemoteCertificates = true;
        
    }, name: "SMTP Health Check", failureStatus: HealthStatus.Degraded);
;

builder.Services.AddHealthChecksUI(opts =>
{
    opts.AddHealthCheckEndpoint("System Health Check", "/admin/_healths");
    opts.SetEvaluationTimeInSeconds(20);
}).AddInMemoryStorage();

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
builder.Services.AddScoped<IBackgroundOperationService, BackgroundOperationService>();
builder.Services.AddScoped<IUserOrderVerificationService, UserOrderVerificationService>();
builder.Services.AddScoped<IAboutUsService, AboutUsService>();
builder.Services.AddScoped<IWhyChooseUsService, WhyChooseUsService>();
builder.Services.AddScoped<IProductImageService, ProductImageService>();

builder.Services.AddScoped<IRedisService, RedisService>();
builder.Services.AddScoped<AuthenticationTokenValidationFilter>();

builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<IOtpOperation, OtpOperation>();
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddSingleton<IEmailVerificationLinkFactory, EmailVerificationLinkFactory>();
builder.Services.AddSingleton<IAuthorizationHandler, DeleteOrderDetailsRequirementHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, UpdateOrderDetailsRequirementHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, GetUserDetailsRequirement>();
builder.Services.AddSingleton<IAuthorizationHandler, UpdateUserDetailsRequirement>();
builder.Services.AddSingleton<IAuthorizationHandler, DeleteUserDetailsRequirement>();

builder.Services.Configure<JwtSettingConfig>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<OtpSettingsConfig>(builder.Configuration.GetSection("OtpSettings"));
builder.Services.Configure<EmailSettingsConfig>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<EmailSettingsWorkerConfig>(builder.Configuration.GetSection("BackgroundWorkerSettings:EmailSettings"));
builder.Services.Configure<RemoveExpiredOtpBackgroundConfig>(builder.Configuration.GetSection("BackgroundWorkerSettings:RemoveExpiredOTP"));
builder.Services.Configure<RemoveExpiredVerificationTokenBackgroundConfig>(builder.Configuration.GetSection("BackgroundWorkerSettings:RemoveExpiredVerificationToken"));
builder.Services.Configure<UserOrderVerificationConfig>(builder.Configuration.GetSection("UserTokenVerification"));
builder.Services.Configure<ImageSizingConfig>(builder.Configuration.GetSection("ImageSizing"));

builder.Services.AddFluentEmail(builder.Configuration.GetSection("EmailSettings")["DefaultFrom"]).AddSmtpSender(host: builder.Configuration.GetSection("EmailSettings")["Host"], port: builder.Configuration.GetSection("EmailSettings").GetValue<int>("Port")).AddRazorRenderer();

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

builder.Services.AddControllers(opts =>
{
    //opts.Filters.Add<AuthenticationTokenValidationFilter>();
});

builder.Services.AddRateLimiter(opts =>
{
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    //opts.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>

    //    RateLimitPartition.GetFixedWindowLimiter("globalRateLimiter", partition =>  new FixedWindowRateLimiterOptions
    //    {
    //        PermitLimit = 3,
    //        AutoReplenishment = true,
    //        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
    //        QueueLimit = 0,
    //        Window = TimeSpan.FromSeconds(6)
    //    })

    //);

    opts.AddPolicy("per-user-limit", context =>
    {
        var userId = context.User.FindFirst(x => x.Type == ClaimTypes.NameIdentifier)?.Value ?? "0";

        if(int.TryParse(userId, out var id) && id > 0)
        {
            return RateLimitPartition.GetTokenBucketLimiter(userId, _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 5,
                TokensPerPeriod = 2,
                ReplenishmentPeriod = TimeSpan.FromMinutes(2)
            });
        }

        return RateLimitPartition.GetFixedWindowLimiter(userId, _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromSeconds(5),
            PermitLimit = 100,
            AutoReplenishment = true,
            QueueLimit = 10,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });
    });

    //opts.AddPolicy("OptionalPolicy", context => RateLimitPartition.GetFixedWindowLimiter("globalRateLimiter", partition => new FixedWindowRateLimiterOptions
    //{
    //    PermitLimit = 3,
    //    AutoReplenishment = true,
    //    Window = TimeSpan.FromSeconds(60)
    //}));

    opts.OnRejected = async (context, token) =>
    {
        var response = GenericResponse<object>.Failure(null, $"Too many requests.", System.Net.HttpStatusCode.TooManyRequests);

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var metadata))
        {
            response.ResponseMessage = $"Too many requests. Please retry again after {metadata.TotalSeconds} seconds";
            await context.HttpContext.Response.WriteAsJsonAsync(response, token);
        }   
        else
        {
            await context.HttpContext.Response.WriteAsJsonAsync(response, token);
        }
            
    };
    
});

builder.Services.AddHostedService<EmailProcessingBackgroundService>();
builder.Services.AddHostedService<RemoveExpiredOtpBackgroundService>();
builder.Services.AddHostedService<RemoveExpiredVerificationTokenBackgroundService>();
builder.Services.AddHostedService<CacheBackgroundProcessor>();
builder.Services.AddHostedService<PriorityEmailProcessorBackgroundService>();
builder.Services.AddHostedService<SendOrderConfirmationNotificationBackgroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseRouting();

app.UseRateLimiter();

app.UseCors("FrontEndPolicy");

app.UseSwagger();
app.UseSwaggerUI(opts =>
{
    opts.RoutePrefix = string.Empty;
    opts.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Management System");
});

app.UseStaticFiles(new StaticFileOptions()
{
    FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "StaticFiles")),
    RequestPath = "/StaticFiles"
});

app.UseExceptionHandler(opts =>
{

});

app.UseAuthentication();

app.UseAuthorization();


//app.UseHttpsRedirection();



app.UseHealthChecks("/admin/_healths", new HealthCheckOptions()
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
    Predicate = p => true

});

app.UseHealthChecksUI(opts =>
{
    opts.UIPath = "/_healths-ui";
    opts.ApiPath = "/admin/_healths-api";
});



await app.MigrateDatabase();

await app.MigrateAndSeedAsync(builder.Configuration);

app.MapWeatherEndpoints();

app.MapControllers();

//await app.MigrateDatabase();

app.Run();


