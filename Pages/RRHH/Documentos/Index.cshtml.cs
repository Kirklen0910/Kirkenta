using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.RRHH.Documentos
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<DocumentoItem> Documentos { get; set; } = new();
        public List<TipoDocumentoEmpleado> TiposDocumento { get; set; } = new();

        public int TotalDocumentos { get; set; }
        public int DocumentosVencidos { get; set; }
        public int DocumentosPorVencer { get; set; }
        public int DocumentosVigentes { get; set; }

        public class DocumentoItem
        {
            public int Id { get; set; }
            public int EmpleadoId { get; set; }
            public string EmpleadoCodigo { get; set; } = "";
            public string EmpleadoNombre { get; set; } = "";
            public string? FotoPath { get; set; }
            public string TipoDocumentoNombre { get; set; } = "";
            public string TipoDocumentoCategoria { get; set; } = "";
            public string NombreArchivo { get; set; } = "";
            public string RutaArchivo { get; set; } = "";
            public string? Descripcion { get; set; }
            public int TamanoKB { get; set; }
            public DateTime FechaSubida { get; set; }
            public DateTime? FechaVencimiento { get; set; }
            public bool Vigente { get; set; }
            public string Estado => !Vigente ? "Vencido" :
                                     (FechaVencimiento.HasValue && FechaVencimiento.Value <= DateTime.Today.AddDays(30))
                                        ? "PorVencer" : "Vigente";
        }

        public IActionResult OnGet(int? empleadoId, string? categoria)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "Documentos", "ver"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Index");
            }

            TiposDocumento = _context.TiposDocumentoEmpleado
                .Where(t => t.Activo)
                .OrderBy(t => t.Orden)
                .ToList();

            var tiposDict = TiposDocumento.ToDictionary(t => t.Id, t => new { t.Nombre, t.Categoria });

            var empleadosDict = _context.Empleados
                .AsNoTracking()
                .ToDictionary(e => e.Id, e => new { e.Codigo, Nombre = $"{e.Nombres} {e.Apellidos}", e.FotoPath });

            var query = _context.AdjuntosEmpleado.AsNoTracking().AsQueryable();

            if (empleadoId.HasValue)
                query = query.Where(a => a.EmpleadoId == empleadoId.Value);

            var lista = query.OrderByDescending(a => a.FechaSubida).ToList();

            var items = lista.Select(a =>
            {
                var emp = empleadosDict.GetValueOrDefault(a.EmpleadoId);
                var tipo = tiposDict.GetValueOrDefault(a.TipoDocumentoId);
                return new DocumentoItem
                {
                    Id = a.Id,
                    EmpleadoId = a.EmpleadoId,
                    EmpleadoCodigo = emp?.Codigo ?? "—",
                    EmpleadoNombre = emp?.Nombre ?? "—",
                    FotoPath = emp?.FotoPath,
                    TipoDocumentoNombre = tipo?.Nombre ?? "—",
                    TipoDocumentoCategoria = tipo?.Categoria ?? "Otros",
                    NombreArchivo = a.NombreArchivo,
                    RutaArchivo = a.RutaArchivo,
                    Descripcion = a.Descripcion,
                    TamanoKB = a.TamanoKB,
                    FechaSubida = a.FechaSubida,
                    FechaVencimiento = a.FechaVencimiento,
                    Vigente = a.Vigente
                };
            }).ToList();

            // Filtro por categoría
            if (!string.IsNullOrWhiteSpace(categoria))
                items = items.Where(i => i.TipoDocumentoCategoria == categoria).ToList();

            Documentos = items;

            TotalDocumentos = items.Count;
            DocumentosVencidos = items.Count(i => !i.Vigente);
            DocumentosPorVencer = items.Count(i => i.Vigente && i.FechaVencimiento.HasValue && i.FechaVencimiento.Value <= DateTime.Today.AddDays(30) && i.FechaVencimiento.Value >= DateTime.Today);
            DocumentosVigentes = TotalDocumentos - DocumentosVencidos - DocumentosPorVencer;

            return Page();
        }

        public IActionResult OnPostEliminar(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "DocumentosDelete", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Documentos/Index");
            }

            var adjunto = _context.AdjuntosEmpleado.FirstOrDefault(a => a.Id == id);
            if (adjunto == null)
            {
                TempData["Error"] = "Documento no encontrado";
                return RedirectToPage("/RRHH/Documentos/Index");
            }

            var env = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
            Helpers.RRHH.AdjuntoEmpleadoHelper.Eliminar(_context, env, id);

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Eliminar documento empleado",
                $"Eliminó el documento '{adjunto.NombreArchivo}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = "Documento eliminado";
            return RedirectToPage("/RRHH/Documentos/Index");
        }
    }
}