using System.Globalization;
using System.Text.RegularExpressions;
using Kirkenta.Data;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Helpers.Import;
using Kirkenta.Helpers.RRHH;
using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;
using static Kirkenta.Helpers.Migracion.MigracionHistorialHelper;

namespace Kirkenta.Helpers.Migracion
{
    /// <summary>
    /// Migración del resto de la operación de un sistema anterior:
    /// catálogos (plan de cuentas, métodos de pago, cajas y bancos con su saldo de arranque, empleados) e
    /// historiales (compras con sus líneas, cotizaciones, ingresos y egresos). Todo lo que crea queda anotado
    /// en RegistrosMigracion con su lote para poder deshacerlo. Nada de lo histórico mueve saldos ni stock:
    /// el saldo real de las cuentas entra por su apertura y la existencia por el inventario inicial.
    /// </summary>
    public static class MigracionOperacionesHelper
    {
        public const string TipoCompras = "compras";
        public const string TipoCotizaciones = "cotizaciones";
        public const string TipoMovimientos = "movimientos";
        public const string TipoSaldosContables = "saldoscontables";
        public const string OrigenMigracion = "Migracion";

        public static readonly string[] Catalogos = { "PlanCuentas", "MetodosPago", "CuentasFinancieras", "Empleados" };
        public static readonly string[] Historiales = { TipoCompras, TipoCotizaciones, TipoMovimientos, TipoSaldosContables };

        public static bool EsCatalogo(string key) => Catalogos.Contains(key);
        public static bool EsHistorial(string tipo) => Historiales.Contains(tipo);

        public static string Nombre(string key) => key switch
        {
            "PlanCuentas" => "Plan de cuentas contable",
            "MetodosPago" => "Métodos de pago",
            "CuentasFinancieras" => "Cajas y bancos (con su saldo de arranque)",
            "Empleados" => "Empleados",
            TipoCompras => "Historial de compras",
            TipoCotizaciones => "Cotizaciones",
            TipoMovimientos => "Ingresos y egresos históricos",
            TipoSaldosContables => "Balance de apertura (saldos contables)",
            _ => key
        };

        // ================= Campos =================
        public static List<CampoDef> Campos(string key) => key switch
        {
            "PlanCuentas" => new()
            {
                new() { Key = "Codigo", Label = "Código de la cuenta", Requerido = true, Alias = new[] { "codigo", "cuenta", "codigocuenta", "numerocuenta", "code", "account", "nrocuenta" } },
                new() { Key = "Nombre", Label = "Nombre", Requerido = true, Alias = new[] { "nombre", "nombrecuenta", "descripcion", "name", "detalle" } },
                new() { Key = "Tipo", Label = "Tipo (activo, pasivo, ingreso…)", Alias = new[] { "tipo", "clase", "tipocuenta", "grupo", "type", "rubro" } },
                new() { Key = "Naturaleza", Label = "Naturaleza (deudora / acreedora)", Alias = new[] { "naturaleza", "saldonormal", "nature" } },
                new() { Key = "Padre", Label = "Cuenta padre", Alias = new[] { "padre", "cuentapadre", "codigopadre", "parent", "mayor" } },
                new() { Key = "Movimiento", Label = "Acepta movimientos", Alias = new[] { "movimiento", "esmovimiento", "detalle", "auxiliar", "imputable", "posteable" } },
                new() { Key = "Activa", Label = "Activa", Alias = new[] { "activa", "activo", "estado", "status" } },
            },
            "MetodosPago" => new()
            {
                new() { Key = "Nombre", Label = "Nombre", Requerido = true, Alias = new[] { "nombre", "metodo", "metodopago", "formapago", "forma", "descripcion", "name" } },
                new() { Key = "Tipo", Label = "Tipo", Alias = new[] { "tipo", "type", "clase" } },
                new() { Key = "Activo", Label = "Activo", Alias = new[] { "activo", "active", "estado", "status" } },
            },
            "CuentasFinancieras" => new()
            {
                new() { Key = "Codigo", Label = "Código", Alias = new[] { "codigo", "code", "clave", "idcuenta" } },
                new() { Key = "Nombre", Label = "Nombre", Requerido = true, Alias = new[] { "nombre", "cuenta", "nombrecuenta", "descripcion", "name", "caja" } },
                new() { Key = "Tipo", Label = "Tipo (caja / banco)", Alias = new[] { "tipo", "tipocuenta", "clase", "type" } },
                new() { Key = "Banco", Label = "Banco", Alias = new[] { "banco", "bank", "entidad", "institucion" } },
                new() { Key = "NumeroCuenta", Label = "Número de cuenta", Alias = new[] { "numerocuenta", "nrocuenta", "nocuenta", "cuentabancaria", "accountnumber", "iban" } },
                new() { Key = "Saldo", Label = "Saldo actual (arranque)", Alias = new[] { "saldo", "saldoactual", "saldoinicial", "balance", "disponible", "monto", "efectivo" } },
                new() { Key = "Responsable", Label = "Responsable", Alias = new[] { "responsable", "cajero", "encargado" } },
                new() { Key = "Activa", Label = "Activa", Alias = new[] { "activa", "activo", "estado", "status" } },
            },
            "Empleados" => new()
            {
                new() { Key = "Codigo", Label = "Código / ficha", Alias = new[] { "codigo", "codigoempleado", "idempleado", "ficha", "numeroempleado", "noempleado", "legajo" } },
                new() { Key = "Nombres", Label = "Nombres", Requerido = true, Alias = new[] { "nombres", "nombre", "primernombre", "firstname", "name" } },
                new() { Key = "Apellidos", Label = "Apellidos", Alias = new[] { "apellidos", "apellido", "primerapellido", "lastname", "surname" } },
                new() { Key = "NombreCompleto", Label = "Nombre completo", Alias = new[] { "nombrecompleto", "empleado", "colaborador", "trabajador", "fullname" } },
                new() { Key = "Cedula", Label = "Identidad / cédula", Alias = new[] { "identidad", "cedula", "dni", "documento", "numeroidentidad", "noidentidad", "documentoidentidad" } },
                new() { Key = "RTN", Label = "RTN", Alias = new[] { "rtn", "nit", "ruc" } },
                new() { Key = "FechaNacimiento", Label = "Fecha de nacimiento", Alias = new[] { "fechanacimiento", "nacimiento", "fnacimiento", "birthdate", "cumpleanos" } },
                new() { Key = "Genero", Label = "Género", Alias = new[] { "genero", "sexo", "gender" } },
                new() { Key = "EstadoCivil", Label = "Estado civil", Alias = new[] { "estadocivil", "civil" } },
                new() { Key = "Telefono", Label = "Teléfono", Alias = new[] { "telefono", "tel", "celular", "movil", "phone" } },
                new() { Key = "Email", Label = "Email", Alias = new[] { "email", "correo", "mail" } },
                new() { Key = "Direccion", Label = "Dirección", Alias = new[] { "direccion", "domicilio", "address" } },
                new() { Key = "Ciudad", Label = "Ciudad", Alias = new[] { "ciudad", "municipio", "city" } },
                new() { Key = "Puesto", Label = "Puesto / cargo", Alias = new[] { "puesto", "cargo", "position", "ocupacion", "plaza" } },
                new() { Key = "Departamento", Label = "Departamento / área", Alias = new[] { "departamento", "area", "seccion", "depto" } },
                new() { Key = "FechaIngreso", Label = "Fecha de ingreso", Alias = new[] { "fechaingreso", "ingreso", "fechacontratacion", "fechaalta", "alta", "hiredate", "fechainicio" } },
                new() { Key = "FechaBaja", Label = "Fecha de baja", Alias = new[] { "fechabaja", "baja", "fechaegreso", "fechasalida", "egreso" } },
                new() { Key = "Salario", Label = "Salario base mensual", Alias = new[] { "salario", "sueldo", "salariobase", "sueldobase", "salary", "salariomensual", "sueldomensual" } },
                new() { Key = "Frecuencia", Label = "Frecuencia de pago", Alias = new[] { "frecuencia", "frecuenciapago", "periodopago", "periodicidad" } },
                new() { Key = "TipoContrato", Label = "Tipo de contrato", Alias = new[] { "tipocontrato", "contrato" } },
                new() { Key = "Banco", Label = "Banco", Alias = new[] { "banco", "bank" } },
                new() { Key = "CuentaBancaria", Label = "Cuenta bancaria", Alias = new[] { "cuentabancaria", "cuenta", "numerocuenta", "nocuenta" } },
                new() { Key = "Vacaciones", Label = "Días de vacaciones disponibles", Alias = new[] { "vacaciones", "diasvacaciones", "vacacionesdisponibles", "saldovacaciones", "diasdisponibles" } },
                new() { Key = "Estado", Label = "Estado / activo", Alias = new[] { "estado", "activo", "status", "situacion" } },
                new() { Key = "Notas", Label = "Notas", Alias = new[] { "notas", "observaciones", "comentarios" } },
            },
            TipoCompras => new()
            {
                new() { Key = "Documento", Label = "N.º de compra / factura del proveedor", Requerido = true, Alias = new[] { "documento", "factura", "numero", "numerofactura", "nocompra", "compra", "orden", "ordencompra", "folio", "doc" } },
                new() { Key = "Fecha", Label = "Fecha", Requerido = true, Alias = new[] { "fecha", "fechacompra", "fechafactura", "emision", "date" } },
                new() { Key = "Proveedor", Label = "Proveedor (nombre)", Alias = new[] { "proveedor", "nombreproveedor", "razonsocial", "supplier", "vendor" } },
                new() { Key = "ProveedorCodigo", Label = "Código del proveedor", Alias = new[] { "codigoproveedor", "idproveedor", "proveedorcodigo", "codproveedor" } },
                new() { Key = "ProveedorRTN", Label = "RTN del proveedor", Alias = new[] { "rtn", "rtnproveedor", "ruc", "nit" } },
                new() { Key = "Total", Label = "Total", Requerido = true, Alias = new[] { "total", "totalfactura", "importe", "montototal", "monto" } },
                new() { Key = "Subtotal", Label = "Subtotal", Alias = new[] { "subtotal", "neto", "sumas" } },
                new() { Key = "Descuento", Label = "Descuento", Alias = new[] { "descuento", "discount" } },
                new() { Key = "Impuestos", Label = "Impuestos", Alias = new[] { "impuestos", "impuesto", "isv", "iva", "tax" } },
                new() { Key = "Saldo", Label = "Saldo pendiente de pagar", Alias = new[] { "saldo", "saldopendiente", "pendiente", "porpagar", "balance" } },
                new() { Key = "Notas", Label = "Notas", Alias = new[] { "notas", "observaciones", "comentario" } },
            },
            TipoCotizaciones => new()
            {
                new() { Key = "Documento", Label = "N.º de cotización", Requerido = true, Alias = new[] { "documento", "cotizacion", "numero", "nocotizacion", "presupuesto", "proforma", "folio", "doc" } },
                new() { Key = "Fecha", Label = "Fecha", Requerido = true, Alias = new[] { "fecha", "fechacotizacion", "emision", "date" } },
                new() { Key = "Vencimiento", Label = "Válida hasta", Alias = new[] { "vencimiento", "validahasta", "vence", "fechavencimiento", "validez" } },
                new() { Key = "Cliente", Label = "Cliente (nombre)", Alias = new[] { "cliente", "nombrecliente", "razonsocial", "customer" } },
                new() { Key = "ClienteCodigo", Label = "Código del cliente", Alias = new[] { "codigocliente", "idcliente", "clientecodigo", "codcliente" } },
                new() { Key = "ClienteRTN", Label = "RTN del cliente", Alias = new[] { "rtn", "rtncliente", "ruc", "nit" } },
                new() { Key = "Total", Label = "Total", Requerido = true, Alias = new[] { "total", "importe", "montototal", "monto" } },
                new() { Key = "Subtotal", Label = "Subtotal", Alias = new[] { "subtotal", "neto" } },
                new() { Key = "Descuento", Label = "Descuento", Alias = new[] { "descuento", "discount" } },
                new() { Key = "Impuestos", Label = "Impuestos", Alias = new[] { "impuestos", "impuesto", "isv", "iva", "tax" } },
                new() { Key = "Estado", Label = "Estado", Alias = new[] { "estado", "status", "situacion" } },
                new() { Key = "Notas", Label = "Notas", Alias = new[] { "notas", "observaciones", "comentario" } },
            },
            TipoMovimientos => new()
            {
                new() { Key = "Fecha", Label = "Fecha", Requerido = true, Alias = new[] { "fecha", "fechamovimiento", "date", "dia" } },
                new() { Key = "Concepto", Label = "Concepto", Requerido = true, Alias = new[] { "concepto", "descripcion", "detalle", "glosa", "motivo", "description" } },
                new() { Key = "Monto", Label = "Monto (con signo o con tipo)", Alias = new[] { "monto", "importe", "valor", "amount", "total" } },
                new() { Key = "Ingreso", Label = "Columna de ingresos / entradas", Alias = new[] { "ingreso", "ingresos", "entrada", "entradas", "haber", "credito", "abono" } },
                new() { Key = "Egreso", Label = "Columna de egresos / salidas", Alias = new[] { "egreso", "egresos", "salida", "salidas", "gasto", "gastos", "debe", "debito", "cargo" } },
                new() { Key = "Tipo", Label = "Tipo (ingreso / egreso)", Alias = new[] { "tipo", "tipomovimiento", "clase", "naturaleza" } },
                new() { Key = "Categoria", Label = "Categoría", Alias = new[] { "categoria", "rubro", "cuentacontable", "clasificacion" } },
                new() { Key = "Cuenta", Label = "Caja / banco", Alias = new[] { "cuenta", "caja", "banco", "cuentabancaria" } },
                new() { Key = "Referencia", Label = "Referencia", Alias = new[] { "referencia", "documento", "numero", "cheque", "factura" } },
                new() { Key = "FormaPago", Label = "Forma de pago", Alias = new[] { "formapago", "metodopago", "forma", "medio" } },
            },
            TipoSaldosContables => new()
            {
                new() { Key = "Codigo", Label = "Código de la cuenta", Requerido = true, Alias = new[] { "codigo", "cuenta", "codigocuenta", "numerocuenta", "code", "account", "nrocuenta" } },
                new() { Key = "Nombre", Label = "Nombre de la cuenta", Alias = new[] { "nombre", "nombrecuenta", "descripcion", "name", "detalle" } },
                new() { Key = "Debe", Label = "Saldo deudor / debe", Alias = new[] { "debe", "deudor", "saldodeudor", "debito", "debit", "cargos" } },
                new() { Key = "Haber", Label = "Saldo acreedor / haber", Alias = new[] { "haber", "acreedor", "saldoacreedor", "credito", "credit", "abonos" } },
                new() { Key = "Saldo", Label = "Saldo (con signo)", Alias = new[] { "saldo", "saldofinal", "balance", "saldoactual", "monto", "importe" } },
                new() { Key = "Fecha", Label = "Fecha de corte", Alias = new[] { "fecha", "fechacorte", "corte", "date" } },
            },
            _ => new()
        };

