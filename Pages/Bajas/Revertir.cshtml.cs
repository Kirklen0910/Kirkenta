using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Bajas
{
    public class RevertirModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RevertirModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string Numero { get; set; } = "";
        public string ProductoNombre { get; set; } = "";
        public decimal Cantidad { get; set; }
        public decimal ValorTotal { get; set; }

        public class InputModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "Debes indicar el motivo de la reversión")]
            [StringLength(1000, MinimumLength = 10, ErrorMessage = "El motivo debe tener al menos 10 caracteres")]
            public string MotivoReversion { get; set; } = string.Empty;
        }

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores pueden revertir bajas";
                return RedirectToPage("/Bajas/Index");
            }

            var baja = _context.BajasInventario.FirstOrDefault(b => b.Id == id);
            if (baja == null)
            {
                TempData["Error"] = "Baja no encontrada";
                return RedirectToPage("/Bajas/Index");
            }

            if (baja.Estado != "Aprobada" || baja.Revertida)
            {
                TempData["Error"] = "Esta baja no puede revertirse";
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
                TempData["Error"] = "Solo administradores pueden revertir bajas";
                return RedirectToPage("/Bajas/Index");
            }

            var baja = _context.BajasInventario.FirstOrDefault(b => b.Id == Input.Id);
            if (baja == null)
            {
                TempData["Error"] = "Baja no encontrada";
                return RedirectToPage("/Bajas/Index");
            }

            if (baja.Estado != "Aprobada" || baja.Revertida)
            {
                TempData["Error"] = "Esta baja no puede revertirse";
                return RedirectToPage("/Bajas/Details", new { id = baja.Id });
            }

            if (!ModelState.IsValid)
            {
                CargarDatos(baja);
                return Page();
            }

            // Reclamo atómico: evita revertir dos veces (doble clic / dos admins).
            // En transacción: si falla el ajuste de stock, la baja no queda marcada como revertida.
            using var tx = _context.Database.IsRelational() ? _context.Database.BeginTransaction() : null;
            if (_context.Database.IsRelational())
            {
                var reclamadas = _context.BajasInventario
                    .Where(b => b.Id == baja.Id && b.Estado == "Aprobada" && !b.Revertida)
                    .ExecuteUpdate(u => u.SetProperty(b => b.Revertida, true));
                if (reclamadas == 0)
                {
                    TempData["Error"] = "Esta baja ya fue revertida";
                    return RedirectToPage("/Bajas/Details", new { id = baja.Id });
                }
                _context.Entry(baja).Reload();
            }

            // Restaurar stock
            var producto = _context.Productos.FirstOrDefault(p => p.Id == baja.ProductoId);
            if (producto != null)
            {
                var costoBaja = baja.Cantidad > 0 ? baja.ValorTotal / baja.Cantidad : (decimal?)null;
                InventarioHelper.Registrar(_context, producto, baja.Cantidad, "ReversionBaja",
                    baja.Numero, baja.Id, currentUser!.Id, costoBaja, $"Reversión de baja: {Input.MotivoReversion}");
            }

            baja.Revertida = true;
            baja.FechaReversion = DateTime.Now;
            baja.UsuarioRevierteId = currentUser!.Id;
            baja.MotivoReversion = Input.MotivoReversion;

            try
            {
                _context.SaveChanges();
                tx?.Commit();
            }
            catch (DbUpdateConcurrencyException)
            {
                tx?.Rollback();
                TempData["Error"] = "El stock del producto cambió mientras se revertía. Intenta de nuevo.";
                return RedirectToPage("/Bajas/Details", new { id = baja.Id });
            }

            ActividadHelper.Registrar(
                _context,
                currentUser.Id,
                "Revertir baja",
                $"Revirtió baja {baja.Numero}: +{baja.Cantidad} x '{producto?.Nombre}'. Motivo: {Input.MotivoReversion}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Baja {baja.Numero} revertida. Stock restaurado.";
            return RedirectToPage("/Bajas/Details", new { id = baja.Id });
        }

        private void CargarDatos(BajaInventario baja)
        {
            Numero = baja.Numero;
            var producto = _context.Productos.FirstOrDefault(p => p.Id == baja.ProductoId);
            ProductoNombre = producto?.Nombre ?? "—";
            Cantidad = baja.Cantidad;
            ValorTotal = baja.ValorTotal;
        }
    }
}