using VandaliaCentral.Components;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Graph;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Identity.Web.UI;
using VandaliaCentral.Services;
using QuestPDF.Infrastructure;
using Blazorise;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;
using Blazorise.Charts;
using System.Net.Http.Headers;

var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

// Make sure environment variables are included (Azure App Service App settings)
// WebApplicationBuilder already includes this by default, but keeping it explicit helps prevent surprises.
builder.Configuration.AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddSingleton<PdfService>();
builder.Services.AddSingleton<LoggingService>();
builder.Services.AddSingleton<CalendarService>();
builder.Services.AddSingleton<IPasswordGeneratorService, PasswordGeneratorService>();

// Bind Freshservice options (maps Freshservice__ApiKey, Freshservice__Domain, etc.)
// Validate at startup so you instantly know if Azure settings are missing/misnamed
builder.Services
    .AddOptions<FreshserviceOptions>()
    .Bind(builder.Configuration.GetSection("Freshservice"))
    .Validate(o => !string.IsNullOrWhiteSpace(o.Domain), "Freshservice:Domain is required (Freshservice__Domain).")
    .Validate(o => !string.IsNullOrWhiteSpace(o.ApiKey), "Freshservice:ApiKey is required (Freshservice__ApiKey).")
    .Validate(o => o.DepartmentId > 0, "Freshservice:DepartmentId is required (Freshservice__DepartmentId).")
    .Validate(o => o.ResponderId > 0, "Freshservice:ResponderId is required (Freshservice__ResponderId).")
    .ValidateOnStart();

// Typed HttpClient for Freshservice
builder.Services
    .AddHttpClient<IFreshserviceService, FreshserviceService>(client =>
    {
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        UseCookies = false
    });

// For PDF API controller
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<GraphUserService>();
builder.Services.AddScoped<GraphEmailService>();

// Microsoft Identity Web + Graph
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

builder.Services.Configure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});

// Enables conditional access and consent handling for Graph calls
builder.Services.AddServerSideBlazor()
    .AddMicrosoftIdentityConsentHandler();

builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

builder.Services.AddScoped<GraphServiceClient>(serviceProvider =>
{
    var tokenAcquisition = serviceProvider.GetRequiredService<ITokenAcquisition>();

    return new GraphServiceClient(new DelegateAuthenticationProvider(async request =>
    {
        var accessToken = await tokenAcquisition.GetAccessTokenForUserAsync(
            new[] { "User.Read.All", "Mail.Send" });

        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    }));
});

var app = builder.Build();

QuestPDF.Settings.License = LicenseType.Community;
app.MapControllers();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// Middleware
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorPages();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