        /// <summary>Detecta columnas; si el nombre del empleado viene en una sola columna, la usa como «Nombres».</summary>
        public static Dictionary<string, string> Detectar(List<string> headers, string key)
        {
            var mapa = MigracionHistorialHelper.Detectar(headers, Campos(key))
                .Where(p => p.Value != null).ToDictionary(p => p.Key, p => p.Value!);
            if (key == "Empleados" && !mapa.ContainsKey("Nombres") && mapa.TryGetValue("NombreCompleto", out var completo))
                mapa["Nombres"] = completo;
            if (key == TipoMovimientos && !mapa.ContainsKey("Monto") && !mapa.ContainsKey("Ingreso") && !mapa.ContainsKey("Egreso"))
            { /* Faltantes lo reporta */ }
            return mapa;
        }

        public static List<string> Faltantes(string key, Dictionary<string, string> mapa)
        {
            var falta = Campos(key).Where(c => c.Requerido && !mapa.ContainsKey(c.Key)).Select(c => c.Label).ToList();
            if (key == TipoMovimientos && !mapa.ContainsKey("Monto") && !mapa.ContainsKey("Ingreso") && !mapa.ContainsKey("Egreso"))
                falta.Add("monto (o columnas de ingresos/egresos)");
            if (key == TipoCompras && !new[] { "Proveedor", "ProveedorCodigo", "ProveedorRTN" }.Any(mapa.ContainsKey))
                falta.Add("proveedor");
            if (key == TipoSaldosContables && !mapa.ContainsKey("Saldo") && !mapa.ContainsKey("Debe") && !mapa.ContainsKey("Haber"))
                falta.Add("saldo (o columnas de debe y haber)");
            return falta;
        }

        // ================= Utilidades =================
        private static string? V(Dictionary<string, string> fila, Dictionary<string, string> mapa, string campo) => Valor(fila, mapa, campo);
        private static decimal? D(Dictionary<string, string> fila, Dictionary<string, string> mapa, string campo) => ParserHelper.ParsearDecimal(V(fila, mapa, campo));
        private static string? R(Dictionary<string, string> fila, Dictionary<string, string> mapa, string campo, int max) =>
            V(fila, mapa, campo) is string s ? Recortar(s, max) : null;

        private static bool EsActivo(string? t)
        {
            if (t == null) return true;
            var b = ParserHelper.ParsearBool(t);
            if (b != null) return b.Value;
            var n = Norm(t);
            return !(n.Contains("inactiv") || n.Contains("baja") || n.Contains("retir") || n.Contains("deshab") || n.Contains("despedid") || n.Contains("renunc") || n == "i" || n == "0");
        }

        private static void Registrar(ApplicationDbContext ctx, string lote, string entidad, int id) =>
            ctx.RegistrosMigracion.Add(new RegistroMigracion { Lote = lote, Entidad = entidad, EntidadId = id, Fecha = DateTime.Now });

        // ================= Catálogos =================
        public static void ProcesarCatalogo(ApplicationDbContext ctx, TablaOrigen tabla, string key, Dictionary<string, string> mapa,
            Resultado r, int? usuarioId)
        {
            var lote = NuevoLote();
            r.Lote = lote;
            switch (key)
            {
                case "PlanCuentas": PlanCuentas(ctx, tabla, mapa, r, lote); break;
                case "MetodosPago": MetodosPago(ctx, tabla, mapa, r, lote); break;
                case "CuentasFinancieras": Cuentas(ctx, tabla, mapa, r, usuarioId, lote); break;
                case "Empleados": Empleados(ctx, tabla, mapa, r, usuarioId, lote); break;
                default: r.Errores.Add("Tipo de catálogo desconocido."); break;
            }
        }

