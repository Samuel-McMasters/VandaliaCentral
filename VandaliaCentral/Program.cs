using VandaliaCentral.Components;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Graph;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Identity.Web.UI;
using VandaliaCentral.Services;
using QuestPDF.Infrastructure;



var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSingleton<PdfService>();

//For pdf api controller
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<EmailsService>();

//===================================================
//Uncomment when I figure out IIS hosting issue
//builder.Services
//    .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
//    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));




builder.Services.AddRazorPages(); // Needed for MicrosoftIdentity UI pages
//builder.Services.Configure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme, options =>
//{
//    options.LogoutPath = "/MicrosoftIdentity/Account/SignOut";
//});

//===================================================
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();


builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});

builder.Services.AddScoped<GraphServiceClient>(serviceProvider =>
{
    var tokenAcquisition = serviceProvider.GetRequiredService<ITokenAcquisition>();

    return new GraphServiceClient(new DelegateAuthenticationProvider(async request =>
    {
        var accessToken = await tokenAcquisition.GetAccessTokenForUserAsync(new[] { "Mail.Send" });
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    }));
});


//===================================================

var app = builder.Build();

QuestPDF.Settings.License = LicenseType.Community;
app.MapControllers();

//Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();




//Uncomment when figured out IIS hosting stuff
app.UseAuthentication();
app.UseAuthorization();



app.UseRouting(); // optional if you're already routing
//app.MapRazorPages(); // THIS is key for the built-in SignOut page to render and redirect
//================================



app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();


app.Run();
