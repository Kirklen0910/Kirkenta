using Kirkenta.Helpers.Import;

namespace Kirkenta.Helpers.Migracion
{
    /// <summary>
    /// Adivina a qué módulo de Kirkenta corresponde cada tabla del sistema viejo, mirando el nombre
    /// de la tabla y sus columnas. No importa nada: solo sugiere. El mapeo fino de columnas lo hace
    /// después el asistente de importación de cada módulo (que el usuario revisa).
    /// </summary>
    public static class MigracionAnalizador
    {
        public static readonly List<EntidadMigracion> Entidades = new()
        {
            new EntidadMigracion
            {
                Key = "Impuestos", Nombre = "Impuestos", Orden = 1,
                UrlImportMap = "/Impuestos/ImportMap", SessionKey = "Impuestos",
                NombresTabla = new[] { "impuestos", "impuesto", "taxes", "tax", "tasasimpuesto", "isv", "iva" },
                Requeridos = new[] { "Nombre", "Porcentaje" },
                Distintivos = new[] { "Porcentaje" },
                Alias = new()
                {
                    { "Nombre", new[] { "nombre", "name", "impuesto", "tax" } },
                    { "Porcentaje", new[] { "porcentaje", "tasa", "rate", "isv", "porcent" } },
                    { "Descripcion", new[] { "descripcion", "description", "detalle" } },
                    { "Activo", new[] { "activo", "active", "estado" } },
                }
            },
            new EntidadMigracion
            {
                Key = "UnidadesMedida", Nombre = "Unidades de medida", Orden = 2,
                UrlImportMap = "/UnidadesMedida/ImportMap", SessionKey = "UnidadesMedida",
                NombresTabla = new[] { "unidadesmedida", "unidadmedida", "unidades", "unidad", "units", "uom", "medidas" },
                Requeridos = new[] { "Nombre" },
                Distintivos = new[] { "Abreviatura" },
                Alias = new()
                {
                    { "Nombre", new[] { "nombre", "name", "unidad", "medida" } },
                    { "Abreviatura", new[] { "abreviatura", "abrev", "abbr", "sigla", "short" } },
                    { "Descripcion", new[] { "descripcion", "description", "detalle" } },
                    { "Activa", new[] { "activa", "activo", "active", "estado" } },
                }
            },
            new EntidadMigracion
            {
                Key = "Categorias", Nombre = "Categorías", Orden = 3,
                UrlImportMap = "/Categorias/ImportMap", SessionKey = "Categorias",
                NombresTabla = new[] { "categorias", "categoria", "categories", "category", "familias", "familia", "rubros", "lineas", "grupos" },
                Requeridos = new[] { "Nombre" },
                Distintivos = new[] { "Color" },
                Alias = new()
                {
                    { "Nombre", new[] { "nombre", "name", "categoria", "rubro", "familia" } },
                    { "Descripcion", new[] { "descripcion", "description", "detalle" } },
                    { "Color", new[] { "color", "hex" } },
                    { "Activa", new[] { "activa", "activo", "active", "estado" } },
                }
            },
            new EntidadMigracion
            {
                Key = "Productos", Nombre = "Productos / inventario", Orden = 4,
                UrlImportMap = "/Productos/ImportMap", SessionKey = "Productos",
                NombresTabla = new[] { "productos", "producto", "products", "product", "articulos", "articulo", "items", "item", "inventario", "mercaderia", "existencias" },
                Requeridos = new[] { "Nombre" },
                Distintivos = new[] { "PrecioVenta", "Stock", "SKU", "CodigoBarras", "PrecioCompra" },
                Alias = new()
                {
                    { "SKU", new[] { "sku", "codigo", "code", "cod", "clave", "codigoproducto", "codigoarticulo", "codart", "codprod", "referencia", "ref", "itemcode", "partnumber", "idproducto", "codigointerno" } },
                    { "CodigoBarras", new[] { "codigobarras", "barras", "barcode", "ean", "upc" } },
                    { "Nombre", new[] { "nombre", "name", "producto", "articulo", "descripcion", "descrip", "desc", "detalle", "nombreproducto", "descripcionarticulo", "itemname" } },
                    { "Categoria", new[] { "categoria", "category", "rubro", "familia", "linea" } },
                    { "UnidadMedida", new[] { "unidadmedida", "unidad", "um", "unit" } },
                    { "PrecioCompra", new[] { "preciocompra", "costo", "cost", "preciocosto", "costopromedio", "costounitario", "costoultimo", "ultimocosto", "preciodecompra", "pcompra", "costoprom" } },
                    { "PrecioVenta", new[] { "precioventa", "pvp", "precio", "price", "preciov", "precio1", "preciodeventa", "pventa", "preciopublico", "precionormal", "preciolista", "saleprice" } },
                    { "PrecioMayorista", new[] { "preciomayorista", "mayorista", "wholesale", "precio2" } },
                    { "Stock", new[] { "stock", "existencia", "cantidad", "qty", "disponible", "existencias", "saldo", "inventarioactual", "stockactual", "onhand", "cantidadactual" } },
                    { "StockMinimo", new[] { "stockminimo", "minimo", "minstock", "existenciaminima", "puntoreorden", "reorden" } },
                    { "Impuesto", new[] { "impuesto", "tax", "iva", "isv" } },
                }
            },
            new EntidadMigracion
            {
                Key = "Clientes", Nombre = "Clientes", Orden = 5,
                UrlImportMap = "/Clientes/ImportMap", SessionKey = "Clientes",
                NombresTabla = new[] { "clientes", "cliente", "customers", "customer", "clients", "client", "deudores" },
                Requeridos = new[] { "Nombre" },
                Distintivos = new[] { "LimiteCredito", "DiasCredito", "TipoCliente" },
                Alias = new()
                {
                    { "Codigo", new[] { "codigo", "code", "cod", "clave", "idcliente", "codigocliente", "codcliente", "nocliente", "numerocliente", "customerid" } },
                    { "Nombre", new[] { "nombre", "name", "cliente", "razonsocial", "nombrecliente", "nombrecompleto", "customername", "nomcliente" } },
                    { "RTN", new[] { "rtn", "ruc", "nit", "taxid", "identificacion", "numerorfc", "rfc", "dni", "cedula", "identidad", "documento" } },
                    { "Email", new[] { "email", "correo", "mail" } },
                    { "Telefono", new[] { "telefono", "tel", "phone", "celular", "movil" } },
                    { "Direccion", new[] { "direccion", "address", "domicilio" } },
                    { "Ciudad", new[] { "ciudad", "city", "municipio" } },
                    { "TipoCliente", new[] { "tipocliente", "tipo", "type" } },
                    { "DiasCredito", new[] { "diascredito", "plazo", "creditodias" } },
                    { "LimiteCredito", new[] { "limitecredito", "limite", "creditlimit", "limitedecredito" } },
                }
            },
            new EntidadMigracion
            {
                Key = "Proveedores", Nombre = "Proveedores", Orden = 6,
                UrlImportMap = "/Proveedores/ImportMap", SessionKey = "Proveedores",
                NombresTabla = new[] { "proveedores", "proveedor", "suppliers", "supplier", "vendors", "vendor", "acreedores" },
                Requeridos = new[] { "Nombre" },
                Distintivos = new[] { "Contacto", "TelefonoContacto" },
                Alias = new()
                {
                    { "Codigo", new[] { "codigo", "code", "cod", "clave", "idproveedor", "codigoproveedor", "codproveedor", "noproveedor", "vendorid" } },
                    { "Nombre", new[] { "nombre", "name", "proveedor", "razonsocial", "nombreproveedor", "vendorname", "nomproveedor" } },
                    { "RTN", new[] { "rtn", "ruc", "nit", "taxid", "rfc", "identificacion", "documento" } },
                    { "Contacto", new[] { "contacto", "contact", "vendedor", "representante" } },
                    { "TelefonoContacto", new[] { "telefonocontacto", "telcontacto", "celularcontacto" } },
                    { "Telefono", new[] { "telefono", "tel", "phone", "celular", "movil" } },
                    { "Email", new[] { "email", "correo", "mail" } },
                    { "Direccion", new[] { "direccion", "address", "domicilio" } },
                    { "Ciudad", new[] { "ciudad", "city", "municipio" } },
                    { "DiasCredito", new[] { "diascredito", "plazo", "creditodias" } },
                }
            },
            DeOperaciones("PlanCuentas", 7,
                new[] { "plancuentas", "plandecuentas", "catalogocuentas", "catalogodecuentas", "cuentascontables", "chartofaccounts", "nomenclatura", "plancontable" },
                new[] { "Naturaleza", "Padre", "Movimiento" }),
            DeOperaciones("MetodosPago", 8,
                new[] { "metodospago", "metodosdepago", "formaspago", "formasdepago", "formapago", "paymentmethods", "tipospago", "mediospago" },
                new[] { "Tipo" }),
            DeOperaciones("CuentasFinancieras", 9,
                new[] { "cuentasbancarias", "bancos", "cajas", "cuentasfinancieras", "cuentasbanco", "bankaccounts", "tesoreria", "cajaybancos" },
                new[] { "NumeroCuenta", "Banco", "Saldo" }),
            DeOperaciones("Empleados", 10,
                new[] { "empleados", "empleado", "employees", "employee", "personal", "colaboradores", "trabajadores", "planilla", "staff", "rrhh" },
                new[] { "Salario", "FechaIngreso", "Cedula", "Puesto" }),
        };

