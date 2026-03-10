using PROJECT_V1.Models;
using PROJECT_V1.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.Configure<ProposalGeneratorOptions>(builder.Configuration.GetSection(ProposalGeneratorOptions.SectionName));
builder.Services.Configure<LlmOptions>(builder.Configuration.GetSection(LlmOptions.SectionName));
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(180);
    options.IncludeSubDomains = true;
    options.Preload = true;
});
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
});

builder.Services.AddHttpClient("OpenAI", (serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<LlmOptions>>().Value.OpenAI;
    var baseUrl = options.ApiBase.TrimEnd('/') + "/";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds));
});

builder.Services.AddHttpClient("Gemini", (serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<LlmOptions>>().Value.Gemini;
    var baseUrl = options.ApiBase.TrimEnd('/') + "/";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds));
});

builder.Services.AddScoped<OpenAiProposalService>();
builder.Services.AddScoped<GeminiProposalService>();
builder.Services.AddScoped<IProposalService, LlmProposalService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "img-src 'self' data:; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
        "font-src 'self' https://fonts.gstatic.com; " +
        "script-src 'self' 'unsafe-inline'; " +
        "base-uri 'self'; " +
        "object-src 'none'; " +
        "frame-ancestors 'none'";
    await next();
});

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
