using AuthClient.Components;
using AuthClient.Services;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);


// Services

// Razor components (Blazor Server)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Access to HttpContext (needed for auth flows, tokens, etc.)
builder.Services.AddHttpContextAccessor();

// Microsoft Identity (Azure AD / Entra ID)
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        builder.Configuration.Bind("AzureAd", options);

        options.SignedOutCallbackPath = "/signout-callback-oidc";

        options.Events = new OpenIdConnectEvents
        {
            OnRedirectToIdentityProviderForSignOut = context =>
            {
                context.ProtocolMessage.PostLogoutRedirectUri =
                    "https://auth.kevinsiraki.com/signout-callback-oidc";

                return Task.CompletedTask;
            }
        };
    })
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

// Authorization
builder.Services.AddAuthorization();

// App services
builder.Services.AddScoped<ApiService>();

// Enables cascading auth state for components
builder.Services.AddCascadingAuthenticationState();

// Controllers + Microsoft Identity UI endpoints (/signin-oidc, etc.)
builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

// Typed HttpClient for API calls
builder.Services.AddHttpClient<ApiService>(client =>
{
    var baseUrl = builder.Configuration["Api:BaseUrl"]
        ?? throw new InvalidOperationException("Api:BaseUrl missing");

    client.BaseAddress = new Uri(baseUrl);
});


// This allows ASP.NET to correctly detect HTTPS when behind a reverse proxy
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;

    // Trust all proxies (safe for internal / home network setups)
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();


// Middleware Pipeline
app.Use(async (context, next) =>
{
    context.Response.Headers.Remove("X-Frame-Options");
    await next();
});
// must come first so scheme (https) is correct
app.UseForwardedHeaders();

// Static files (wwwroot)
app.UseStaticFiles();

app.UseRouting();

// Authentication / Authorization
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Controllers (required for Microsoft Identity endpoints)
app.MapControllers();

app.MapGet("/signout-callback-oidc", context =>
{
    context.Response.Redirect("/");
    return Task.CompletedTask;
});

// Razor components (Blazor Server)
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();