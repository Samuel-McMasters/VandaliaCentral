using VandaliaCentral.Components;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Identity.Web.UI;
using VandaliaCentral.Services;
using QuestPDF.Infrastructure;



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSingleton<PdfService>();

//For pdf api controller
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddScoped<EmailService>();

//===================================================
//Uncomment when I figure out IIS hosting issue
//builder.Services
//    .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
//    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));


//builder.Services.AddRazorPages(); // Needed for MicrosoftIdentity UI pages
//builder.Services.Configure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme, options =>
//{
//    options.LogoutPath = "/MicrosoftIdentity/Account/SignOut";
//});




//builder.Services.AddAuthorization(options =>
//{
//    options.FallbackPolicy = options.DefaultPolicy;
//});

//===================================================

var app = builder.Build();

QuestPDF.Settings.License = LicenseType.Community;
app.MapControllers();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();




//Uncomment when figured out IIS hosting stuff
//app.UseAuthentication();
//app.UseAuthorization();



app.UseRouting(); // optional if you're already routing
//app.MapRazorPages(); // THIS is key for the built-in SignOut page to render and redirect
//================================



app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();


app.Run();