        /// <summary>Entidad de detección para los catálogos de MigracionOperacionesHelper (se importan con la migración automática).</summary>
        private static EntidadMigracion DeOperaciones(string key, int orden, string[] nombresTabla, string[] distintivos)
        {
            var campos = MigracionOperacionesHelper.Campos(key);
            return new EntidadMigracion
            {
                Key = key, Nombre = MigracionOperacionesHelper.Nombre(key), Orden = orden,
                NombresTabla = nombresTabla, Distintivos = distintivos,
                // El empleado puede traer el nombre en una sola columna: no se exige «Nombres» para reconocer la tabla
                Requeridos = key == "Empleados" ? Array.Empty<string>() : campos.Where(c => c.Requerido).Select(c => c.Key).ToArray(),
                Alias = campos.ToDictionary(c => c.Key, c => c.Alias)
            };
        }

        /// <summary>Tablas de historial que reconocemos por su nombre (Tipo = herramienta de la etapa 2).</summary>
        private static readonly (string[] Claves, string Texto, string? Tipo)[] Historiales =
        {
            (new[] { "backup", "respaldo", "bitacora", "auditoria", "logs", "sesion", "migration", "migracion" }, "Copia o registro técnico del sistema anterior: no se migra", null),
            (new[] { "detalle", "linea", "renglon", "item", "partida" }, "Líneas de documentos: se unen a su venta, compra o cotización", null),
            (new[] { "vacacion", "nomina", "asistencia", "permisoempleado", "planillapago", "incapacidad" }, "Historial de RRHH (nóminas, vacaciones): se migra el saldo dentro de «Empleados»", null),
            (new[] { "cuentasporcobrar", "cxc", "saldocliente", "receivable", "abonocliente", "carteraclientes" }, "Saldos por cobrar", MigracionHistorialHelper.TipoCxC),
            (new[] { "cuentasporpagar", "cxp", "saldoproveedor", "payable", "carteraproveedor" }, "Saldos por pagar", MigracionHistorialHelper.TipoCxP),
            (new[] { "kardex", "movimientoinv", "movimientosinv", "inventariomov", "movinv", "stockmove" }, "Movimientos de inventario (histórico)", MigracionHistorialHelper.TipoKardex),
            // Compras, cotizaciones y movimientos antes que ventas: «facturascompra» o «facturaproveedor» son compras
            (new[] { "compra", "purchase", "ordencompra", "facturaproveedor", "ingresomercaderia" }, "Historial de compras", MigracionOperacionesHelper.TipoCompras),
            (new[] { "cotizacion", "presupuesto", "proforma", "quote", "quotation" }, "Cotizaciones", MigracionOperacionesHelper.TipoCotizaciones),
            (new[] { "gasto", "egreso", "ingresoscaja", "movimientoscaja", "movimientosbanco", "movimientofinanciero", "movimientosfinanciero", "librocaja", "librobanco", "transaccion", "expense" }, "Ingresos y egresos históricos", MigracionOperacionesHelper.TipoMovimientos),
            (new[] { "balanza", "balancecomprobacion", "balancedecomprobacion", "saldoscontables", "saldoscuentas", "trialbalance", "aperturacontable", "balanceinicial", "balancegeneral" }, "Balance de apertura (saldos contables)", MigracionOperacionesHelper.TipoSaldosContables),
            (new[] { "venta", "factura", "sales", "invoice", "ticket" }, "Historial de ventas", MigracionHistorialHelper.TipoVentas),
            (new[] { "usuario", "user", "login" }, "Usuarios del sistema (se crean manualmente por seguridad: las contraseñas no se pueden migrar)", null),
        };

