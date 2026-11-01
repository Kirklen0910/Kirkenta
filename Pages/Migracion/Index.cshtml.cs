using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Import;
using Kirkenta.Helpers.Migracion;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Migracion
{
    [RequestSizeLimit(200_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 200_000_000)]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context) { _context = context; }

        [BindProperty] public IFormFile? Archivo { get; set; }

        public bool HayPrevio { get; set; }
        public List<DeteccionTabla> Detecciones { get; set; } = new();
        public List<string> Avisos { get; set; } = new();
        public string? Token { get; set; }
        public string NombreArchivo { get; set; } = "";
        public List<MigracionHistorialHelper.LoteInfo> Lotes { get; set; } = new();

        private bool EsAdmin()
        {
            var u = _context.Usuarios.FirstOrDefault(x => x.Username == User.Identity!.Name);
            return u?.Rol == "Admin";
        }

        public IActionResult OnGet()
        {
            if (!EsAdmin()) { TempData["Error"] = "Solo el administrador puede usar la migración."; return RedirectToPage("/Index"); }

            Lotes = MigracionHistorialHelper.ListarLotes(_context);
            var token = HttpContext.Session.GetString(MigracionAlmacen.SessionToken);
            if (!string.IsNullOrEmpty(token) && Request.Query.ContainsKey("continuar"))
            {
                var tablas = MigracionAlmacen.Cargar(token);
                if (tablas != null) CargarAnalisis(token, tablas);
            }
            else
            {
                HayPrevio = !string.IsNullOrEmpty(token) && MigracionAlmacen.Cargar(token) != null;
            }
            return Page();
        }

        public IActionResult OnPostSubir()
        {
            if (!EsAdmin()) return RedirectToPage("/Index");

            if (Archivo == null || Archivo.Length == 0)
            {
                TempData["Error"] = "Selecciona el archivo del sistema anterior.";
                return RedirectToPage();
            }
            if (Archivo.Length > 200_000_000)
            {
                TempData["Error"] = "El archivo supera 200 MB. Expórtalo por partes (por ejemplo una tabla por archivo).";
                return RedirectToPage();
            }

            ResultadoMigracionLectura lectura;
            using (var ms = new MemoryStream())
            {
                Archivo.CopyTo(ms);
                ms.Position = 0;
                lectura = MigracionLector.Leer(Archivo.FileName, ms);
            }

            return Analizar(lectura, Path.GetFileName(Archivo.FileName));
        }

        /// <summary>Datos de conexión a la base del sistema anterior (no se guardan en ningún lado).</summary>
        [BindProperty] public DatosConexion Conexion { get; set; } = new();

        /// <summary>Lee todas las tablas directamente de la base del sistema anterior (solo lectura).</summary>
        public IActionResult OnPostConectar()
        {
            if (!EsAdmin()) return RedirectToPage("/Index");
            if (!MigracionConexionDirecta.Motores.Contains(Conexion.Motor) || string.IsNullOrWhiteSpace(Conexion.BaseDatos))
            {
                TempData["Error"] = "Elige el tipo de base de datos y escribe el nombre de la base (o la ruta del archivo .fdb).";
                return RedirectToPage();
            }
            var lectura = MigracionConexionDirecta.Leer(Conexion);
            Conexion.Clave = "";   // no se devuelve a la pantalla
            return Analizar(lectura, $"{MigracionConexionDirecta.NombreMotor(Conexion.Motor)} · {Conexion.BaseDatos}");
        }

        private IActionResult Analizar(ResultadoMigracionLectura lectura, string origen)
        {
            if (lectura.Tablas.Count == 0)
            {
                TempData["Error"] = "No se encontraron tablas con datos. " + string.Join(" ", lectura.Avisos);
                return RedirectToPage();
            }

            var anterior = HttpContext.Session.GetString(MigracionAlmacen.SessionToken);
            MigracionAlmacen.Eliminar(anterior);

            var token = MigracionAlmacen.Guardar(lectura.Tablas);
            HttpContext.Session.SetString(MigracionAlmacen.SessionToken, token);

            var user = _context.Usuarios.FirstOrDefault(x => x.Username == User.Identity!.Name);
            ActividadHelper.Registrar(_context, user?.Id ?? 0, "Migración: analizar datos",
                $"Analizó «{origen}»: {lectura.Tablas.Count} tablas, {lectura.Tablas.Sum(t => t.Rows.Count)} filas",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            NombreArchivo = origen;
            Lotes = MigracionHistorialHelper.ListarLotes(_context);
            CargarAnalisis(token, lectura.Tablas);
            Avisos = lectura.Avisos;
            return Page();
        }

        /// <summary>Envía una tabla (o un lote de 5.000 filas) al asistente de importación del módulo elegido.</summary>
        public IActionResult OnPostEnviar(string token, int tabla, string entidad, int lote)
        {
            if (!EsAdmin()) return RedirectToPage("/Index");

            var tablas = MigracionAlmacen.Cargar(token);
            if (tablas == null || tabla < 0 || tabla >= tablas.Count)
            {
                TempData["Error"] = "La migración expiró. Vuelve a subir el archivo.";
                return RedirectToPage();
            }

            if (!string.IsNullOrEmpty(entidad) && entidad.StartsWith("@"))
            {
                var tipo = entidad.Substring(1);
                if (!MigracionHistorialHelper.Tipos.Contains(tipo)) return RedirectToPage(new { continuar = 1 });
                HttpContext.Session.SetString(MigracionAlmacen.SessionToken, token);
                return RedirectToPage("/Migracion/Historial", new { Token = token, Tabla = tabla, Tipo = tipo });
            }

            var ent = MigracionAnalizador.BuscarEntidad(entidad);
            if (ent == null)
            {
                TempData["Error"] = "Elige a qué módulo va esa tabla.";
                HttpContext.Session.SetString(MigracionAlmacen.SessionToken, token);
                return RedirectToPage(new { continuar = 1 });
            }

            var t = tablas[tabla];

            // Empleados, cajas y bancos, plan de cuentas y métodos de pago no tienen asistente de columnas:
            // se importan directo con las columnas detectadas (todo o nada) y se informa el resultado.
            if (string.IsNullOrEmpty(ent.UrlImportMap))
            {
                var mapa = MigracionCatalogosHelper.Detectar(t.Headers, ent.Key);
                var falta = MigracionCatalogosHelper.Faltantes(ent.Key, mapa);
                HttpContext.Session.SetString(MigracionAlmacen.SessionToken, token);
                if (falta.Count > 0)
                {
                    TempData["Error"] = $"«{t.Nombre}»: no se encontró la columna de {string.Join(", ", falta)}. Usa la migración automática para elegir las columnas.";
                    return RedirectToPage(new { continuar = 1 });
                }
                var usuario = _context.Usuarios.FirstOrDefault(x => x.Username == User.Identity!.Name);
                using var tx = _context.Database.BeginTransaction();
                var r = MigracionCatalogosHelper.Procesar(_context, t, ent.Key, mapa, usuario?.Id, true);
                tx.Commit();
                TempData["Success"] = $"{ent.Nombre}: {r.Creados} creado(s), {r.YaImportados} ya existían, {r.DescartadasTotal} fila(s) descartadas."
                    + (r.Advertencias.Count > 0 ? " " + string.Join(" ", r.Advertencias.Take(3)) : "");
                return RedirectToPage(new { continuar = 1 });
            }

            if (lote < 0) lote = 0;
            var filas = t.Rows.Skip(lote * MigracionAlmacen.FilasPorLote).Take(MigracionAlmacen.FilasPorLote).ToList();
            if (filas.Count == 0)
            {
                TempData["Error"] = "Ese lote no tiene filas.";
                return RedirectToPage(new { continuar = 1 });
            }

            var cfg = new ImportWizardConfig { SessionKey = ent.SessionKey };
            ImportWizardHelper.GuardarEnSesion(HttpContext.Session, cfg, t.Headers, filas);
            HttpContext.Session.SetString(MigracionAlmacen.SessionToken, token);

            return Redirect(ent.UrlImportMap);
        }

        public IActionResult OnPostDeshacerLote(string lote)
        {
            if (!EsAdmin()) return RedirectToPage("/Index");
            var (borrados, conservados) = MigracionHistorialHelper.DeshacerLote(_context, lote);
            var user = _context.Usuarios.FirstOrDefault(x => x.Username == User.Identity!.Name);
            ActividadHelper.Registrar(_context, user?.Id ?? 0, "Migración: deshacer lote",
                $"Lote {lote}: {borrados} registros borrados, {conservados.Count} conservados",
                HttpContext.Connection.RemoteIpAddress?.ToString());
            TempData["Success"] = $"Lote {lote} deshecho: {borrados} registros eliminados." +
                (conservados.Count > 0 ? $" Se conservaron {conservados.Count} que ya tienen cobros, pagos o movimientos (ej.: {conservados[0]})." : "");
            return RedirectToPage(new { continuar = 1 });
        }

        public IActionResult OnPostDescartar()
        {
            if (!EsAdmin()) return RedirectToPage("/Index");
            MigracionAlmacen.Eliminar(HttpContext.Session.GetString(MigracionAlmacen.SessionToken));
            HttpContext.Session.Remove(MigracionAlmacen.SessionToken);
            TempData["Success"] = "Migración descartada. Los datos temporales se borraron.";
            return RedirectToPage();
        }

        private void CargarAnalisis(string token, List<TablaOrigen> tablas)
        {
            Token = token;
            HayPrevio = false;
            Detecciones = MigracionAnalizador.Analizar(tablas)
                .OrderBy(d => d.Entidad?.Orden ?? 99)
                .ThenBy(d => d.Indice)
                .ToList();
        }
    }
}
