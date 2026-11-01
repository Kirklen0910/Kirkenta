using System.Text.Json;
using Kirkenta.Services.Asistente;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Asistente
{
    public class IndexModel : PageModel
    {
        private readonly AsistenteService _asistente;

        public IndexModel(AsistenteService asistente)
        {
            _asistente = asistente;
        }

        public bool IaActiva => _asistente.IaActiva;

        /// <summary>Acciones que el usuario actual puede usar, agrupadas por módulo (para la pantalla de ayuda).</summary>
        public List<IGrouping<string, AccionVista>> Grupos { get; set; } = new();

        public record AccionVista(string Modulo, string Titulo, string Descripcion, string Ejemplo, bool RequierePassword);

        public void OnGet()
        {
            var json = JsonSerializer.Serialize(_asistente.Catalogo(User.Identity?.Name));
            var lista = JsonSerializer.Deserialize<List<JsonElement>>(json) ?? new();
            var acciones = new List<AccionVista>();
            foreach (var e in lista)
            {
                var def = AsistenteCatalogo.Buscar(e.GetProperty("Id").GetString());
                if (def == null) continue;
                acciones.Add(new AccionVista(def.Modulo, def.Titulo, def.Descripcion, def.Ejemplo, def.RequierePassword));
            }
            Grupos = acciones.GroupBy(a => a.Modulo).ToList();
        }

        /// <summary>Lista de acciones para los atajos del chat flotante.</summary>
        public IActionResult OnGetCatalogo() => new JsonResult(_asistente.Catalogo(User.Identity?.Name));

        public async Task<IActionResult> OnPostMensajeAsync(string? texto, string? contexto)
        {
            ContextoCliente? ctx = null;
            if (!string.IsNullOrWhiteSpace(contexto))
            {
                try { ctx = JsonSerializer.Deserialize<ContextoCliente>(contexto, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
                catch { ctx = null; }
            }
            var r = await _asistente.ProcesarAsync(texto, ctx, User.Identity?.Name, HttpContext.Connection.RemoteIpAddress?.ToString());
            return new JsonResult(r);
        }

        public IActionResult OnPostConfirmar(string? token, string? password, string? autorizador)
        {
            var r = _asistente.Confirmar(token, password, autorizador, User.Identity?.Name, HttpContext.Connection.RemoteIpAddress?.ToString());
            return new JsonResult(r);
        }
    }
}