        public static List<DeteccionTabla> Analizar(IEnumerable<TablaOrigen> tablas)
        {
            var resultado = new List<DeteccionTabla>();
            int idx = 0;
            foreach (var t in tablas)
            {
                var det = AnalizarTabla(t);
                det.Indice = idx++;
                resultado.Add(det);
            }
            return resultado;
        }

        public static DeteccionTabla AnalizarTabla(TablaOrigen tabla)
        {
            var det = new DeteccionTabla { Tabla = tabla };
            var nombreNorm = ColumnMapper.Normalizar(tabla.Nombre);

            EntidadMigracion? mejor = null;
            int mejorPuntaje = 0;
            Dictionary<string, string?> mejorMapa = new();

            foreach (var e in Entidades)
            {
                var mapa = ColumnMapper.Detectar(tabla.Headers, e.Alias);
                CompletarPorContenido(e, tabla, mapa);
                int puntaje = Puntuar(e, nombreNorm, mapa);
                if (puntaje > mejorPuntaje)
                {
                    mejorPuntaje = puntaje; mejor = e; mejorMapa = mapa;
                }
            }

            // «inventario» contiene «venta»: para esa palabra se busca sin «inventario»
            // «inventario» contiene «venta» y «sucursales» contiene «sales»
            var sinFalsos = nombreNorm.Replace("inventario", "~").Replace("sucursal", "~");
            var hist = Historiales.FirstOrDefault(f => f.Claves.Any(c => (c is "venta" or "sales" ? sinFalsos : nombreNorm).Contains(c)));
            if (hist.Texto != null)
            {
                det.PosibleFuturo = hist.Texto;
                det.TipoHistorial = hist.Tipo;
            }

            if (mejor != null && mejorPuntaje >= 30)
            {
                det.Entidad = mejor;
                det.Puntaje = mejorPuntaje;
                det.Mapa = mejorMapa;
                det.Confianza = mejorPuntaje >= 60 ? "Alta" : mejorPuntaje >= 40 ? "Media" : "Baja";
                // Si el nombre apunta claramente a otra cosa ("ventas", "movimientos_inventario", "copia de…"),
                // manda el nombre, salvo que la tabla se llame exactamente como la entidad
                bool nombreExacto = mejor.NombresTabla.Contains(nombreNorm);
                if (det.PosibleFuturo != null && (!nombreExacto || mejorPuntaje < 60)) { det.Entidad = null; det.Confianza = "Sin sugerencia"; }
            }
            return det;
        }