        // ---------- Plan de cuentas ----------
        private static string TipoCuentaContable(string? texto, string codigo)
        {
            var n = Norm(texto);
            if (n.Contains("activ")) return "Activo";
            if (n.Contains("pasiv")) return "Pasivo";
            if (n.Contains("patrim") || n.Contains("capital")) return "Patrimonio";
            if (n.Contains("ingres") || n.Contains("venta") || n.Contains("renta")) return "Ingreso";
            if (n.Contains("cost")) return "Costo";
            if (n.Contains("gast") || n.Contains("egres")) return "Gasto";
            // Convención de catálogos contables: 1 activo, 2 pasivo, 3 patrimonio, 4 ingresos, 5 costos, 6 gastos
            return codigo.TrimStart().FirstOrDefault() switch
            {
                '1' => "Activo", '2' => "Pasivo", '3' => "Patrimonio", '4' => "Ingreso", '5' => "Costo", _ => "Gasto"
            };
        }

        /// <summary>Partes del código (1.1.01, 1-1-01, 110101 en bloques de 1-1-2-2…): sirve para el nivel y el padre.</summary>
        private static string[] Segmentos(string codigo) =>
            Regex.Split(codigo.Trim(), @"[.\-\s/]+").Where(s => s.Length > 0).ToArray();

        /// <summary>
        /// Prefijo de las cuentas del sistema anterior cuyo código ya existe en Kirkenta con otro significado
        /// (p. ej. «1.1 Caja» allá y «1.1 Activo Corriente» aquí): entran como «MIG.1.1» para no mezclar catálogos.
        /// </summary>
        public const string PrefijoMigradas = "MIG.";

        /// <summary>Dos nombres de cuenta hablan de lo mismo (iguales o uno contiene al otro).</summary>
        private static bool NombresCompatibles(string? a, string? b)
        {
            var na = Norm(a); var nb = Norm(b);
            if (na.Length == 0 || nb.Length == 0) return true;
            return na == nb || (Math.Min(na.Length, nb.Length) >= 4 && (na.Contains(nb) || nb.Contains(na)));
        }

        private static void PlanCuentas(ApplicationDbContext ctx, TablaOrigen tabla, Dictionary<string, string> mapa, Resultado r, string lote)
        {
            var existentes = ctx.PlanCuentas.AsNoTracking().Select(p => new { p.Codigo, p.Nombre }).ToList()
                .ToDictionary(p => p.Codigo, p => p.Nombre, StringComparer.OrdinalIgnoreCase);
            var remap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);   // código del archivo → código en Kirkenta
            var filas = new List<(int nf, PlanCuenta p, string? padre, bool? mov)>();
            int n = 0;
            foreach (var fila in tabla.Rows)
            {
                MigracionCorrida.Fila(tabla.Rows.Count);
                n++; int nf = n + 1;
                var codigo = V(fila, mapa, "Codigo");
                var nombre = V(fila, mapa, "Nombre");
                if (codigo == null || nombre == null) { r.Omitidos++; r.Error(nf, "sin código o sin nombre."); continue; }
                codigo = Recortar(codigo.Trim(), 20);
                if (existentes.TryGetValue(codigo, out var nombreActual))
                {
                    if (NombresCompatibles(nombre, nombreActual)) { r.YaImportados++; continue; }
                    // Mismo código, otra cuenta: el catálogo del sistema anterior es distinto
                    var alterno = Recortar(PrefijoMigradas + codigo, 20);
                    if (existentes.ContainsKey(alterno)) { remap[codigo] = alterno; r.YaImportados++; continue; }
                    r.Aviso(nf, $"La cuenta {codigo} «{nombre}» del sistema anterior no es la {codigo} «{nombreActual}» de Kirkenta: entra como {alterno} para que el contador la reclasifique.");
                    remap[codigo] = alterno;
                    codigo = alterno;
                }
                existentes[codigo] = nombre;

                var tipo = TipoCuentaContable(V(fila, mapa, "Tipo"), codigo);
                var natTxt = Norm(V(fila, mapa, "Naturaleza"));
                var naturaleza = natTxt.StartsWith("acre") || natTxt == "c" || natTxt == "h" ? "Acreedora"
                               : natTxt.StartsWith("deu") || natTxt == "d" ? "Deudora"
                               : (tipo is "Activo" or "Costo" or "Gasto") ? "Deudora" : "Acreedora";
                var seg = Segmentos(codigo.StartsWith(PrefijoMigradas) ? codigo[PrefijoMigradas.Length..] : codigo);
                var movTxt = V(fila, mapa, "Movimiento");
                filas.Add((nf, new PlanCuenta
                {
                    Codigo = codigo, Nombre = Recortar(nombre, 150), Tipo = tipo, Naturaleza = naturaleza,
                    Nivel = Math.Max(1, seg.Length), Activa = EsActivo(V(fila, mapa, "Activa"))
                }, V(fila, mapa, "Padre"), movTxt == null ? null : ParserHelper.ParsearBool(movTxt) ?? EsActivo(movTxt)));
            }

