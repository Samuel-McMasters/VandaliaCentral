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

var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSingleton<PdfService>();

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
    .AddMicrosoftIdentityUI(); // <- this is the correct usage // Needed for MicrosoftIdentity UI pages


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

app.MapRazorPages(); // <-- ADD THIS
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();