        private static int Puntuar(EntidadMigracion e, string nombreTablaNorm, Dictionary<string, string?> mapa)
        {
            double puntaje = 0;

            if (nombreTablaNorm.Length > 0)
            {
                if (e.NombresTabla.Any(n => n == nombreTablaNorm)) puntaje += 45;
                else if (e.NombresTabla.Any(n => n.Length >= 4 && nombreTablaNorm.Contains(n))) puntaje += 30;
            }

            int encontrados = mapa.Count(p => p.Value != null);
            puntaje += 35.0 * encontrados / Math.Max(1, e.Alias.Count);

            int distintivos = e.Distintivos.Count(d => mapa.TryGetValue(d, out var v) && v != null);
            if (e.Distintivos.Length > 0)
                puntaje += 20.0 * Math.Min(1.0, distintivos / (double)Math.Min(2, e.Distintivos.Length));

            bool faltaRequerido = e.Requeridos.Any(r => !mapa.TryGetValue(r, out var v) || v == null);
            if (faltaRequerido) puntaje *= 0.4;

            // Catálogos con pocas columnas (impuestos, unidades, categorías, métodos de pago) se parecen a cualquier
            // tabla con «nombre» y «tipo» (feriados, monedas, roles…): sin un nombre de tabla que coincida no se sugieren.
            bool nombreCoincide = nombreTablaNorm.Length > 0 && e.NombresTabla.Any(n => n == nombreTablaNorm || (n.Length >= 4 && nombreTablaNorm.Contains(n)));
            if (e.Alias.Count <= 4 && !nombreCoincide) puntaje = Math.Min(puntaje, 35);

            return (int)Math.Round(puntaje);
        }

        /// <summary>
        /// Adaptación dinámica: cuando el nombre de una columna no dice nada (c1, campo7, x_tel…) se mira el contenido
        /// de sus datos para reconocer correos, RTN/identidad y teléfonos. El usuario siempre revisa el mapeo antes de importar.
        /// </summary>
        public static void CompletarPorContenido(EntidadMigracion e, TablaOrigen t, Dictionary<string, string?> mapa)
        {
            if (t.Rows.Count < 3) return;
            var usados = new HashSet<string>(mapa.Values.Where(v => v != null).Select(v => v!), StringComparer.OrdinalIgnoreCase);
            var muestra = t.Rows.Take(200).ToList();

            void Probar(string campo, Func<string, bool> ok)
            {
                if (!e.Alias.ContainsKey(campo) || (mapa.TryGetValue(campo, out var actual) && actual != null)) return;
                foreach (var h in t.Headers.Where(h => !usados.Contains(h)))
                {
                    var vals = muestra.Select(r => r.TryGetValue(h, out var x) ? x : "").Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                    if (vals.Count < 3) continue;
                    if (vals.Count(ok) >= 0.8 * vals.Count) { mapa[campo] = h; usados.Add(h); return; }
                }
            }

            static string Digitos(string s) => new(s.Where(char.IsDigit).ToArray());
            Probar("Email", s => System.Text.RegularExpressions.Regex.IsMatch(s.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"));
            Probar("RTN", s => s.All(c => char.IsDigit(c) || c == '-' || c == ' ') && Digitos(s).Length is 13 or 14);
            Probar("Telefono", s => s.All(c => char.IsDigit(c) || "-+() ".Contains(c)) && Digitos(s).Length is >= 7 and <= 12);
        }

        public static EntidadMigracion? BuscarEntidad(string? key) =>
            Entidades.FirstOrDefault(e => string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase));
    }
}