            // Padre: el indicado o el código sin su último segmento. Movimiento: el indicado o "no tiene hijas".
            var todos = ctx.PlanCuentas.AsNoTracking().Select(p => p.Codigo).ToList().Concat(filas.Select(f => f.p.Codigo))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var (nf, p, padre, _) in filas)
            {
                string? candidato = padre?.Trim();
                if (string.IsNullOrEmpty(candidato))
                {
                    var original = p.Codigo.StartsWith(PrefijoMigradas) ? p.Codigo[PrefijoMigradas.Length..] : p.Codigo;
                    var seg = Segmentos(original);
                    if (seg.Length > 1)
                    {
                        var sep = original.Contains('.') ? "." : original.Contains('-') ? "-" : ".";
                        candidato = string.Join(sep, seg.Take(seg.Length - 1));
                    }
                }
                // Si la madre también se renombró (MIG.), la hija cuelga de la renombrada
                if (candidato != null && remap.TryGetValue(candidato, out var madre)) candidato = madre;
                p.CodigoPadre = candidato != null && todos.Contains(candidato) ? candidato : null;
            }
            var padres = filas.Where(f => f.p.CodigoPadre != null).Select(f => f.p.CodigoPadre!).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var (_, p, _, mov) in filas)
            {
                p.EsMovimiento = mov ?? !padres.Contains(p.Codigo);
                ctx.PlanCuentas.Add(p);
                r.Creados++;
            }
            ctx.SaveChanges();
            foreach (var f in filas) Registrar(ctx, lote, "PlanCuenta", f.p.Id);
            ctx.SaveChanges();
        }

        // ---------- Métodos de pago ----------
        private static string TipoMetodo(string nombre, string? tipo)
        {
            var n = Norm((tipo ?? "") + " " + nombre);
            if (n.Contains("transfer") || n.Contains("deposit") || Regex.IsMatch(n, @"\bach\b") || n.Contains("banco")) return "Transferencia";
            if (n.Contains("tarjeta") || n.Contains("card") || Regex.IsMatch(n, @"\bpos\b") || n.Contains("visa") || n.Contains("master")) return "Tarjeta";
            if (n.Contains("cheque")) return "Cheque";
            if (n.Contains("credito") || n.Contains("fiado")) return "Credito";
            if (n.Contains("efectivo") || n.Contains("cash") || n.Contains("contado")) return "Efectivo";
            return "Otro";
        }

        private static void MetodosPago(ApplicationDbContext ctx, TablaOrigen tabla, Dictionary<string, string> mapa, Resultado r, string lote)
        {
            var nombres = ctx.MetodosPago.AsNoTracking().Select(m => m.Nombre).ToList().Select(Norm).ToHashSet();
            var nuevos = new List<MetodoPago>();
            int n = 0;
            foreach (var fila in tabla.Rows)
            {
                MigracionCorrida.Fila(tabla.Rows.Count);
                n++; int nf = n + 1;
                var nombre = V(fila, mapa, "Nombre");
                if (nombre == null) { r.Omitidos++; r.Error(nf, "sin nombre."); continue; }
                if (!nombres.Add(Norm(nombre))) { r.YaImportados++; continue; }
                var tipo = TipoMetodo(nombre, V(fila, mapa, "Tipo"));
                var m = new MetodoPago
                {
                    Nombre = Recortar(nombre, 50), Tipo = tipo, Activo = EsActivo(V(fila, mapa, "Activo")),
                    RequiereReferencia = tipo is "Tarjeta" or "Transferencia" or "Cheque"
                };
                ctx.MetodosPago.Add(m); nuevos.Add(m);
                r.Creados++;
            }
            ctx.SaveChanges();
            foreach (var m in nuevos) Registrar(ctx, lote, "MetodoPago", m.Id);
            ctx.SaveChanges();
        }

        // ---------- Cajas y bancos ----------
        private static void Cuentas(ApplicationDbContext ctx, TablaOrigen tabla, Dictionary<string, string> mapa, Resultado r, int? usuarioId, string lote)
        {
            var cuentas = ctx.CuentasFinancieras.ToList();
            var conMovimientos = ctx.MovimientosFinancieros.AsNoTracking()
                .Where(m => m.Origen != "Apertura").Select(m => m.CuentaId).Distinct().ToList().ToHashSet();
            var codigos = cuentas.Select(c => c.Codigo).ToHashSet(StringComparer.OrdinalIgnoreCase);
            int consecutivo = 1;
            int n = 0;
            foreach (var fila in tabla.Rows)
            {
                MigracionCorrida.Fila(tabla.Rows.Count);
                n++; int nf = n + 1;
                var nombre = V(fila, mapa, "Nombre");
                if (nombre == null) { r.Omitidos++; r.Error(nf, "sin nombre."); continue; }
                var codigo = V(fila, mapa, "Codigo");
                var numero = V(fila, mapa, "NumeroCuenta");
                var saldo = Math.Round(D(fila, mapa, "Saldo") ?? 0m, 2, MidpointRounding.AwayFromZero);
                var banco = V(fila, mapa, "Banco");
                var tipoTxt = Norm(V(fila, mapa, "Tipo"));
                var tipo = tipoTxt.Contains("banc") || tipoTxt.Contains("ahorro") || tipoTxt.Contains("corriente") || tipoTxt.Contains("cheque")
                           || (tipoTxt.Length == 0 && (banco != null || numero != null)) ? "Banco" : "Caja";

                var existente = cuentas.FirstOrDefault(c =>
                    (codigo != null && string.Equals(c.Codigo, codigo, StringComparison.OrdinalIgnoreCase)) ||
                    (numero != null && c.NumeroCuenta != null && SoloDigitos(c.NumeroCuenta) == SoloDigitos(numero) && SoloDigitos(numero).Length >= 4) ||
                    Norm(c.Nombre) == Norm(nombre));
                if (existente != null)
                {
                    // Una cuenta del sistema sin uso (p. ej. «Caja General» recién instalada) toma el saldo del sistema anterior
                    if (saldo > 0 && existente.SaldoInicial == 0 && existente.SaldoActual == 0 && !conMovimientos.Contains(existente.Id))
                    {
                        existente.SaldoInicial = saldo; existente.SaldoActual = saldo;
                        if (banco != null && existente.Banco == null) existente.Banco = Recortar(banco, 100);
                        if (numero != null && existente.NumeroCuenta == null) existente.NumeroCuenta = Recortar(numero, 50);
                        ctx.SaveChanges();
                        var (mov, _) = AperturaCuentaHelper.RegistrarAperturaCuenta(ctx, existente.Id, usuarioId);
                        if (mov != null) Registrar(ctx, lote, "MovimientoFinanciero", mov.Id);
                        r.Monto += saldo; r.Creados++;
                        r.Aviso(nf, $"«{existente.Nombre}» ya existía sin movimientos: toma el saldo de arranque L. {saldo:N2}.");
                    }
                    else
                    {
                        r.YaImportados++;
                        if (saldo != existente.SaldoActual)
                            r.Aviso(nf, $"«{existente.Nombre}» ya existe con saldo L. {existente.SaldoActual:N2} (el archivo dice L. {saldo:N2}); no se cambió.");
                    }
                    continue;
                }

                if (codigo == null || codigo.Length > 20 || codigos.Contains(codigo))
                {
                    do { codigo = (tipo == "Banco" ? "BAN-MIG-" : "CAJ-MIG-") + consecutivo.ToString("00"); consecutivo++; } while (codigos.Contains(codigo));
                }
                codigos.Add(codigo);
                if (saldo < 0) r.Aviso(nf, $"«{nombre}» tiene saldo negativo (L. {saldo:N2}): se registra en 0; revisa si es un sobregiro.");

                var c = new CuentaFinanciera
                {
                    Codigo = codigo, Nombre = Recortar(nombre, 100), Tipo = tipo,
                    Banco = banco == null ? null : Recortar(banco, 100), NumeroCuenta = numero == null ? null : Recortar(numero, 50),
                    SaldoInicial = Math.Max(0, saldo), SaldoActual = Math.Max(0, saldo),
                    Responsable = R(fila, mapa, "Responsable", 100), Activa = EsActivo(V(fila, mapa, "Activa")),
                    Descripcion = "Migrada del sistema anterior", UsuarioCreoId = usuarioId, FechaCreacion = DateTime.Now
                };
                ctx.CuentasFinancieras.Add(c);
                ctx.SaveChanges();
                cuentas.Add(c);
                Registrar(ctx, lote, "CuentaFinanciera", c.Id);
                if (c.SaldoInicial > 0)
                {
                    var (mov, _) = AperturaCuentaHelper.RegistrarAperturaCuenta(ctx, c.Id, usuarioId);
                    if (mov != null) Registrar(ctx, lote, "MovimientoFinanciero", mov.Id);
                }
                r.Monto += c.SaldoInicial;
                r.Creados++;
            }
            ctx.SaveChanges();
        }

        // ---------- Empleados ----------
        private static string Frecuencia(string? t)
        {
            var n = Norm(t);
            if (n.Contains("seman")) return "Semanal";
            if (n.Contains("catorc") || n.Contains("bisem") || n.Contains("14")) return "Catorcenal";
            if (n.Contains("quinc") || n.Contains("15")) return "Quincenal";
            return "Mensual";
        }

        /// <summary>«EMP-0007» → «EMP-0008» (respeta los ceros a la izquierda).</summary>
        internal static string SiguienteCodigo(string codigo)
        {
            var m = System.Text.RegularExpressions.Regex.Match(codigo, @"^(.*?)(\d+)$");
            if (!m.Success) return codigo + "-2";
            var num = (long.Parse(m.Groups[2].Value) + 1).ToString().PadLeft(m.Groups[2].Value.Length, '0');
            return m.Groups[1].Value + num;
        }

        private static void Empleados(ApplicationDbContext ctx, TablaOrigen tabla, Dictionary<string, string> mapa, Resultado r, int? usuarioId, string lote)
        {
            var existentes = ctx.Empleados.AsNoTracking().Select(e => new { e.Codigo, e.Cedula, e.Nombres, e.Apellidos }).ToList();
            var porCedula = existentes.Where(e => SoloDigitos(e.Cedula).Length >= 6).Select(e => SoloDigitos(e.Cedula)).ToHashSet();
            var porCodigo = existentes.Select(e => e.Codigo).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var porNombre = existentes.Select(e => Norm(e.Nombres + " " + e.Apellidos)).ToHashSet();
            bool hayVacaciones = mapa.ContainsKey("Vacaciones");
            var nuevos = new List<Empleado>();
            int n = 0;
            foreach (var fila in tabla.Rows)
            {
                MigracionCorrida.Fila(tabla.Rows.Count);
                n++; int nf = n + 1;
                var nombres = V(fila, mapa, "Nombres");
                var apellidos = V(fila, mapa, "Apellidos");
                if (nombres == null) { r.Omitidos++; r.Error(nf, "sin nombre."); continue; }
                if (apellidos == null && !mapa.ContainsKey("Apellidos"))
                {
                    // Nombre completo en una sola columna: dos primeras palabras = nombres, el resto = apellidos
                    var partes = nombres.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (partes.Length >= 3) { nombres = string.Join(' ', partes.Take(2)); apellidos = string.Join(' ', partes.Skip(2)); }
                    else if (partes.Length == 2) { nombres = partes[0]; apellidos = partes[1]; }
                }
                apellidos ??= "";
                var cedula = V(fila, mapa, "Cedula") ?? "";
                var codigo = V(fila, mapa, "Codigo");
                var digCed = SoloDigitos(cedula);
                if ((digCed.Length >= 6 && porCedula.Contains(digCed)) || (codigo != null && porCodigo.Contains(codigo))
                    || porNombre.Contains(Norm(nombres + " " + apellidos)))
                { r.YaImportados++; continue; }
                if (digCed.Length >= 6) porCedula.Add(digCed);
                porNombre.Add(Norm(nombres + " " + apellidos));

                if (codigo == null || codigo.Length > 20 || porCodigo.Contains(codigo))
                {
                    // el correlativo sale de la base y los de este lote aún no están guardados: se salta lo ya usado
                    codigo = EmpleadoHelper.GenerarCodigo(ctx);
                    while (porCodigo.Contains(codigo)) codigo = SiguienteCodigo(codigo);
                }
                porCodigo.Add(codigo);

                var ingreso = ParsearFecha(V(fila, mapa, "FechaIngreso"));
                if (ingreso == null) { ingreso = DateTime.Today; r.Aviso(nf, $"«{nombres} {apellidos}» sin fecha de ingreso válida: se usó hoy (corrígela para calcular vacaciones y prestaciones)."); }
                var baja = ParsearFecha(V(fila, mapa, "FechaBaja"));
                var activo = EsActivo(V(fila, mapa, "Estado")) && baja == null;
                var salario = Math.Max(0, Math.Round(D(fila, mapa, "Salario") ?? 0m, 2, MidpointRounding.AwayFromZero));
                if (salario == 0) r.Aviso(nf, $"«{nombres} {apellidos}» sin salario: entra con L. 0.00.");
                var email = V(fila, mapa, "Email");
                if (email != null && !email.Contains('@')) email = null;
                var genero = Norm(V(fila, mapa, "Genero"));

                var e = new Empleado
                {
                    Codigo = codigo, Nombres = Recortar(nombres, 100), Apellidos = Recortar(apellidos, 100), Cedula = Recortar(cedula, 20),
                    RTN = R(fila, mapa, "RTN", 20), FechaNacimiento = ParsearFecha(V(fila, mapa, "FechaNacimiento")),
                    Genero = genero.StartsWith("m") ? "Masculino" : genero.StartsWith("f") ? "Femenino" : null,
                    EstadoCivil = R(fila, mapa, "EstadoCivil", 20), Telefono = R(fila, mapa, "Telefono", 20), Email = email == null ? null : Recortar(email, 100),
                    Direccion = R(fila, mapa, "Direccion", 300), Ciudad = R(fila, mapa, "Ciudad", 100),
                    PuestoNombre = R(fila, mapa, "Puesto", 100), Departamento = R(fila, mapa, "Departamento", 100),
                    FechaIngreso = ingreso.Value, FechaBaja = baja,
                    TipoContrato = Norm(V(fila, mapa, "TipoContrato")).Contains("tempor") ? "Temporal" : "Indefinido",
                    SalarioBase = salario, FrecuenciaPago = Frecuencia(V(fila, mapa, "Frecuencia")),
                    Banco = R(fila, mapa, "Banco", 100), CuentaBancaria = R(fila, mapa, "CuentaBancaria", 50),
                    Estado = activo ? "Activo" : "Baja", Notas = V(fila, mapa, "Notas"),
                    UsuarioCreoId = usuarioId, FechaCreacion = DateTime.Now
                };
                // Vacaciones: si el sistema anterior trae el saldo se respeta y no se vuelven a acumular los años ya cumplidos;
                // si no lo trae, el saldo queda en 0 (no se inventan días que quizá ya se gozaron).
                var anios = EmpleadoHelper.CalcularAniosAntiguedad(e);
                e.UltimoAnioVacacionesProcesado = anios;
                var dias = hayVacaciones ? Math.Max(0, D(fila, mapa, "Vacaciones") ?? 0m) : 0m;
                e.DiasVacacionesDisponibles = dias; e.DiasVacacionesGanados = dias;
                ctx.Empleados.Add(e); nuevos.Add(e);
                r.Monto += salario;
                r.Creados++;
            }
            if (!hayVacaciones && nuevos.Count > 0)
                r.Aviso(0, "El archivo no trae los días de vacaciones disponibles: quedan en 0. Ajústalos en el expediente de cada empleado.");
            ctx.SaveChanges();
            foreach (var e in nuevos) Registrar(ctx, lote, "Empleado", e.Id);
            ctx.SaveChanges();
        }

        // ================= Historiales =================
        public static List<CampoDef> CamposHistorial(string tipo) => Campos(tipo);

        public static Resultado ProcesarHistorial(ApplicationDbContext ctx, TablaOrigen tabla, Opciones op, bool aplicar, int? usuarioId)
        {
            var r = new Resultado { Tipo = op.Tipo, Filas = tabla.Rows.Count, Tolerante = op.Tolerante };
            var falta = Faltantes(op.Tipo, op.Mapa);
            if (falta.Count > 0) { r.Errores.Add("No se encontró la columna de: " + string.Join(", ", falta) + "."); return r; }

            var propia = aplicar && ctx.Database.CurrentTransaction == null && ctx.Database.IsRelational();
            using var tx = propia ? ctx.Database.BeginTransaction() : null;
            try
            {
                var lote = NuevoLote();
                r.Lote = lote;
                switch (op.Tipo)
                {
                    case TipoCompras: Compras(ctx, tabla, op, r, usuarioId, lote); break;
                    case TipoCotizaciones: Cotizaciones(ctx, tabla, op, r, usuarioId, lote); break;
                    case TipoMovimientos: Movimientos(ctx, tabla, op, r, usuarioId, lote); break;
                    case TipoSaldosContables: SaldosContables(ctx, tabla, op, r, usuarioId, lote); break;
                }
                if (!aplicar || r.Errores.Count > 0) { tx?.Rollback(); ctx.ChangeTracker.Clear(); if (!aplicar) r.Aplicado = false; return r; }
                tx?.Commit();
                r.Aplicado = true;
            }
            catch
            {
                tx?.Rollback();
                ctx.ChangeTracker.Clear();
                throw;
            }
            return r;
        }

        /// <summary>Líneas de detalle agrupadas por documento (misma tabla de líneas que el historial de ventas).</summary>
        private static Dictionary<string, List<(string? cod, string? nom, decimal cant, decimal precio, decimal desc)>> Lineas(Opciones op, Resultado r)
        {
            var res = new Dictionary<string, List<(string?, string?, decimal, decimal, decimal)>>(StringComparer.OrdinalIgnoreCase);
            if (op.Detalle == null) return res;
            foreach (var fila in op.Detalle.Rows)
            {
                var doc = Valor(fila, op.MapaDetalle, "Documento");
                var cant = ParserHelper.ParsearDecimal(Valor(fila, op.MapaDetalle, "Cantidad"));
                var precio = ParserHelper.ParsearDecimal(Valor(fila, op.MapaDetalle, "Precio"));
                if (doc == null || cant == null || cant <= 0 || precio == null) continue;
                if (!res.TryGetValue(doc, out var l)) res[doc] = l = new();
                l.Add((Valor(fila, op.MapaDetalle, "ProductoCodigo"), Valor(fila, op.MapaDetalle, "ProductoNombre"), cant.Value,
                       Math.Round(precio.Value, 4), Math.Round(ParserHelper.ParsearDecimal(Valor(fila, op.MapaDetalle, "Descuento")) ?? 0, 2, MidpointRounding.AwayFromZero)));
            }
            return res;
        }

        private static int Tercero(ApplicationDbContext ctx, IndiceTerceros idx, bool proveedor, string? codigo, string? rtn, string? nombre, int? usuarioId, Resultado r)
        {
            var id = idx.Buscar(codigo, rtn, nombre);
            if (id != null) return id.Value;
            var nom = Recortar(nombre ?? rtn ?? codigo ?? "Sin nombre", 150);
            var cod = codigo != null && codigo.Length <= 20 && !idx.CodigosUsados.Contains(codigo) ? codigo : null;
            int nuevo;
            if (proveedor)
            {
                var p = new Proveedor { Nombre = nom, Codigo = cod, RTN = rtn == null ? null : Recortar(rtn, 20), Activo = true, UsuarioCreoId = usuarioId, Notas = "Creado por la migración (venía en el historial de compras)." };
                ctx.Proveedores.Add(p); ctx.SaveChanges(); nuevo = p.Id;
            }
            else
            {
                var c = new Cliente { Nombre = nom, Codigo = cod, RTN = rtn == null ? null : Recortar(rtn, 20), Activo = true, TipoCliente = "Regular", UsuarioCreoId = usuarioId, Notas = "Creado por la migración (venía en las cotizaciones)." };
                ctx.Clientes.Add(c); ctx.SaveChanges(); nuevo = c.Id;
            }
            idx.Agregar(new Tercero { Id = nuevo, Codigo = cod, Rtn = SoloDigitos(rtn), Nombre = nom });
            r.TercerosCreados++;
            return nuevo;
        }

        private static decimal TasaDe(decimal subtotal, decimal descuento, decimal impuestos)
        {
            var baseImp = subtotal - descuento;
            if (impuestos <= 0 || baseImp <= 0) return 0m;
            var tasa = Math.Round(impuestos / baseImp * 100m, 0);
            return tasa is >= 1 and <= 100 ? tasa : 0m;
        }

        // ---------- Compras ----------
        private static void Compras(ApplicationDbContext ctx, TablaOrigen tabla, Opciones op, Resultado r, int? usuarioId, string lote)
        {
            var idxProv = IndiceProveedores(ctx);
            var idxProd = IndiceProductos(ctx);
            var lineas = Lineas(op, r);
            var numeros = ctx.OrdenesCompra.AsNoTracking().Select(o => o.Numero).ToList().ToHashSet(StringComparer.OrdinalIgnoreCase);
            int? generico = null;
            int n = 0;
            foreach (var fila in tabla.Rows)
            {
                MigracionCorrida.Fila(tabla.Rows.Count);
                n++; int nf = n + 1;
                var doc = V(fila, op.Mapa, "Documento");
                var fecha = ParsearFecha(V(fila, op.Mapa, "Fecha"));
                var total = D(fila, op.Mapa, "Total");
                if (doc == null || fecha == null || total == null || total < 0) { r.Omitidos++; r.Error(nf, "documento, fecha o total no válidos."); continue; }
                var numero = Recortar("H-CMP-" + doc.Trim(), 50);
                if (numeros.Contains(numero)) { r.YaImportados++; continue; }
                numeros.Add(numero);

                var provId = Tercero(ctx, idxProv, true, V(fila, op.Mapa, "ProveedorCodigo"), V(fila, op.Mapa, "ProveedorRTN"), V(fila, op.Mapa, "Proveedor"), usuarioId, r);
                var desc = Math.Round(D(fila, op.Mapa, "Descuento") ?? 0, 2, MidpointRounding.AwayFromZero);
                var imp = Math.Round(D(fila, op.Mapa, "Impuestos") ?? 0, 2, MidpointRounding.AwayFromZero);
                var sub = Math.Round(D(fila, op.Mapa, "Subtotal") ?? (total.Value - imp + desc), 2, MidpointRounding.AwayFromZero);
                var saldo = Math.Min(Math.Max(0, Math.Round(D(fila, op.Mapa, "Saldo") ?? 0, 2, MidpointRounding.AwayFromZero)), total.Value);
                var tasa = TasaDe(sub, desc, imp);

                var oc = new OrdenCompra
                {
                    Numero = numero, ProveedorId = provId, Fecha = fecha.Value, FechaRecepcion = fecha.Value, Estado = "Recibida",
                    Subtotal = sub, Descuento = desc, Impuestos = imp, Total = Math.Round(total.Value, 2, MidpointRounding.AwayFromZero), Saldo = saldo,
                    Notas = $"Compra histórica migrada — documento {doc}" + (V(fila, op.Mapa, "Notas") is string nt ? " — " + nt : ""),
                    UsuarioCreoId = usuarioId, UsuarioRecibioId = usuarioId, LoteMigracion = lote
                };
                ctx.OrdenesCompra.Add(oc);
                ctx.SaveChanges();

                if (lineas.TryGetValue(doc.Trim(), out var ls))
                {
                    foreach (var (cod, nom, cant, precio, dLinea) in ls)
                    {
                        var prodId = idxProd.Buscar(cod, nom) ?? (generico ??= ProductoGenerico(ctx));
                        var bruto = Math.Round(cant * precio, 2, MidpointRounding.AwayFromZero);
                        var neto = Math.Max(0, bruto - dLinea);
                        ctx.DetalleOrdenesCompra.Add(new DetalleOrdenCompra
                        {
                            OrdenCompraId = oc.Id, ProductoId = prodId, Cantidad = cant, CantidadRecibida = cant, PrecioUnitario = precio,
                            Descuento = dLinea, ImpuestoPorcentaje = tasa, Subtotal = bruto, Total = Math.Round(neto * (1 + tasa / 100m), 2, MidpointRounding.AwayFromZero)
                        });
                        r.LineasCreadas++;
                    }
                }
                r.Monto += oc.Total;
                r.Creados++;
            }
            if (generico != null) r.Aviso(0, "Algunas líneas tenían artículos que no existen en el catálogo: quedaron con «Artículo histórico (migración)».");
            r.Aviso(0, "Las compras históricas no mueven el inventario ni las cuentas: la existencia y los saldos actuales vienen de su propia migración.");
            ctx.SaveChanges();
        }

        // ---------- Cotizaciones ----------
        private static void Cotizaciones(ApplicationDbContext ctx, TablaOrigen tabla, Opciones op, Resultado r, int? usuarioId, string lote)
        {
            var idxCli = IndiceClientes(ctx);
            var idxProd = IndiceProductos(ctx);
            var lineas = Lineas(op, r);
            var numeros = ctx.Cotizaciones.AsNoTracking().Select(c => c.Numero).ToList().ToHashSet(StringComparer.OrdinalIgnoreCase);
            int? generico = null;
            var nuevas = new List<Cotizacion>();
            int n = 0;
            foreach (var fila in tabla.Rows)
            {
                MigracionCorrida.Fila(tabla.Rows.Count);
                n++; int nf = n + 1;
                var doc = V(fila, op.Mapa, "Documento");
                var fecha = ParsearFecha(V(fila, op.Mapa, "Fecha"));
                var total = D(fila, op.Mapa, "Total");
                if (doc == null || fecha == null || total == null || total < 0) { r.Omitidos++; r.Error(nf, "documento, fecha o total no válidos."); continue; }
                var numero = Recortar("H-COT-" + doc.Trim(), 50);
                if (numeros.Contains(numero)) { r.YaImportados++; continue; }
                numeros.Add(numero);

                bool hayCliente = new[] { "Cliente", "ClienteCodigo", "ClienteRTN" }.Any(k => V(fila, op.Mapa, k) != null);
                int clienteId = hayCliente
                    ? Tercero(ctx, idxCli, false, V(fila, op.Mapa, "ClienteCodigo"), V(fila, op.Mapa, "ClienteRTN"), V(fila, op.Mapa, "Cliente"), usuarioId, r)
                    : 1;
                var desc = Math.Round(D(fila, op.Mapa, "Descuento") ?? 0, 2, MidpointRounding.AwayFromZero);
                var imp = Math.Round(D(fila, op.Mapa, "Impuestos") ?? 0, 2, MidpointRounding.AwayFromZero);
                var sub = Math.Round(D(fila, op.Mapa, "Subtotal") ?? (total.Value - imp + desc), 2, MidpointRounding.AwayFromZero);
                var vence = ParsearFecha(V(fila, op.Mapa, "Vencimiento"));
                var est = Norm(V(fila, op.Mapa, "Estado"));
                var estado = est.Contains("acept") || est.Contains("aprob") ? "Aceptada"
                           : est.Contains("rechaz") || est.Contains("anul") || est.Contains("cancel") ? "Rechazada"
                           : est.Contains("factur") || est.Contains("convert") || est.Contains("vend") ? "Convertida"
                           : vence != null && vence.Value.Date < DateTime.Today ? "Vencida" : "Enviada";
                var c = new Cotizacion
                {
                    Numero = numero, ClienteId = clienteId, Fecha = fecha.Value, FechaVencimiento = vence,
                    Subtotal = sub, Descuento = desc, Impuestos = imp, Total = Math.Round(total.Value, 2, MidpointRounding.AwayFromZero), Estado = estado,
                    Validez = vence != null ? Math.Max(0, (vence.Value.Date - fecha.Value.Date).Days) : 30,
                    Notas = $"Cotización migrada — documento {doc}" + (V(fila, op.Mapa, "Notas") is string nt ? " — " + nt : ""),
                    UsuarioCreoId = usuarioId
                };
                ctx.Cotizaciones.Add(c);
                ctx.SaveChanges();
                nuevas.Add(c);

                var tasa = TasaDe(sub, desc, imp);
                if (lineas.TryGetValue(doc.Trim(), out var ls))
                {
                    foreach (var (cod, nom, cant, precio, dLinea) in ls)
                    {
                        var prodId = idxProd.Buscar(cod, nom) ?? (generico ??= ProductoGenerico(ctx));
                        var neto = Math.Max(0, Math.Round(cant * precio, 2, MidpointRounding.AwayFromZero) - dLinea);
                        ctx.DetalleCotizaciones.Add(new DetalleCotizacion
                        {
                            CotizacionId = c.Id, ProductoId = prodId, Descripcion = nom == null ? null : Recortar(nom, 300), Cantidad = cant,
                            PrecioUnitario = precio, Descuento = dLinea, ImpuestoPorcentaje = tasa, Subtotal = neto,
                            Total = Math.Round(neto * (1 + tasa / 100m), 2, MidpointRounding.AwayFromZero)
                        });
                        r.LineasCreadas++;
                    }
                }
                else if (estado is "Enviada" or "Aceptada")
                    r.Aviso(nf, $"La cotización {doc} no trae líneas: no se podrá convertir en venta sin agregarle productos.");
                r.Monto += c.Total;
                r.Creados++;
            }
            ctx.SaveChanges();
            foreach (var c in nuevas) Registrar(ctx, lote, "Cotizacion", c.Id);
            ctx.SaveChanges();
        }

        // ---------- Ingresos y egresos históricos ----------
        private static void Movimientos(ApplicationDbContext ctx, TablaOrigen tabla, Opciones op, Resultado r, int? usuarioId, string lote)
        {
            var cuentas = ctx.CuentasFinancieras.AsNoTracking().ToList();
            var cuentaDefecto = cuentas.FirstOrDefault(c => c.Activa && c.Tipo == "Caja") ?? cuentas.FirstOrDefault();
            if (cuentaDefecto == null) { r.Errores.Add("No hay ninguna caja o banco en el sistema para registrar los movimientos."); return; }
            var categorias = ctx.CategoriasFinancieras.ToList();
            var moneda = ctx.Monedas.AsNoTracking().FirstOrDefault(m => m.EsPredeterminada)?.Id ?? 1;
            var nuevos = new List<MovimientoFinanciero>();
            var numerosLote = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            int Categoria(string tipo, string? nombre)
            {
                var nom = string.IsNullOrWhiteSpace(nombre) ? (tipo == "Ingreso" ? "Otros ingresos (migración)" : "Otros gastos (migración)") : Recortar(nombre, 100);
                var c = categorias.FirstOrDefault(x => x.Tipo == tipo && Norm(x.Nombre) == Norm(nom));
                if (c != null) return c.Id;
                c = new CategoriaFinanciera { Tipo = tipo, Nombre = nom, Activa = true, FechaCreacion = DateTime.Now, Color = tipo == "Ingreso" ? "#16a34a" : "#dc2626" };
                ctx.CategoriasFinancieras.Add(c); ctx.SaveChanges();
                categorias.Add(c);
                return c.Id;
            }

            int n = 0;
            foreach (var fila in tabla.Rows)
            {
                MigracionCorrida.Fila(tabla.Rows.Count);
                n++; int nf = n + 1;
                var fecha = ParsearFecha(V(fila, op.Mapa, "Fecha"));
                var concepto = V(fila, op.Mapa, "Concepto");
                if (fecha == null || concepto == null) { r.Omitidos++; r.Error(nf, "fecha o concepto no válidos."); continue; }

                // Monto: columna con signo + tipo, o columnas separadas de ingresos y egresos
                decimal monto; string tipo;
                var ing = D(fila, op.Mapa, "Ingreso") ?? 0; var egr = D(fila, op.Mapa, "Egreso") ?? 0;
                if (ing != 0 || egr != 0) { tipo = ing >= egr ? "Ingreso" : "Egreso"; monto = Math.Abs(ing >= egr ? ing - egr : egr - ing); }
                else
                {
                    var m = D(fila, op.Mapa, "Monto");
                    if (m == null || m == 0) { r.Omitidos++; r.Error(nf, "sin monto."); continue; }
                    var t = Norm(V(fila, op.Mapa, "Tipo"));
                    tipo = t.Contains("egres") || t.Contains("gast") || t.Contains("salid") || t.Contains("pago") || t == "e" || t == "d" ? "Egreso"
                         : t.Contains("ingres") || t.Contains("entrad") || t.Contains("cobr") || t == "i" || t == "c" ? "Ingreso"
                         : m < 0 ? "Egreso" : "Ingreso";
                    monto = Math.Abs(m.Value);
                }
                monto = Math.Round(monto, 2, MidpointRounding.AwayFromZero);
                if (monto == 0) { r.Omitidos++; r.Error(nf, "monto en cero."); continue; }

                var nomCuenta = V(fila, op.Mapa, "Cuenta");
                var cuenta = nomCuenta == null ? cuentaDefecto
                    : cuentas.FirstOrDefault(c => Norm(c.Nombre) == Norm(nomCuenta) || string.Equals(c.Codigo, nomCuenta, StringComparison.OrdinalIgnoreCase)) ?? cuentaDefecto;

                var numero = NumeroDocumentoHelper.GenerarSiguiente(ctx, "MovimientoFinanciero");
                while (!numerosLote.Add(numero)) numero = SiguienteCodigo(numero);   // los del lote aún no están guardados
                var mov = new MovimientoFinanciero
                {
                    Numero = numero,
                    Tipo = tipo, Fecha = fecha.Value, CuentaId = cuenta.Id, CategoriaId = Categoria(tipo, V(fila, op.Mapa, "Categoria")),
                    Monto = monto, MonedaId = moneda, TipoCambio = 1, Concepto = Recortar(concepto, 200),
                    Referencia = R(fila, op.Mapa, "Referencia", 100), FormaPago = R(fila, op.Mapa, "FormaPago", 50),
                    Origen = OrigenMigracion, EsAutomatico = true, Estado = "Activo", FechaCreacion = DateTime.Now, UsuarioCreoId = usuarioId,
                    // Histórico: cuenta para los informes de esos meses pero no vuelve a mover el saldo (ya está en la apertura)
                    Notas = SaldoHelper.MarcaSoloContable + " Movimiento histórico migrado [" + lote + "]"
                };
                ctx.MovimientosFinancieros.Add(mov); nuevos.Add(mov);
                r.Monto += tipo == "Ingreso" ? monto : -monto;
                r.Creados++;
            }
            ctx.SaveChanges();
            foreach (var m in nuevos) Registrar(ctx, lote, "MovimientoFinanciero", m.Id);
            ctx.SaveChanges();
            if (nuevos.Count > 0) r.Aviso(0, "Los ingresos y egresos históricos aparecen en los informes de sus fechas, pero no cambian el saldo actual de las cajas y bancos.");
        }

        // ---------- Balance de apertura ----------
        public const string CodigoAjusteApertura = "3.9.99";

        private static void SaldosContables(ApplicationDbContext ctx, TablaOrigen tabla, Opciones op, Resultado r, int? usuarioId, string lote)
        {
            if (ctx.RegistrosMigracion.Any(x => x.Entidad == "AsientoContable"))
            { r.YaImportados = tabla.Rows.Count; r.Aviso(0, "Ya se importó un balance de apertura. Deshaz ese lote si quieres cargar otro."); return; }

            var porCodigo = ctx.PlanCuentas.ToList().ToDictionary(p => p.Codigo, StringComparer.OrdinalIgnoreCase);
            var filas = new List<(int nf, string codigo, string? nombre, decimal debe, decimal haber, DateTime? fecha)>();
            int n = 0;
            foreach (var fila in tabla.Rows)
            {
                MigracionCorrida.Fila(tabla.Rows.Count);
                n++; int nf = n + 1;
                var codigo = V(fila, op.Mapa, "Codigo")?.Trim();
                if (codigo == null) { r.Omitidos++; r.Error(nf, "sin código de cuenta."); continue; }
                decimal debe = Math.Round(D(fila, op.Mapa, "Debe") ?? 0, 2, MidpointRounding.AwayFromZero), haber = Math.Round(D(fila, op.Mapa, "Haber") ?? 0, 2, MidpointRounding.AwayFromZero);
                if (debe == 0 && haber == 0 && D(fila, op.Mapa, "Saldo") is decimal saldo && saldo != 0)
                {
                    // Saldo con signo: positivo en el lado normal de la cuenta (deudora → debe, acreedora → haber)
                    var nat = porCodigo.TryGetValue(codigo, out var pc) ? pc.Naturaleza
                            : TipoCuentaContable(V(fila, op.Mapa, "Nombre"), codigo) is "Activo" or "Costo" or "Gasto" ? "Deudora" : "Acreedora";
                    if ((nat == "Deudora") == (saldo > 0)) debe = Math.Round(Math.Abs(saldo), 2, MidpointRounding.AwayFromZero); else haber = Math.Round(Math.Abs(saldo), 2, MidpointRounding.AwayFromZero);
                }
                if (debe < 0) { haber -= debe; debe = 0; }
                if (haber < 0) { debe -= haber; haber = 0; }
                if (debe == 0 && haber == 0) { r.Omitidos++; r.Error(nf, $"la cuenta {codigo} no tiene saldo."); continue; }
                filas.Add((nf, codigo, V(fila, op.Mapa, "Nombre"), debe, haber, ParsearFecha(V(fila, op.Mapa, "Fecha"))));
            }

            // Un balance de comprobación trae las cuentas madre con el total de sus hijas: solo se toman las de detalle
            bool EsMadre(string c) => filas.Any(o => o.codigo.Length > c.Length && o.codigo.StartsWith(c, StringComparison.OrdinalIgnoreCase)
                                                   && (!char.IsLetterOrDigit(o.codigo[c.Length]) || c.All(char.IsLetterOrDigit)));
            var detalle = new List<(int nf, PlanCuenta cuenta, decimal debe, decimal haber)>();
            DateTime? corte = null;
            foreach (var f in filas)
            {
                if (EsMadre(f.codigo)) { r.YaImportados++; continue; }
                // La cuenta del sistema anterior: la que se migró con ese código (MIG.), la de Kirkenta con el mismo
                // código y un nombre compatible, o la de Kirkenta con exactamente ese nombre
                PlanCuenta? cuenta = null;
                if (porCodigo.TryGetValue(PrefijoMigradas + f.codigo, out var migrada)) cuenta = migrada;
                else if (porCodigo.TryGetValue(f.codigo, out var mismoCodigo) && NombresCompatibles(f.nombre, mismoCodigo.Nombre)) cuenta = mismoCodigo;
                else if (f.nombre != null) cuenta = porCodigo.Values.FirstOrDefault(p => p.EsMovimiento && Norm(p.Nombre) == Norm(f.nombre));

                if (cuenta == null)
                {
                    if (f.nombre == null) { r.Omitidos++; r.Error(f.nf, $"la cuenta {f.codigo} no existe en el plan de cuentas y no trae nombre."); continue; }
                    var codigo = porCodigo.ContainsKey(f.codigo) ? PrefijoMigradas + f.codigo : f.codigo;
                    var tipo = TipoCuentaContable(f.nombre, f.codigo);
                    cuenta = new PlanCuenta
                    {
                        Codigo = Recortar(codigo, 20), Nombre = Recortar(f.nombre, 150), Tipo = tipo,
                        Naturaleza = tipo is "Activo" or "Costo" or "Gasto" ? "Deudora" : "Acreedora",
                        Nivel = Math.Max(1, Segmentos(f.codigo).Length), EsMovimiento = true, Activa = true
                    };
                    ctx.PlanCuentas.Add(cuenta); ctx.SaveChanges();
                    porCodigo[cuenta.Codigo] = cuenta;
                    Registrar(ctx, lote, "PlanCuenta", cuenta.Id);
                    r.Aviso(f.nf, codigo == f.codigo
                        ? $"La cuenta {f.codigo} «{f.nombre}» no existía: se creó."
                        : $"La cuenta {f.codigo} «{f.nombre}» no es la {f.codigo} de Kirkenta: se creó como {codigo} para que el contador la reclasifique.");
                }
                else if (!cuenta.EsMovimiento)
                {
                    // Cuenta de resumen en Kirkenta (p. ej. «3.1 Capital»): si tiene una sola subcuenta de detalle compatible
                    // («3.1.01 Capital Social»), el saldo va ahí
                    var madre = cuenta;
                    var hijas = porCodigo.Values.Where(p => p.EsMovimiento && string.Equals(p.CodigoPadre, madre.Codigo, StringComparison.OrdinalIgnoreCase)
                                                            && NombresCompatibles(p.Nombre, f.nombre ?? madre.Nombre)).ToList();
                    if (hijas.Count == 1) cuenta = hijas[0];
                    else r.Aviso(f.nf, $"El saldo de {f.codigo} quedó en {cuenta.Codigo} «{cuenta.Nombre}», que en Kirkenta es una cuenta de resumen: revísalo con el contador.");
                }
                detalle.Add((f.nf, cuenta, f.debe, f.haber));
                corte ??= f.fecha;
            }
            if (detalle.Count == 0) { r.Errores.Add("El balance no trae cuentas con saldo."); return; }

            var totalDebe = detalle.Sum(d => d.debe);
            var totalHaber = detalle.Sum(d => d.haber);
            var asiento = new AsientoContable
            {
                Numero = NumeroDocumentoHelper.GenerarSiguiente(ctx, "AsientoContable"),
                Fecha = (corte ?? DateTime.Today).Date,
                Concepto = "Asiento de apertura: saldos contables del sistema anterior",
                Referencia = "MIGRACION:" + lote, Estado = "Activo", UsuarioId = usuarioId, FechaCreacion = DateTime.Now
            };
            ctx.AsientosContables.Add(asiento);
            ctx.SaveChanges();
            foreach (var d in detalle)
                ctx.DetallesAsiento.Add(new DetalleAsiento { AsientoContableId = asiento.Id, PlanCuentaId = d.cuenta.Id, Debe = d.debe, Haber = d.haber, Detalle = "Saldo inicial" });

            // Si el balance del sistema anterior no cuadra, la diferencia queda a la vista en una cuenta de ajuste
            var dif = totalDebe - totalHaber;
            if (dif != 0)
            {
                if (!porCodigo.TryGetValue(CodigoAjusteApertura, out var ajuste))
                {
                    ajuste = new PlanCuenta { Codigo = CodigoAjusteApertura, Nombre = "Ajuste de apertura por migración", Tipo = "Patrimonio", Naturaleza = "Acreedora", Nivel = 3, EsMovimiento = true, Activa = true };
                    ctx.PlanCuentas.Add(ajuste); ctx.SaveChanges();
                    Registrar(ctx, lote, "PlanCuenta", ajuste.Id);
                }
                ctx.DetallesAsiento.Add(new DetalleAsiento { AsientoContableId = asiento.Id, PlanCuentaId = ajuste.Id, Debe = dif < 0 ? -dif : 0, Haber = dif > 0 ? dif : 0, Detalle = "Diferencia del balance del sistema anterior" });
                r.Aviso(0, $"El balance del sistema anterior no cuadraba (debe L. {totalDebe:N2} y haber L. {totalHaber:N2}): la diferencia de L. {Math.Abs(dif):N2} quedó en la cuenta {CodigoAjusteApertura} «Ajuste de apertura por migración». Revísala con tu contador.");
            }
            ctx.SaveChanges();
            Registrar(ctx, lote, "AsientoContable", asiento.Id);

            // Las cajas y bancos migrados ya están en este balance: su apertura deja de generar asiento propio (no se duplica)
            var cuentasMigradas = ctx.RegistrosMigracion.Where(x => x.Entidad == "CuentaFinanciera").Select(x => x.EntidadId).ToList();
            foreach (var m in ctx.MovimientosFinancieros.Where(m => m.Origen == "Apertura" && cuentasMigradas.Contains(m.CuentaId)).ToList())
            {
                if (m.Notas != null && m.Notas.Contains(AsientoAutomaticoHelper.MarcaSinAsiento)) continue;
                m.Notas = ((m.Notas ?? "") + " " + AsientoAutomaticoHelper.MarcaSinAsiento).Trim();
                Registrar(ctx, lote, "AperturaSinAsiento", m.Id);
            }
            ctx.SaveChanges();
            r.Monto = Math.Max(totalDebe, totalHaber);
            r.Creados = detalle.Count;
        }

        // ================= Numeración fiscal continua =================
        /// <summary>
        /// Si las facturas del sistema anterior usan el mismo rango fiscal (CAI) configurado, la próxima factura de Kirkenta
        /// sigue después de la última emitida allá. Devuelve el texto del ajuste o null.
        /// </summary>
        public static string? ContinuarNumeracionFiscal(ApplicationDbContext ctx, IEnumerable<string> documentos)
        {
            var empresa = ctx.ConfiguracionEmpresa.FirstOrDefault();
            if (empresa == null || string.IsNullOrWhiteSpace(empresa.RangoInicial)) return null;
            if (!FacturacionFiscalHelper.TryParsear(empresa.RangoInicial, out var prefijo, out var inicio, out _)) return null;
            FacturacionFiscalHelper.TryParsear(empresa.RangoFinal, out _, out var fin, out _);
            long max = 0;
            foreach (var d in documentos)
                if (FacturacionFiscalHelper.TryParsear(d?.Trim(), out var pf, out var num, out _) && pf == prefijo && num >= inicio && (fin == 0 || num <= fin))
                    max = Math.Max(max, num);
            if (max == 0 || max + 1 <= empresa.SiguienteNumeroFiscal) return null;
            empresa.SiguienteNumeroFiscal = max + 1;
            ctx.SaveChanges();
            return $"La facturación fiscal continúa después de la última factura del sistema anterior: la próxima será la n.º {max + 1}.";
        }

        // ================= Deshacer =================
        /// <summary>Borra lo registrado en el lote (salvo lo que ya se usó en Kirkenta, que se informa).</summary>
        public static int DeshacerRegistros(ApplicationDbContext ctx, string lote, List<string> conservados)
        {
            int borrados = 0;
            var regs = ctx.RegistrosMigracion.Where(x => x.Lote == lote).ToList();
            // Primero lo que depende de otros (movimientos, cotizaciones), al final cuentas y plan contable
            var orden = new[] { "AperturaSinAsiento", "AsientoContable", "MovimientoFinanciero", "Cotizacion", "Empleado", "MetodoPago", "CuentaFinanciera", "PlanCuenta" };
            foreach (var g in regs.GroupBy(x => x.Entidad).OrderBy(g => Array.IndexOf(orden, g.Key)))
            {
                var ids = g.Select(x => x.EntidadId).ToList();
                switch (g.Key)
                {
                    case "AperturaSinAsiento":
                        // La apertura de la caja vuelve a generar su asiento propio
                        foreach (var m in ctx.MovimientosFinancieros.Where(m => ids.Contains(m.Id)).ToList())
                            m.Notas = m.Notas?.Replace(AsientoAutomaticoHelper.MarcaSinAsiento, "").Trim();
                        break;
                    case "AsientoContable":
                        foreach (var a in ctx.AsientosContables.Where(a => ids.Contains(a.Id)).ToList())
                        {
                            ctx.DetallesAsiento.RemoveRange(ctx.DetallesAsiento.Where(d => d.AsientoContableId == a.Id));
                            ctx.AsientosContables.Remove(a); borrados++;
                        }
                        break;
                    case "MovimientoFinanciero":
                        foreach (var m in ctx.MovimientosFinancieros.Where(m => ids.Contains(m.Id)).ToList())
                        {
                            if (m.Origen == "Apertura")
                            {
                                var c = ctx.CuentasFinancieras.FirstOrDefault(x => x.Id == m.CuentaId);
                                if (c != null && ctx.MovimientosFinancieros.Any(x => x.CuentaId == c.Id && x.Origen != "Apertura" && x.Origen != OrigenMigracion))
                                { conservados.Add($"Apertura de «{c.Nombre}»: la cuenta ya tiene movimientos."); continue; }
                                if (c != null) { c.SaldoActual -= m.Monto; c.SaldoInicial = Math.Max(0, c.SaldoInicial - m.Monto); if (c.SaldoActual < 0) c.SaldoActual = 0; }
                            }
                            ctx.MovimientosFinancieros.Remove(m); borrados++;
                        }
                        break;
                    case "Cotizacion":
                        foreach (var c in ctx.Cotizaciones.Where(c => ids.Contains(c.Id)).ToList())
                        {
                            if (c.VentaId != null || c.FacturaId != null) { conservados.Add($"Cotización {c.Numero}: ya se convirtió en venta."); continue; }
                            ctx.DetalleCotizaciones.RemoveRange(ctx.DetalleCotizaciones.Where(d => d.CotizacionId == c.Id));
                            ctx.Cotizaciones.Remove(c); borrados++;
                        }
                        break;
                    case "Empleado":
                        foreach (var e in ctx.Empleados.Where(e => ids.Contains(e.Id)).ToList())
                        {
                            if (ctx.DetalleNominas.Any(d => d.EmpleadoId == e.Id) || ctx.ValesEmpleado.Any(v => v.EmpleadoId == e.Id))
                            { conservados.Add($"Empleado {e.Codigo}: ya tiene nóminas o vales."); continue; }
                            ctx.Empleados.Remove(e); borrados++;
                        }
                        break;
                    case "CuentaFinanciera":
                        foreach (var c in ctx.CuentasFinancieras.Where(c => ids.Contains(c.Id)).ToList())
                        {
                            if (ctx.MovimientosFinancieros.Any(m => m.CuentaId == c.Id || m.CuentaDestinoId == c.Id))
                            { conservados.Add($"Cuenta «{c.Nombre}»: tiene movimientos."); continue; }
                            ctx.CuentasFinancieras.Remove(c); borrados++;
                        }
                        break;
                    case "MetodoPago":
                        foreach (var m in ctx.MetodosPago.Where(m => ids.Contains(m.Id)).ToList())
                        {
                            if (ctx.Pagos.Any(p => p.MetodoPagoId == m.Id)) { conservados.Add($"Método «{m.Nombre}»: ya se usó en cobros."); continue; }
                            ctx.MetodosPago.Remove(m); borrados++;
                        }
                        break;
                    case "PlanCuenta":
                        foreach (var p in ctx.PlanCuentas.Where(p => ids.Contains(p.Id)).ToList())
                        {
                            if (ctx.CategoriasFinancieras.Any(c => c.PlanCuentaId == p.Id) || ctx.DetallesAsiento.Any(d => d.PlanCuentaId == p.Id))
                            { conservados.Add($"Cuenta contable {p.Codigo}: ya está en uso."); continue; }
                            ctx.PlanCuentas.Remove(p); borrados++;
                        }
                        break;
                }
                ctx.SaveChanges();
            }
            // Las cuentas que se borran arrastran su registro de apertura (ya borrado arriba si estaba en el lote)
            ctx.RegistrosMigracion.RemoveRange(ctx.RegistrosMigracion.Where(x => x.Lote == lote));
            ctx.SaveChanges();
            return borrados;
        }

        public static Dictionary<string, int> ContarRegistros(ApplicationDbContext ctx) =>
            ctx.RegistrosMigracion.AsNoTracking().GroupBy(x => x.Lote).Select(g => new { g.Key, N = g.Count() }).ToList()
               .ToDictionary(x => x.Key, x => x.N);
    }
}
