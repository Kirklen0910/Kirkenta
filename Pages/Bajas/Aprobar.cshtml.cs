using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Bajas
{
    public class AprobarModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AprobarModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string Numero { get; set; } = "";
        public string ProductoNombre { get; set; } = "";
        public string TipoBaja { get; set; } = "";
        public decimal Cantidad { get; set; }
        public decimal ValorTotal { get; set; }
        public string Motivo { get; set; } = "";
        public string UsuarioSolicita { get; set; } = "";

        public class InputModel
        {
            public int Id { get; set; }
            public bool Aprobar { get; set; } = true;
            public string? MotivoRechazo { get; set; }
        }

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores pueden aprobar bajas";
                return RedirectToPage("/Bajas/Index");
            }

            var baja = _context.BajasInventario.FirstOrDefault(b => b.Id == id);
            if (baja == null)
            {
                TempData["Error"] = "Baja no encontrada";
                return RedirectToPage("/Bajas/Index");
            }

            if (baja.Estado != "Pendiente")
            {
                TempData["Error"] = "Esta baja ya fue procesada";
                return RedirectToPage("/Bajas/Details", new { id });
            }

            CargarDatos(baja);
            Input.Id = baja.Id;
            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores pueden aprobar bajas";
                return RedirectToPage("/Bajas/Index");
            }

            var baja = _context.BajasInventario.FirstOrDefault(b => b.Id == Input.Id);
            if (baja == null)
            {
                TempData["Error"] = "Baja no encontrada";
                return RedirectToPage("/Bajas/Index");
            }

            if (baja.Estado != "Pendiente")
            {
                TempData["Error"] = "Esta baja ya fue procesada";
                return RedirectToPage("/Bajas/Details", new { id = baja.Id });
            }

            // Si rechaza, requiere motivo
            if (!Input.Aprobar && string.IsNullOrWhiteSpace(Input.MotivoRechazo))
            {
                ModelState.AddModelError("Input.MotivoRechazo", "Debes indicar el motivo del rechazo");
                CargarDatos(baja);
                return Page();
            }

            if (Input.Aprobar)
            {
                // APROBAR: descontar stock
                var producto = _context.Productos.FirstOrDefault(p => p.Id == baja.ProductoId);
                if (producto != null)
                {
                    producto.Stock -= baja.Cantidad;
                }

                baja.Estado = "Aprobada";
                baja.UsuarioApruebaId = currentUser!.Id;
                baja.FechaAprobacion = DateTime.Now;

                ActividadHelper.Registrar(
                    _context,
                    currentUser.Id,
                    "Aprobar baja",
                    $"Aprobó baja {baja.Numero}: {baja.Cantidad} x '{producto?.Nombre}'",
                    HttpContext.Connection.RemoteIpAddress?.ToString());

                TempData["Success"] = $"Baja {baja.Numero} aprobada. Stock actualizado.";
            }
            else
            {
                // RECHAZAR
                baja.Estado = "Rechazada";
                baja.UsuarioApruebaId = currentUser!.Id;
                baja.FechaAprobacion = DateTime.Now;
                baja.MotivoRechazo = Input.MotivoRechazo;

                ActividadHelper.Registrar(
                    _context,
                    currentUser.Id,
                    "Rechazar baja",
                    $"Rechazó baja {baja.Numero}: {Input.MotivoRechazo}",
                    HttpContext.Connection.RemoteIpAddress?.ToString());

                TempData["Success"] = $"Baja {baja.Numero} rechazada.";
            }

            _context.SaveChanges();
            return RedirectToPage("/Bajas/Details", new { id = baja.Id });
        }

        private void CargarDatos(BajaInventario baja)
        {
            Numero = baja.Numero;
            var producto = _context.Productos.FirstOrDefault(p => p.Id == baja.ProductoId);
            ProductoNombre = producto?.Nombre ?? "—";
            TipoBaja = baja.TipoBaja;
            Cantidad = baja.Cantidad;
            ValorTotal = baja.ValorTotal;
            Motivo = baja.Motivo;
            UsuarioSolicita = _context.Usuarios.FirstOrDefault(u => u.Id == baja.UsuarioSolicitaId)?.Username ?? "—";
        }
    }
}