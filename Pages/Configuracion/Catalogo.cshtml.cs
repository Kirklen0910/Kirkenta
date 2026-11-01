using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.Configuracion
{
    /// <summary>Activa y personaliza el catálogo en línea público. Solo Admin.</summary>
    public class CatalogoModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public CatalogoModel(ApplicationDbContext db) { _db = db; }

        public class Ajustes
        {
            public bool Habilitado { get; set; }
            public string? Titulo { get; set; }
            public string? Mensaje { get; set; }
            public string? WhatsApp { get; set; }
            public bool MostrarStock { get; set; }
            public decimal PedidoMinimo { get; set; }
        }

        [BindProperty] public Ajustes Form { get; set; } = new();
        public string Enlace { get; set; } = "";
        public string TelefonoEmpresa { get; set; } = "";
        public int ProductosPublicados { get; set; }
        public int PedidosNuevos { get; set; }

        private bool EsAdmin()
        {
            var nombre = User.Identity?.Name?.Trim();
            return !string.IsNullOrEmpty(nombre) && PermisoHelper.UsuarioTieneAcceso(_db, nombre, "Configuracion", "Catalogo");
        }

        private void Cargar()
        {
            var c = _db.CatalogoWebConfig.AsNoTracking().FirstOrDefault();
            if (c != null)
                Form = new Ajustes { Habilitado = c.Habilitado, Titulo = c.Titulo, Mensaje = c.Mensaje, WhatsApp = c.WhatsApp, MostrarStock = c.MostrarStock, PedidoMinimo = c.PedidoMinimo };
            Enlace = $"{Request.Scheme}://{Request.Host}/Catalogo";
            TelefonoEmpresa = _db.ConfiguracionEmpresa.Select(e => e.Telefono).FirstOrDefault() ?? "";
            ProductosPublicados = _db.Productos.Count(p => p.Activo && p.Stock > 0 && p.PrecioVenta > 0);
            PedidosNuevos = _db.PedidosWeb.Count(p => p.Estado == "Nuevo");
        }

        public IActionResult OnGet()
        {
            if (!EsAdmin()) { TempData["Error"] = "Solo un administrador puede configurar el catálogo en línea."; return Redirect("/"); }
            Cargar();
            return Page();
        }

        public IActionResult OnPost()
        {
            if (!EsAdmin()) { TempData["Error"] = "Solo un administrador puede configurar el catálogo en línea."; return Redirect("/"); }

            var wa = CatalogoWebHelper.Limpiar(Form.WhatsApp, 30);
            if (wa.Length > 0 && WhatsAppHelper.Normalizar(wa) == null)
            {
                ModelState.AddModelError("", "El número de WhatsApp no es válido. Usa el formato +504 9999-9999.");
                Cargar();
                return Page();
            }
            if (Form.Habilitado && wa.Length == 0 && string.IsNullOrWhiteSpace(_db.ConfiguracionEmpresa.Select(e => e.Telefono).FirstOrDefault()))
            {
                ModelState.AddModelError("", "Para activar el catálogo indica el WhatsApp que recibirá los pedidos (o registra el teléfono de la empresa).");
                Cargar();
                return Page();
            }

            var c = _db.CatalogoWebConfig.FirstOrDefault();
            if (c == null) { c = new CatalogoWebConfig(); _db.CatalogoWebConfig.Add(c); }
            c.Habilitado = Form.Habilitado;
            c.Titulo = CatalogoWebHelper.Limpiar(Form.Titulo, 120) is { Length: > 0 } t ? t : null;
            c.Mensaje = CatalogoWebHelper.Limpiar(Form.Mensaje, 300) is { Length: > 0 } m ? m : null;
            c.WhatsApp = wa.Length > 0 ? wa : null;
            c.MostrarStock = Form.MostrarStock;
            c.PedidoMinimo = Math.Max(0, Form.PedidoMinimo);
            _db.SaveChanges();

            var usuario = _db.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            if (usuario != null)
                ActividadHelper.Registrar(_db, usuario.Id, "Catálogo en línea", Form.Habilitado ? "Activó/actualizó el catálogo en línea" : "Desactivó el catálogo en línea",
                    HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = Form.Habilitado ? "Catálogo guardado y activo." : "Catálogo guardado (desactivado: nadie puede verlo).";
            return RedirectToPage();
        }
    }
}
