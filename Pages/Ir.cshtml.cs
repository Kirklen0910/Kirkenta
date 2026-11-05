using Kirkenta.Data;
using Kirkenta.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages
{
    /// <summary>
    /// «Ir a»: lleva al detalle de cualquier documento por su número (/Ir?doc=VEN-0012) y le dice a las
    /// pantallas qué números y nombres se pueden volver enlace (handler Resolver).
    /// </summary>
    public class IrModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public IrModel(ApplicationDbContext db) { _db = db; }

        public string? Doc { get; set; }

        public IActionResult OnGet(string? doc)
        {
            Doc = doc?.Trim();
            if (string.IsNullOrEmpty(Doc)) return Page();
            var url = DocumentoEnlaceHelper.Documentos(_db, User.Identity?.Name, new[] { Doc }).Values.FirstOrDefault();
            return url != null ? LocalRedirect(url) : Page();
        }

        /// <summary>docs, clientes y proveedores separados por «|»; responde solo lo que existe y se puede abrir.</summary>
        public IActionResult OnGetResolver(string? docs, string? clientes, string? proveedores)
        {
            static IEnumerable<string> Partes(string? s) => (s ?? "").Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var u = User.Identity?.Name;
            return new JsonResult(new
            {
                docs = DocumentoEnlaceHelper.Documentos(_db, u, Partes(docs)),
                clientes = DocumentoEnlaceHelper.Terceros(_db, u, Partes(clientes), false),
                proveedores = DocumentoEnlaceHelper.Terceros(_db, u, Partes(proveedores), true)
            });
        }
    }
}
