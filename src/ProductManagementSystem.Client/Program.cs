using ProductManagementSystem.Client.Components;
using ProductManagementSystem.Client.Handlers;
using ProductManagementSystem.Client.Utilities;
using ProductManagementSystem.Client.Utilities.Contracts;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

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

builder.Services.AddScoped<ILocalStorageUtility, LocalStorageUtility>();

builder.Services.AddHttpClient(builder.Configuration.GetValue<string>("ApiClient:Key") ?? throw new ArgumentNullException("The api key name is not provided yet"), opts =>
{
    opts.BaseAddress = new Uri(builder.Configuration.GetValue<string>("ApiClient:BaseUri") ?? throw new ArgumentNullException("The api base uri is not provided yet"));
    opts.Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue<double>("ApiClient:TimeoutAfterSeconds"));
    opts.DefaultRequestVersion = HttpVersion.Version11;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
