using HelpDesk.Infrastructure.Persistence;
using HelpDesk.SDK;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.ResponseCacheAttribute
    {
        NoStore = true,
        Location = Microsoft.AspNetCore.Mvc.ResponseCacheLocation.None
    });
});

builder.Services.AddDbContext<HelpDeskDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("helpdesk")));

builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddHttpClient("api", client =>
{
    client.BaseAddress = new Uri("https+http://api");
}).AddServiceDiscovery();

// SDK (Refit): registra los clientes tipados (ICategoriesApi, ITicketsApi, ...).
// Aspire resuelve "api" y aplica service discovery vía ConfigureHttpClientDefaults.
builder.Services.AddHelpDeskSdk("https+http://api");

// Capa de servicio del Web (patrón de PR 2). Registrar uno por módulo migrado.
builder.Services.AddScoped<HelpDesk.Web.Services.CategoriesService>();

var app = builder.Build();

app.MapDefaultEndpoints();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
    context.Response.Headers["Pragma"] = "no-cache";
    context.Response.Headers["Expires"] = "0";
    await next();
});

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();