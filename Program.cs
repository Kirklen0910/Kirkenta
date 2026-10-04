using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Helpers.RRHH;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 🔌 Base de datos MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// 📄 Razor Pages + Reglas de autorización
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Auth/Login");
    options.Conventions.AllowAnonymousToPage("/Auth/Register");
    options.Conventions.AllowAnonymousToPage("/Auth/Logout");
});

// 🔐 Autenticación por cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.Name = "KirkentaAuth";
    });

builder.Services.AddAuthorization();

// 📦 Sesión (necesaria para el wizard de importación)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "KirkentaSession";
});

// 🌐 Acceso a HttpContext
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// 🔄 SEEDERS AL ARRANCAR
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Migrar permisos faltantes
        PermisoSeeder.MigrarPermisosFaltantes(db);

        // Seed de Finanzas (cajas y categorías)
        FinanzasSeeder.Seed(db);

        // Seed de RRHH — Tipos de documentos
        TipoDocumentoEmpleadoSeeder.Seed(db);

        // Seed de Feriados (año actual)
        FeriadoSeeder.Seed(db, DateTime.Today.Year);

        // Acumulación de vacaciones anuales
        var procesados = EmpleadoHelper.ProcesarAcumulacionGlobal(db);
        if (procesados > 0)
        {
            Console.WriteLine($"[EmpleadoHelper] Se procesaron {procesados} empleados para acumulación de vacaciones");
        }

        // Actualizar vigencias de documentos
        AdjuntoEmpleadoHelper.ActualizarVigencias(db);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Startup] Error en seeders: {ex.Message}");
    }
}

// 🌐 Pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();