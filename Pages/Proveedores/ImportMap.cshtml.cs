using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Import;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace Kirkenta.Pages.Proveedores
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

        public class CampoMapeo
        {
            public string Key { get; set; } = "";
            public string Label { get; set; } = "";
            public bool Requerido { get; set; }
        }

        private static readonly Dictionary<string, string[]> AliasCampos = new()
        {
            { "Codigo",          new[] { "codigo", "code", "cod", "clave" } },
            { "Nombre",          new[] { "nombre", "name", "proveedor", "razon", "razonsocial" } },
            { "RazonSocial",     new[] { "razonsocial", "razon_social", "nombrelegal" } },
            { "RTN",             new[] { "rtn", "ruc", "nit", "taxid" } },
            { "Contacto",        new[] { "contacto", "contact", "vendedor", "representante" } },
            { "TelefonoContacto",new[] { "telefonocontacto", "telcontacto", "celularcontacto" } },
            { "Telefono",        new[] { "telefono", "tel", "phone", "celular", "movil" } },
            { "Email",           new[] { "email", "correo", "mail", "e-mail" } },
            { "Direccion",       new[] { "direccion", "address", "domicilio" } },
            { "Ciudad",          new[] { "ciudad", "city", "municipio" } },
            { "Pais",            new[] { "pais", "country" } },
            { "CondicionPago",   new[] { "condicionpago", "condicion", "formapago", "plazopago" } },
            { "DiasCredito",     new[] { "diascredito", "dias", "plazo" } },
            { "LimiteCredito",   new[] { "limitecredito", "limite" } },
            { "Banco",           new[] { "banco", "bank" } },
            { "CuentaBancaria",  new[] { "cuentabancaria", "cuenta", "ncuenta", "numerocuenta" } },
            { "Notas",           new[] { "notas", "notes", "observaciones", "comentarios" } },
            { "Activo",          new[] { "activo", "active", "estado", "status" } },
        };

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Compras", "ProveedoresImport", "crear"))
            {
                TempData["Error"] = "No tienes permiso para importar proveedores";
                return RedirectToPage("/Proveedores/Index");
            }

            var jsonHeaders = HttpContext.Session.GetString("ImportProveedores_Headers");
            var jsonRows = HttpContext.Session.GetString("ImportProveedores_Rows");

            if (string.IsNullOrEmpty(jsonHeaders) || string.IsNullOrEmpty(jsonRows))
            {
                TempData["Error"] = "La sesión expiró. Vuelve a subir el archivo.";
                return RedirectToPage("/Proveedores/Import");
            }

            HeadersArchivo = JsonSerializer.Deserialize<List<string>>(jsonHeaders) ?? new();
            FilasArchivo = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(jsonRows) ?? new();

            MapaSugerido = ColumnMapper.Detectar(HeadersArchivo, AliasCampos);

            Campos = new List<CampoMapeo>
            {
                new() { Key = "Codigo",           Label = "Código",             Requerido = false },
                new() { Key = "Nombre",           Label = "Nombre",             Requerido = true },
                new() { Key = "RazonSocial",      Label = "Razón social",       Requerido = false },
                new() { Key = "RTN",              Label = "RTN",                Requerido = false },
                new() { Key = "Contacto",         Label = "Persona de contacto",Requerido = false },
                new() { Key = "TelefonoContacto", Label = "Teléfono del contacto",Requerido = false },
                new() { Key = "Telefono",         Label = "Teléfono principal", Requerido = false },
                new() { Key = "Email",            Label = "Email",              Requerido = false },
                new() { Key = "Direccion",        Label = "Dirección",          Requerido = false },
                new() { Key = "Ciudad",           Label = "Ciudad",             Requerido = false },
                new() { Key = "Pais",             Label = "País",               Requerido = false },
                new() { Key = "CondicionPago",    Label = "Condición de pago",  Requerido = false },
                new() { Key = "DiasCredito",      Label = "Días de crédito",    Requerido = false },
                new() { Key = "LimiteCredito",    Label = "Límite de crédito",  Requerido = false },
                new() { Key = "Banco",            Label = "Banco",              Requerido = false },
                new() { Key = "CuentaBancaria",   Label = "Cuenta bancaria",    Requerido = false },
                new() { Key = "Notas",            Label = "Notas",              Requerido = false },
                new() { Key = "Activo",           Label = "Activo (Sí/No)",     Requerido = false },
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
                !PermisoHelper.TienePermiso(_context, currentRol, "Compras", "ProveedoresImport", "crear"))
            {
                TempData["Error"] = "No tienes permiso para importar proveedores";
                return RedirectToPage("/Proveedores/Index");
            }

            var jsonHeaders = HttpContext.Session.GetString("ImportProveedores_Headers");
            var jsonRows = HttpContext.Session.GetString("ImportProveedores_Rows");

            if (string.IsNullOrEmpty(jsonHeaders) || string.IsNullOrEmpty(jsonRows))
            {
                TempData["Error"] = "La sesión expiró. Vuelve a subir el archivo.";
                return RedirectToPage("/Proveedores/Import");
            }

            var filas = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(jsonRows) ?? new();

            if (string.IsNullOrEmpty(Mapeo.GetValueOrDefault("Nombre")))
            {
                TempData["Error"] = "Debes mapear al menos el campo 'Nombre'";
                return RedirectToPage("/Proveedores/ImportMap");
            }

            var resultado = ImportarProveedores(filas, Mapeo, ModoImportacion, currentUser?.Id);

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Importar proveedores",
                $"Importó proveedores: {resultado.Resumen()}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            HttpContext.Session.Remove("ImportProveedores_Headers");
            HttpContext.Session.Remove("ImportProveedores_Rows");

            TempData["Success"] = $"Importación completada: {resultado.Resumen()}";
            return RedirectToPage("/Proveedores/Index");
        }

        private ImportResult ImportarProveedores(
            List<Dictionary<string, string>> filas,
            Dictionary<string, string> mapa,
            string modo,
            int? usuarioId)
        {
            var resultado = new ImportResult { TotalFilas = filas.Count };
            var monedaDefecto = _context.Monedas.FirstOrDefault(m => m.EsPredeterminada)?.Id ?? 1;

            int filaNum = 1;
            foreach (var fila in filas)
            {
                filaNum++;
                try
                {
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

                    Proveedor? existente = null;
                    if (!string.IsNullOrEmpty(codigo))
                        existente = _context.Proveedores.FirstOrDefault(p => p.Codigo == codigo);
                    if (existente == null && !string.IsNullOrEmpty(rtn))
                        existente = _context.Proveedores.FirstOrDefault(p => p.RTN == rtn);

                    if (existente != null && modo == "crear")
                    {
                        resultado.Ignorados++;
                        continue;
                    }

                    if (existente == null && modo == "actualizar")
                    {
                        resultado.AgregarAdvertencia(filaNum, $"Proveedor '{nombre}' no existe, se ignora");
                        resultado.Ignorados++;
                        continue;
                    }

                    if (string.IsNullOrEmpty(codigo) && existente == null)
                    {
                        codigo = NumeroDocumentoHelper.GenerarSiguiente(_context, "Proveedor");
                    }

                    var activo = ParsearBool(Obtener("Activo"));
                    var condicion = Obtener("CondicionPago") ?? "Contado";
                    var diasCredito = ParsearInt(Obtener("DiasCredito"));
                    var limiteCredito = ParsearDecimal(Obtener("LimiteCredito"));

                    if (existente != null)
                    {
                        existente.Nombre = nombre;
                        existente.RazonSocial = Obtener("RazonSocial") ?? existente.RazonSocial;
                        existente.RTN = rtn ?? existente.RTN;
                        existente.Contacto = Obtener("Contacto") ?? existente.Contacto;
                        existente.TelefonoContacto = Obtener("TelefonoContacto") ?? existente.TelefonoContacto;
                        existente.Telefono = Obtener("Telefono") ?? existente.Telefono;
                        existente.Email = Obtener("Email") ?? existente.Email;
                        existente.Direccion = Obtener("Direccion") ?? existente.Direccion;
                        existente.Ciudad = Obtener("Ciudad") ?? existente.Ciudad;
                        existente.Pais = Obtener("Pais") ?? existente.Pais;
                        existente.CondicionPago = condicion;
                        existente.DiasCredito = diasCredito ?? existente.DiasCredito;
                        existente.LimiteCredito = limiteCredito ?? existente.LimiteCredito;
                        existente.Banco = Obtener("Banco") ?? existente.Banco;
                        existente.CuentaBancaria = Obtener("CuentaBancaria") ?? existente.CuentaBancaria;
                        existente.Notas = Obtener("Notas") ?? existente.Notas;
                        existente.Activo = activo ?? existente.Activo;
                        resultado.Actualizados++;
                    }
                    else
                    {
                        var proveedor = new Proveedor
                        {
                            Codigo = codigo,
                            Nombre = nombre,
                            RazonSocial = Obtener("RazonSocial"),
                            RTN = rtn,
                            Contacto = Obtener("Contacto"),
                            TelefonoContacto = Obtener("TelefonoContacto"),
                            Telefono = Obtener("Telefono"),
                            Email = Obtener("Email"),
                            Direccion = Obtener("Direccion"),
                            Ciudad = Obtener("Ciudad"),
                            Pais = Obtener("Pais") ?? "Honduras",
                            CondicionPago = condicion,
                            DiasCredito = diasCredito ?? 0,
                            LimiteCredito = limiteCredito ?? 0,
                            Banco = Obtener("Banco"),
                            CuentaBancaria = Obtener("CuentaBancaria"),
                            MonedaId = monedaDefecto,
                            Notas = Obtener("Notas"),
                            Activo = activo ?? true,
                            FechaCreacion = DateTime.Now,
                            UsuarioCreoId = usuarioId,
                            EmpresaId = 1
                        };

                        _context.Proveedores.Add(proveedor);
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