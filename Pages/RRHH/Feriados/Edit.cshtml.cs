using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.RRHH.Feriados
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public int Id { get; set; }

            [Required]
            [StringLength(150)]
            public string Nombre { get; set; } = "";

            [Required]
            public DateTime Fecha { get; set; }

            [Required]
            [StringLength(3)]
            public string PaisCodigo { get; set; } = "HN";

            [Required]
            public string Tipo { get; set; } = "Nacional";

            public bool EsRecurrente { get; set; }
            public bool EsMovil { get; set; }

            [StringLength(300)]
            public string? Descripcion { get; set; }

            public bool Activo { get; set; }
        }

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" && !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "FeriadosCreate", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Feriados/Index");
            }

            var feriado = _context.Feriados.FirstOrDefault(f => f.Id == id);
            if (feriado == null)
            {
                TempData["Error"] = "Feriado no encontrado";
                return RedirectToPage("/RRHH/Feriados/Index");
            }

            Input = new InputModel
            {
                Id = feriado.Id,
                Nombre = feriado.Nombre,
                Fecha = feriado.Fecha,
                PaisCodigo = feriado.PaisCodigo,
                Tipo = feriado.Tipo,
                EsRecurrente = feriado.EsRecurrente,
                EsMovil = feriado.EsMovil,
                Descripcion = feriado.Descripcion,
                Activo = feriado.Activo
            };

            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" && !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "FeriadosCreate", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Feriados/Index");
            }

            if (!ModelState.IsValid) return Page();

            var feriado = _context.Feriados.FirstOrDefault(f => f.Id == Input.Id);
            if (feriado == null)
            {
                TempData["Error"] = "Feriado no encontrado";
                return RedirectToPage("/RRHH/Feriados/Index");
            }

            feriado.Nombre = Input.Nombre;
            feriado.Fecha = Input.Fecha;
            feriado.PaisCodigo = Input.PaisCodigo;
            feriado.Tipo = Input.Tipo;
            feriado.EsRecurrente = Input.EsRecurrente;
            feriado.EsMovil = Input.EsMovil;
            feriado.Descripcion = Input.Descripcion;
            feriado.Activo = Input.Activo;

            _context.SaveChanges();

            TempData["Success"] = "Feriado actualizado";
            return RedirectToPage("/RRHH/Feriados/Index");
        }
    }
}