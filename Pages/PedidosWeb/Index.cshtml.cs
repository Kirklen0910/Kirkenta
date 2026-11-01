using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Kirkenta.Pages.PedidosWeb
{
    /// <summary>Bandeja de pedidos que llegan desde el catálogo en línea.</summary>
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public IndexModel(ApplicationDbContext db) { _db = db; }

        public record LineaVista(string Nombre, decimal Cantidad, decimal Precio);
        public record Fila(PedidoWeb Pedido, List<LineaVista> Lineas, string? EnlaceWhatsApp);

        public List<Fila> Pedidos { get; set; } = new();
        [BindProperty(SupportsGet = true)] public string Estado { get; set; } = "Nuevo";
        public int Nuevos { get; set; }
        public bool PuedeEditar { get; set; }
        public bool PuedeCobrar { get; set; }

        private bool Puede(string accion) => PermisoHelper.UsuarioTienePermiso(_db, User.Identity?.Name, "Ventas", "Pedidos", accion);

        private void Cargar()
        {
            PuedeEditar = Puede("editar");
            PuedeCobrar = PermisoHelper.UsuarioTienePermiso(_db, User.Identity?.Name, "Ventas", "Create", "crear");
            Nuevos = _db.PedidosWeb.Count(p => p.Estado == "Nuevo");
            var q = _db.PedidosWeb.AsNoTracking().AsQueryable();
            if (Estado is "Nuevo" or "Atendido" or "Cancelado") q = q.Where(p => p.Estado == Estado);
            var lista = q.OrderByDescending(p => p.Fecha).Take(100).ToList();
            var negocio = _db.ConfiguracionEmpresa.Select(e => e.Nombre).FirstOrDefault() ?? "Kirkenta";
            Pedidos = lista.Select(p =>
            {
                List<LineaVista> lineas;
                try
                {
                    using var doc = JsonDocument.Parse(p.ItemsJson);
                    lineas = doc.RootElement.EnumerateArray().Select(e => new LineaVista(
                        e.GetProperty("nombre").GetString() ?? "",
                        e.GetProperty("cantidad").GetDecimal(),
                        e.GetProperty("precio").GetDecimal())).ToList();
                }
                catch { lineas = new(); }
                var msg = $"Hola {p.Nombre}, te escribimos de {negocio} por tu pedido {p.Numero}.";
                return new Fila(p, lineas, WhatsAppHelper.Enlace(p.Telefono, msg));
            }).ToList();
        }

        public IActionResult OnGet()
        {
            if (!Puede("ver")) { TempData["Error"] = "No tienes permiso para ver los pedidos web."; return Redirect("/"); }
            Cargar();
            return Page();
        }

        public IActionResult OnPostCambiar(int id, string nuevo)
        {
            if (!Puede("editar")) { TempData["Error"] = "No tienes permiso para modificar pedidos."; return Redirect("/"); }
            if (nuevo is not ("Nuevo" or "Atendido" or "Cancelado")) return RedirectToPage(new { Estado });
            var p = _db.PedidosWeb.FirstOrDefault(x => x.Id == id);
            if (p != null)
            {
                p.Estado = nuevo;
                _db.SaveChanges();
                var u = _db.Usuarios.FirstOrDefault(x => x.Username == User.Identity!.Name);
                if (u != null) ActividadHelper.Registrar(_db, u.Id, "Pedido web", $"Marcó el pedido {p.Numero} como {nuevo}", HttpContext.Connection.RemoteIpAddress?.ToString());
            }
            return RedirectToPage(new { Estado });
        }
    }
}
