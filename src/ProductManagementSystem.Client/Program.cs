using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.Tokens;
using ProductManagementSystem.Client.AuthProviderUtility;
using ProductManagementSystem.Client.Components;
using ProductManagementSystem.Client.Handlers;
using ProductManagementSystem.Client.Utilities;
using ProductManagementSystem.Client.Utilities.Contracts;
using System.Net;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddAuthentication(opts =>
{
    opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opts.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

}).AddJwtBearer("Bearer", opts =>
{
    var tokenParameter = new TokenValidationParameters()
    {
        ValidateAudience = true,
        ValidateIssuer = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,

        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("PmsSECRET") ?? "Test")),
        ValidIssuer = "TaskManagementAPI",
        ValidAudiences = "https://localhost:7082;http://localhost:5246".Split(";", StringSplitOptions.TrimEntries)
    };

    opts.TokenValidationParameters = tokenParameter;

    //opts.Events = new JwtBearerEvents
    //{
    //    OnChallenge = context =>
    //    {
    //        context.HandleResponse();
    //        return Task.CompletedTask;
    //    }
    //};
});

builder.Services.AddAuthorizationCore();

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<AuthenticationStateProvider, AuthStateProvider>();

builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, MiddlewareAuthenticationResultHandler>();

builder.Services.AddScoped<GetReviewsHandler>();
builder.Services.AddScoped<GetProductsHandler>();
builder.Services.AddScoped<AddReviewHandler>();
builder.Services.AddScoped<GetProductCategoryHandler>();
builder.Services.AddScoped<AddProductHandler>();
builder.Services.AddScoped<RestockProductHandler>();
builder.Services.AddScoped<GetProductUpdateDetailsHandler>();
builder.Services.AddScoped<UpdateProductHandler>();
builder.Services.AddScoped<DeleteProductHandler>();
builder.Services.AddScoped<AddProductCategoryHandler>();
builder.Services.AddScoped<DeleteProductCategoryHandler>();
builder.Services.AddScoped<GetProductHandler>();
builder.Services.AddScoped<GetMultipleProductsHandler>();
builder.Services.AddScoped<AddOrderHandler>();
builder.Services.AddScoped<GetOrderDetailsHandler>();
builder.Services.AddScoped<GetOrdersHandler>();
builder.Services.AddScoped<UpdateOrderStatusHandler>();
builder.Services.AddScoped<DeleteOrderHandler>();
builder.Services.AddScoped<AddFeedbackHandler>();
builder.Services.AddScoped<GetRoleHandler>();
builder.Services.AddScoped<RegisterUserHandler>();
builder.Services.AddScoped<LoginHandler>();
builder.Services.AddScoped<LogoutHandler>();
builder.Services.AddScoped<GetUserDetailsHandler>();
builder.Services.AddScoped<SendVerificationOtpHandler>();
builder.Services.AddScoped<VerifyOtpHandler>();

builder.Services.AddScoped<ILocalStorageUtility, LocalStorageUtility>();

builder.Services.AddTransient<OriginHandler>();
builder.Services.AddScoped<RequestContext>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient(builder.Configuration.GetValue<string>("ApiClient:Key") ?? throw new ArgumentNullException("The api key name is not provided yet"), opts =>
{
    opts.BaseAddress = new Uri(builder.Configuration.GetValue<string>("ApiClient:BaseUri") ?? throw new ArgumentNullException("The api base uri is not provided yet"));
    opts.Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue<double>("ApiClient:TimeoutAfterSeconds"));
    opts.DefaultRequestVersion = HttpVersion.Version11;

}).AddHttpMessageHandler<OriginHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//app.UseStatusCodePagesWithRedirects("/StatusCode/{0}");

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
