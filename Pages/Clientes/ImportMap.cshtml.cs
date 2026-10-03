using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Import;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace Kirkenta.Pages.Clientes
{
    public class ImportMapModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ImportMapModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<string> HeadersArchivo { get; set; } = new();
        public List<Dictionary<string, string>> FilasArchivo { get; set; } = new();
        public Dictionary<string, string?> MapaSugerido { get; set; } = new();
        public List<CampoMapeo> Campos { get; set; } = new();

        // Campos que el usuario puede mapear
        public class CampoMapeo
        {
            public string Key { get; set; } = "";
            public string Label { get; set; } = "";
            public bool Requerido { get; set; }
        }

        // Alias de cada campo (para autodetección)
        private static readonly Dictionary<string, string[]> AliasCampos = new()
        {
            { "Codigo",       new[] { "codigo", "code", "cod", "clave", "id_cliente" } },
            { "Nombre",       new[] { "nombre", "name", "cliente", "razon", "razonsocial", "razon_social" } },
            { "RazonSocial",  new[] { "razonsocial", "razon_social", "nombrelegal" } },
            { "RTN",          new[] { "rtn", "ruc", "nit", "taxid", "identificacion" } },
            { "Email",        new[] { "email", "correo", "mail", "e-mail" } },
            { "Telefono",     new[] { "telefono", "tel", "phone", "celular", "movil" } },
            { "Direccion",    new[] { "direccion", "address", "domicilio" } },
            { "Ciudad",       new[] { "ciudad", "city", "municipio" } },
            { "Pais",         new[] { "pais", "country" } },
            { "TipoCliente",  new[] { "tipocliente", "tipo", "categoria", "type" } },
            { "DiasCredito",  new[] { "diascredito", "dias", "plazo", "creditodias" } },
            { "LimiteCredito",new[] { "limitecredito", "limite", "creditlimit" } },
            { "Notas",        new[] { "notas", "notes", "observaciones", "comentarios" } },
            { "Activo",       new[] { "activo", "active", "estado", "status" } },
        };

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Ventas", "ClientesImport", "crear"))
            {
                TempData["Error"] = "No tienes permiso para importar clientes";
                return RedirectToPage("/Clientes/Index");
            }

            // Recuperar datos de la sesión
            var jsonHeaders = HttpContext.Session.GetString("ImportClientes_Headers");
            var jsonRows = HttpContext.Session.GetString("ImportClientes_Rows");

            if (string.IsNullOrEmpty(jsonHeaders) || string.IsNullOrEmpty(jsonRows))
            {
                TempData["Error"] = "La sesión expiró. Vuelve a subir el archivo.";
                return RedirectToPage("/Clientes/Import");
            }

            HeadersArchivo = JsonSerializer.Deserialize<List<string>>(jsonHeaders) ?? new();
            FilasArchivo = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(jsonRows) ?? new();

            // Autodetectar mapa
            MapaSugerido = ColumnMapper.Detectar(HeadersArchivo, AliasCampos);

            // Definir campos editables
            Campos = new List<CampoMapeo>
            {
                new() { Key = "Codigo",        Label = "Código",           Requerido = false },
                new() { Key = "Nombre",        Label = "Nombre",           Requerido = true },
                new() { Key = "RazonSocial",   Label = "Razón social",     Requerido = false },
                new() { Key = "RTN",           Label = "RTN",              Requerido = false },
                new() { Key = "Email",         Label = "Email",            Requerido = false },
                new() { Key = "Telefono",      Label = "Teléfono",         Requerido = false },
                new() { Key = "Direccion",     Label = "Dirección",        Requerido = false },
                new() { Key = "Ciudad",        Label = "Ciudad",           Requerido = false },
                new() { Key = "Pais",          Label = "País",             Requerido = false },
                new() { Key = "TipoCliente",   Label = "Tipo de cliente",  Requerido = false },
                new() { Key = "DiasCredito",   Label = "Días de crédito",  Requerido = false },
                new() { Key = "LimiteCredito", Label = "Límite de crédito",Requerido = false },
                new() { Key = "Notas",         Label = "Notas",            Requerido = false },
                new() { Key = "Activo",        Label = "Activo (Sí/No)",   Requerido = false },
            };

            return Page();
        }

        [BindProperty]
        public Dictionary<string, string> Mapeo { get; set; } = new();

        [BindProperty]
        public string ModoImportacion { get; set; } = "upsert";

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Ventas", "ClientesImport", "crear"))
            {
                TempData["Error"] = "No tienes permiso para importar clientes";
                return RedirectToPage("/Clientes/Index");
            }

            // Recuperar datos
            var jsonHeaders = HttpContext.Session.GetString("ImportClientes_Headers");
            var jsonRows = HttpContext.Session.GetString("ImportClientes_Rows");

            if (string.IsNullOrEmpty(jsonHeaders) || string.IsNullOrEmpty(jsonRows))
            {
                TempData["Error"] = "La sesión expiró. Vuelve a subir el archivo.";
                return RedirectToPage("/Clientes/Import");
            }

            var headers = JsonSerializer.Deserialize<List<string>>(jsonHeaders) ?? new();
            var filas = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(jsonRows) ?? new();

            // Validar mapeo mínimo
            if (string.IsNullOrEmpty(Mapeo.GetValueOrDefault("Nombre")))
            {
                TempData["Error"] = "Debes mapear al menos el campo 'Nombre'";
                return RedirectToPage("/Clientes/ImportMap");
            }

            // Procesar importación
            var resultado = ImportarClientes(filas, Mapeo, ModoImportacion, currentUser?.Id);

            // Registrar actividad
            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Importar clientes",
                $"Importó clientes: {resultado.Resumen()}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            // Limpiar sesión
            HttpContext.Session.Remove("ImportClientes_Headers");
            HttpContext.Session.Remove("ImportClientes_Rows");

            // Guardar resultado temporal para mostrarlo en el Index
            TempData["ImportResultado"] = JsonSerializer.Serialize(resultado);
            TempData["Success"] = $"Importación completada: {resultado.Resumen()}";
            return RedirectToPage("/Clientes/Index");
        }

        private ImportResult ImportarClientes(
            List<Dictionary<string, string>> filas,
            Dictionary<string, string> mapa,
            string modo,
            int? usuarioId)
        {
            var resultado = new ImportResult { TotalFilas = filas.Count };
            var codigosExistentes = _context.Clientes
                .Where(c => c.Codigo != null)
                .Select(c => c.Codigo!)
                .ToHashSet();

            var rtnExistentes = _context.Clientes
                .Where(c => c.RTN != null)
                .Select(c => c.RTN!)
                .ToHashSet();

            int filaNum = 1;
            foreach (var fila in filas)
            {
                filaNum++;
                try
                {
                    // Extraer valores según mapeo
                    string? Obtener(string campo)
                    {
                        var header = mapa.GetValueOrDefault(campo);
                        if (string.IsNullOrEmpty(header)) return null;
                        return fila.GetValueOrDefault(header)?.Trim();
                    }

                    var nombre = Obtener("Nombre");
                    if (string.IsNullOrWhiteSpace(nombre))
                    {
                        resultado.AgregarError(filaNum, "El campo 'Nombre' es obligatorio");
                        continue;
                    }

                    var codigo = Obtener("Codigo");
                    var rtn = Obtener("RTN");

                    // Buscar existente (por Código o RTN)
                    Cliente? existente = null;
                    if (!string.IsNullOrEmpty(codigo))
                        existente = _context.Clientes.FirstOrDefault(c => c.Codigo == codigo);
                    if (existente == null && !string.IsNullOrEmpty(rtn))
                        existente = _context.Clientes.FirstOrDefault(c => c.RTN == rtn);

                    if (existente != null && modo == "crear")
                    {
                        resultado.Ignorados++;
                        continue;
                    }

                    if (existente == null && modo == "actualizar")
                    {
                        resultado.AgregarAdvertencia(filaNum, $"Cliente '{nombre}' no existe, se ignora (modo: solo actualizar)");
                        resultado.Ignorados++;
                        continue;
                    }

                    // Autogenerar código si no viene
                    if (string.IsNullOrEmpty(codigo) && existente == null)
                    {
                        codigo = NumeroDocumentoHelper.GenerarSiguiente(_context, "Cliente");
                    }

                    var activo = ParsearBool(Obtener("Activo"));
                    var diasCredito = ParsearInt(Obtener("DiasCredito"));
                    var limiteCredito = ParsearDecimal(Obtener("LimiteCredito"));

                    if (existente != null)
                    {
                        // Actualizar
                        existente.Nombre = nombre;
                        existente.RazonSocial = Obtener("RazonSocial") ?? existente.RazonSocial;
                        existente.RTN = rtn ?? existente.RTN;
                        existente.Email = Obtener("Email") ?? existente.Email;
                        existente.Telefono = Obtener("Telefono") ?? existente.Telefono;
                        existente.Direccion = Obtener("Direccion") ?? existente.Direccion;
                        existente.Ciudad = Obtener("Ciudad") ?? existente.Ciudad;
                        existente.Pais = Obtener("Pais") ?? existente.Pais;
                        existente.TipoCliente = Obtener("TipoCliente") ?? existente.TipoCliente;
                        existente.DiasCredito = diasCredito ?? existente.DiasCredito;
                        existente.LimiteCredito = limiteCredito ?? existente.LimiteCredito;
                        existente.Notas = Obtener("Notas") ?? existente.Notas;
                        existente.Activo = activo ?? existente.Activo;
                        resultado.Actualizados++;
                    }
                    else
                    {
                        // Crear
                        var cliente = new Cliente
                        {
                            Codigo = codigo,
                            Nombre = nombre,
                            RazonSocial = Obtener("RazonSocial"),
                            RTN = rtn,
                            Email = Obtener("Email"),
                            Telefono = Obtener("Telefono"),
                            Direccion = Obtener("Direccion"),
                            Ciudad = Obtener("Ciudad"),
                            Pais = Obtener("Pais") ?? "Honduras",
                            TipoCliente = Obtener("TipoCliente") ?? "Regular",
                            DiasCredito = diasCredito ?? 0,
                            LimiteCredito = limiteCredito ?? 0,
                            Notas = Obtener("Notas"),
                            Activo = activo ?? true,
                            FechaCreacion = DateTime.Now,
                            UsuarioCreoId = usuarioId
                        };

                        _context.Clientes.Add(cliente);
                        resultado.Creados++;
                    }
                }
                catch (Exception ex)
                {
                    resultado.AgregarError(filaNum, $"Error inesperado: {ex.Message}");
                }
            }

            _context.SaveChanges();
            return resultado;
        }

        private static bool? ParsearBool(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;

            var normalizado = valor.Trim().ToLowerInvariant();
            if (normalizado == "1" || normalizado == "true" || normalizado == "sí" || normalizado == "si" ||
                normalizado == "yes" || normalizado == "y" || normalizado == "activo" || normalizado == "activa")
                return true;
            if (normalizado == "0" || normalizado == "false" || normalizado == "no" ||
                normalizado == "n" || normalizado == "inactivo" || normalizado == "inactiva")
                return false;

            return null;
        }

        private static int? ParsearInt(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;
            return int.TryParse(valor.Trim(), out var result) ? result : null;
        }

        private static decimal? ParsearDecimal(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;
            var limpio = valor.Trim().Replace(",", "").Replace("L.", "").Replace("$", "").Trim();
            return decimal.TryParse(limpio, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : null;
        }
    }
}