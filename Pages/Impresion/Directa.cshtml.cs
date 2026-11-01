using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Impresion
{
    /// <summary>Puntos de entrada (JSON) para imprimir tickets directo en la impresora térmica y abrir la gaveta.</summary>
    public class DirectaModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ImpresoraTermicaService _impresora;

        public DirectaModel(ApplicationDbContext db, ImpresoraTermicaService impresora)
        {
            _db = db; _impresora = impresora;
        }

        public IActionResult OnGet() => NotFound();

        private bool PuedeFacturas() => PermisoHelper.UsuarioTieneAcceso(_db, User.Identity?.Name, "Ventas", "Facturas");
        private bool PuedeVender() => PermisoHelper.UsuarioTienePermiso(_db, User.Identity?.Name, "Ventas", "Create", "crear");

        private static JsonResult Resp(bool ok, string mensaje) => new(new { ok, mensaje });

        public async Task<IActionResult> OnPostFacturaAsync(int id)
        {
            if (!PuedeFacturas()) return Resp(false, "No tienes permiso para imprimir facturas.");
            var r = await _impresora.ImprimirFacturaAsync(id);
            return Resp(r.Ok, r.Mensaje);
        }

        public async Task<IActionResult> OnPostVentaAsync(int id)
        {
            if (!PuedeVender() && !PuedeFacturas()) return Resp(false, "No tienes permiso para imprimir tickets de venta.");
            var r = await _impresora.ImprimirVentaAsync(id);
            return Resp(r.Ok, r.Mensaje);
        }

        public async Task<IActionResult> OnPostGavetaAsync()
        {
            if (!PuedeVender() && !PuedeFacturas()) return Resp(false, "No tienes permiso para abrir la gaveta.");
            var r = await _impresora.AbrirGavetaAsync();
            return Resp(r.Ok, r.Mensaje);
        }
    }
}
