using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Import;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Proveedores
{
    public class ImportMapModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ImportMapModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public ImportWizardConfig WizardConfig { get; set; } = new();
        public List<string> HeadersArchivo { get; set; } = new();
        public List<Dictionary<string, string>> FilasArchivo { get; set; } = new();
        public Dictionary<string, string?> MapaSugerido { get; set; } = new();

        [BindProperty]
        public Dictionary<string, string> Mapeo { get; set; } = new();

        [BindProperty]
        public string ModoImportacion { get; set; } = "upsert";

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            WizardConfig = BuildConfig();

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, WizardConfig.ModuloPermiso, WizardConfig.SubmoduloPermiso, "crear"))
            {
                TempData["Error"] = $"No tienes permiso para importar {WizardConfig.NombrePlural}";
                return Redirect(WizardConfig.UrlIndex);
            }

            if (!CargarDesdeSesion())
            {
                TempData["Error"] = "La sesión expiró. Vuelve a subir el archivo.";
                return Redirect(WizardConfig.UrlImport);
            }

            MapaSugerido = ColumnMapper.Detectar(HeadersArchivo, WizardConfig.AliasCampos);

            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            WizardConfig = BuildConfig();

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, WizardConfig.ModuloPermiso, WizardConfig.SubmoduloPermiso, "crear"))
            {
                TempData["Error"] = $"No tienes permiso para importar {WizardConfig.NombrePlural}";
                return Redirect(WizardConfig.UrlIndex);
            }

            if (!CargarDesdeSesion())
            {
                TempData["Error"] = "La sesión expiró. Vuelve a subir el archivo.";
                return Redirect(WizardConfig.UrlImport);
            }

            var faltantes = WizardConfig.CamposRequeridos
                .Where(campo => string.IsNullOrEmpty(Mapeo.GetValueOrDefault(campo)))
                .ToList();

            if (faltantes.Count > 0)
            {
                var labelsFaltantes = WizardConfig.Campos
                    .Where(c => faltantes.Contains(c.Key))
                    .Select(c => c.Label);

                TempData["Error"] = $"Debes mapear: {string.Join(", ", labelsFaltantes)}";
                return Redirect(WizardConfig.UrlImportMap);
            }

            var resultado = ImportarProveedores(FilasArchivo, Mapeo, ModoImportacion, currentUser?.Id);

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                $"Importar {WizardConfig.NombrePlural}",
                $"Importó {WizardConfig.NombrePlural}: {resultado.Resumen()}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            ImportWizardHelper.LimpiarSesion(HttpContext.Session, WizardConfig);

            TempData["Success"] = $"Importación completada: {resultado.Resumen()}";
            return Redirect(WizardConfig.UrlIndex);
        }

        private bool CargarDesdeSesion()
        {
            var data = ImportWizardHelper.RecuperarDeSesion(HttpContext.Session, WizardConfig);
            if (data == null) return false;

            HeadersArchivo = data.Value.Headers;
            FilasArchivo = data.Value.Rows;
            return true;
        }

        private static ImportWizardConfig BuildConfig()
        {
            return new ImportWizardConfig
            {
                NombrePlural = "proveedores",
                NombreSingular = "proveedor",
                ModuloPermiso = "Compras",
                SubmoduloPermiso = "ProveedoresImport",
                SessionKey = "Proveedores",
                UrlIndex = "/Proveedores/Index",
                UrlImport = "/Proveedores/Import",
                UrlImportMap = "/Proveedores/ImportMap",
                CamposRequeridos = new[] { "Nombre" },
                Campos = new List<CampoMapeo>
                {
                    new() { Key = "Codigo",           Label = "Código",                Requerido = false },
                    new() { Key = "Nombre",           Label = "Nombre",                Requerido = true },
                    new() { Key = "RazonSocial",      Label = "Razón social",          Requerido = false },
                    new() { Key = "RTN",              Label = "RTN",                   Requerido = false },
                    new() { Key = "Contacto",         Label = "Persona de contacto",   Requerido = false },
                    new() { Key = "TelefonoContacto", Label = "Teléfono del contacto", Requerido = false },
                    new() { Key = "Telefono",         Label = "Teléfono principal",    Requerido = false },
                    new() { Key = "Email",            Label = "Email",                 Requerido = false },
                    new() { Key = "Direccion",        Label = "Dirección",             Requerido = false },
                    new() { Key = "Ciudad",           Label = "Ciudad",                Requerido = false },
                    new() { Key = "Pais",             Label = "País",                  Requerido = false },
                    new() { Key = "CondicionPago",    Label = "Condición de pago",     Requerido = false },
                    new() { Key = "DiasCredito",      Label = "Días de crédito",       Requerido = false },
                    new() { Key = "LimiteCredito",    Label = "Límite de crédito",     Requerido = false },
                    new() { Key = "Banco",            Label = "Banco",                 Requerido = false },
                    new() { Key = "CuentaBancaria",   Label = "Cuenta bancaria",       Requerido = false },
                    new() { Key = "Notas",            Label = "Notas",                 Requerido = false },
                    new() { Key = "Activo",           Label = "Activo (Sí/No)",        Requerido = false },
                },
                AliasCampos = new Dictionary<string, string[]>
                {
                    { "Codigo",           new[] { "codigo", "code", "cod", "clave" } },
                    { "Nombre",           new[] { "nombre", "name", "proveedor", "razon", "razonsocial" } },
                    { "RazonSocial",      new[] { "razonsocial", "razon_social", "nombrelegal" } },
                    { "RTN",              new[] { "rtn", "ruc", "nit", "taxid" } },
                    { "Contacto",         new[] { "contacto", "contact", "vendedor", "representante" } },
                    { "TelefonoContacto", new[] { "telefonocontacto", "telcontacto", "celularcontacto" } },
                    { "Telefono",         new[] { "telefono", "tel", "phone", "celular", "movil" } },
                    { "Email",            new[] { "email", "correo", "mail", "e-mail" } },
                    { "Direccion",        new[] { "direccion", "address", "domicilio" } },
                    { "Ciudad",           new[] { "ciudad", "city", "municipio" } },
                    { "Pais",             new[] { "pais", "country" } },
                    { "CondicionPago",    new[] { "condicionpago", "condicion", "formapago", "plazopago" } },
                    { "DiasCredito",      new[] { "diascredito", "dias", "plazo" } },
                    { "LimiteCredito",    new[] { "limitecredito", "limite" } },
                    { "Banco",            new[] { "banco", "bank" } },
                    { "CuentaBancaria",   new[] { "cuentabancaria", "cuenta", "ncuenta", "numerocuenta" } },
                    { "Notas",            new[] { "notas", "notes", "observaciones", "comentarios" } },
                    { "Activo",           new[] { "activo", "active", "estado", "status" } },
                }
            };
        }

        private ImportResult ImportarProveedores(
            List<Dictionary<string, string>> filas,
            Dictionary<string, string> mapa,
            string modo,
            int? usuarioId)
        {
            var resultado = new ImportResult { TotalFilas = filas.Count };

            // ===== 1. Precargar códigos y RTNs existentes =====
            var codigosExistentes = _context.Proveedores
                .Where(p => p.Codigo != null)
                .Select(p => p.Codigo!)
                .ToHashSet();

            var rtnsExistentes = _context.Proveedores
                .Where(p => p.RTN != null)
                .Select(p => p.RTN!)
                .ToHashSet();

            // ===== 2. Serie y próximo número local =====
            var serieProveedor = _context.SeriesDocumentos
                .FirstOrDefault(s => s.Tipo == "Proveedor" && s.EsPredeterminada && s.Activa);

            string prefijoSerie = serieProveedor?.Prefijo ?? "PROV";
            string sepSerie = serieProveedor?.Separador ?? "-";
            int longitudSerie = serieProveedor?.LongitudNumero ?? 4;
            string? formatoSerie = serieProveedor?.FormatoPersonalizado;

            int proximoNumeroLocal = serieProveedor?.SiguienteNumero ?? (codigosExistentes.Count + 1);

            while (codigosExistentes.Contains(
                FormatearCodigo(prefijoSerie, sepSerie, longitudSerie, proximoNumeroLocal, formatoSerie)))
            {
                proximoNumeroLocal++;
            }

            var monedaDefecto = _context.Monedas.FirstOrDefault(m => m.EsPredeterminada)?.Id ?? 1;

            // ===== 3. Procesar filas =====
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

                    if (nombre.Length > 150)
                    {
                        resultado.AgregarError(filaNum, $"El nombre excede 150 caracteres");
                        continue;
                    }

                    var codigo = Obtener("Codigo");
                    var rtn = Obtener("RTN");

                    // Validar longitud de código
                    if (!string.IsNullOrEmpty(codigo) && codigo.Length > 20)
                    {
                        resultado.AgregarAdvertencia(filaNum,
                            $"El código '{codigo}' excede 20 caracteres. Se ignora y se autogenera.");
                        codigo = null;
                    }

                    if (!string.IsNullOrEmpty(rtn) && rtn.Length > 20)
                    {
                        resultado.AgregarAdvertencia(filaNum, $"El RTN excede 20 caracteres. Se ignora.");
                        rtn = null;
                    }

                    // Buscar existente
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

                    // Autogenerar código
                    if (string.IsNullOrEmpty(codigo) && existente == null)
                    {
                        do
                        {
                            codigo = FormatearCodigo(prefijoSerie, sepSerie, longitudSerie, proximoNumeroLocal, formatoSerie);
                            proximoNumeroLocal++;
                        }
                        while (codigosExistentes.Contains(codigo));

                        codigosExistentes.Add(codigo);
                    }

                    var activo = ParserHelper.ParsearBool(Obtener("Activo"));
                    var condicion = Obtener("CondicionPago") ?? "Contado";
                    var diasCredito = ParserHelper.ParsearInt(Obtener("DiasCredito"));
                    var limiteCredito = ParserHelper.ParsearDecimal(Obtener("LimiteCredito"));

                    if (existente != null)
                    {
                        existente.Nombre = nombre;
                        existente.RazonSocial = Truncar(Obtener("RazonSocial"), 200) ?? existente.RazonSocial;
                        existente.RTN = rtn ?? existente.RTN;
                        existente.Contacto = Truncar(Obtener("Contacto"), 100) ?? existente.Contacto;
                        existente.TelefonoContacto = Truncar(Obtener("TelefonoContacto"), 30) ?? existente.TelefonoContacto;
                        existente.Telefono = Truncar(Obtener("Telefono"), 30) ?? existente.Telefono;
                        existente.Email = Truncar(Obtener("Email"), 150) ?? existente.Email;
                        existente.Direccion = Truncar(Obtener("Direccion"), 300) ?? existente.Direccion;
                        existente.Ciudad = Truncar(Obtener("Ciudad"), 100) ?? existente.Ciudad;
                        existente.Pais = Truncar(Obtener("Pais"), 80) ?? existente.Pais;
                        existente.CondicionPago = Truncar(condicion, 20) ?? existente.CondicionPago;
                        existente.DiasCredito = diasCredito ?? existente.DiasCredito;
                        existente.LimiteCredito = limiteCredito ?? existente.LimiteCredito;
                        existente.Banco = Truncar(Obtener("Banco"), 100) ?? existente.Banco;
                        existente.CuentaBancaria = Truncar(Obtener("CuentaBancaria"), 50) ?? existente.CuentaBancaria;
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
                            RazonSocial = Truncar(Obtener("RazonSocial"), 200),
                            RTN = rtn,
                            Contacto = Truncar(Obtener("Contacto"), 100),
                            TelefonoContacto = Truncar(Obtener("TelefonoContacto"), 30),
                            Telefono = Truncar(Obtener("Telefono"), 30),
                            Email = Truncar(Obtener("Email"), 150),
                            Direccion = Truncar(Obtener("Direccion"), 300),
                            Ciudad = Truncar(Obtener("Ciudad"), 100),
                            Pais = Truncar(Obtener("Pais"), 80) ?? "Honduras",
                            CondicionPago = Truncar(condicion, 20) ?? "Contado",
                            DiasCredito = diasCredito ?? 0,
                            LimiteCredito = limiteCredito ?? 0,
                            Banco = Truncar(Obtener("Banco"), 100),
                            CuentaBancaria = Truncar(Obtener("CuentaBancaria"), 50),
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

            if (serieProveedor != null && resultado.Creados > 0)
            {
                serieProveedor.SiguienteNumero = proximoNumeroLocal;
                _context.SaveChanges();
            }

            return resultado;
        }

        private static string FormatearCodigo(
            string prefijo, string separador, int longitud, int numero, string? formatoPersonalizado)
        {
            var numeroStr = numero.ToString().PadLeft(longitud, '0');
            var anio = DateTime.Now.Year.ToString();
            var mes = DateTime.Now.Month.ToString("D2");
            var dia = DateTime.Now.Day.ToString("D2");

            string resultado;

            if (!string.IsNullOrWhiteSpace(formatoPersonalizado))
            {
                resultado = formatoPersonalizado
                    .Replace("{PREFIX}", prefijo)
                    .Replace("{PREFIJO}", prefijo)
                    .Replace("{SUFFIX}", "")
                    .Replace("{SUFIJO}", "")
                    .Replace("{SEP}", separador)
                    .Replace("{YEAR}", anio)
                    .Replace("{ANIO}", anio)
                    .Replace("{MONTH}", mes)
                    .Replace("{MES}", mes)
                    .Replace("{DAY}", dia)
                    .Replace("{DIA}", dia)
                    .Replace("{NUM}", numeroStr);
            }
            else
            {
                resultado = $"{prefijo}{separador}{numeroStr}";
            }

            if (resultado.Length > 20)
                resultado = resultado.Substring(0, 20);

            return resultado;
        }

        private static string? Truncar(string? valor, int maximo)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;
            valor = valor.Trim();
            return valor.Length > maximo ? valor.Substring(0, maximo) : valor;
        }
    }
}