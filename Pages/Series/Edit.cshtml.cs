using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Series
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
            public string Tipo { get; set; } = string.Empty;

            [Required]
            [StringLength(50)]
            public string Nombre { get; set; } = "Principal";

            [Required]
            [StringLength(20)]
            public string Prefijo { get; set; } = string.Empty;

            [StringLength(20)]
            public string? Sufijo { get; set; }

            [StringLength(5)]
            public string Separador { get; set; } = "-";

            [Range(1, 10)]
            public int LongitudNumero { get; set; } = 4;

            [Range(1, int.MaxValue)]
            public int SiguienteNumero { get; set; } = 1;

            [StringLength(100)]
            public string? FormatoPersonalizado { get; set; }

            public bool EsPredeterminada { get; set; }
            public bool Activa { get; set; }
        }

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/Series/Index");
            }

            var serie = _context.SeriesDocumentos.FirstOrDefault(s => s.Id == id);
            if (serie == null)
            {
                TempData["Error"] = "Serie no encontrada";
                return RedirectToPage("/Series/Index");
            }

            Input = new InputModel
            {
                Id = serie.Id,
                Tipo = serie.Tipo,
                Nombre = serie.Nombre,
                Prefijo = serie.Prefijo,
                Sufijo = serie.Sufijo,
                Separador = serie.Separador,
                LongitudNumero = serie.LongitudNumero,
                SiguienteNumero = serie.SiguienteNumero,
                FormatoPersonalizado = serie.FormatoPersonalizado,
                EsPredeterminada = serie.EsPredeterminada,
                Activa = serie.Activa
            };

            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/Series/Index");
            }

            if (!ModelState.IsValid) return Page();

            var serie = _context.SeriesDocumentos.FirstOrDefault(s => s.Id == Input.Id);
            if (serie == null)
            {
                TempData["Error"] = "Serie no encontrada";
                return RedirectToPage("/Series/Index");
            }

            // Si es predeterminada, quitar la predeterminada anterior del mismo tipo
            if (Input.EsPredeterminada && !serie.EsPredeterminada)
            {
                var anteriores = _context.SeriesDocumentos
                    .Where(s => s.Tipo == Input.Tipo && s.EsPredeterminada && s.Id != Input.Id)
                    .ToList();
                foreach (var s in anteriores) s.EsPredeterminada = false;
            }

            serie.Tipo = Input.Tipo;
            serie.Nombre = Input.Nombre;
            serie.Prefijo = Input.Prefijo;
            serie.Sufijo = Input.Sufijo;
            serie.Separador = Input.Separador;
            serie.LongitudNumero = Input.LongitudNumero;
            serie.SiguienteNumero = Input.SiguienteNumero;
            serie.FormatoPersonalizado = Input.FormatoPersonalizado;
            serie.EsPredeterminada = Input.EsPredeterminada;
            serie.Activa = Input.Activa;

            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Editar serie",
                $"Editó la serie '{serie.Nombre}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Serie '{serie.Nombre}' actualizada";
            return RedirectToPage("/Series/Index");
        }
    }
}