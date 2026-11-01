using Kirkenta.Data;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Helpers.Import;
using Microsoft.EntityFrameworkCore;
using static Kirkenta.Helpers.Migracion.MigracionHistorialHelper;

namespace Kirkenta.Helpers.Migracion
{
    public class PasoMigracion
    {
        public int Orden { get; set; }
        /// <summary>Clave de catálogo (Productos, Clientes…) o «@ventas», «@cxc», «@cxp», «@kardex». Vacío = no migrar.</summary>
        public string Destino { get; set; } = "";
        public string Titulo { get; set; } = "";
        public int TablaIndice { get; set; }
        public string TablaNombre { get; set; } = "";
        public int Filas { get; set; }
        public bool Incluir { get; set; }
        public string? Motivo { get; set; }
        public string Confianza { get; set; } = "";
        public Dictionary<string, string> Mapa { get; set; } = new();
        public int? DetalleIndice { get; set; }
        public Dictionary<string, string> MapaDetalle { get; set; } = new();
        public bool EsHistorial => Destino.StartsWith("@");
        public string Tipo => Destino.TrimStart('@');
        /// <summary>Uniones por id interno que se hicieron (cliente_id → clientes…).</summary>
        public List<string> Relaciones { get; } = new();
    }

    public class ResultadoPaso
    {
        public PasoMigracion Paso { get; set; } = new();
        public Resultado? Res { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>Una fila del informe «sistema anterior contra Kirkenta».</summary>
    public class LineaConciliacion
    {
        public string Titulo { get; set; } = "";
        public string Tabla { get; set; } = "";
        public int FilasOrigen { get; set; }
        public int Creados { get; set; }
        public int YaExistian { get; set; }
        public int Descartados { get; set; }
        /// <summary>Qué se suma (total vendido, saldo por cobrar…); null si la tabla no tiene montos.</summary>
        public string? Concepto { get; set; }
        public decimal? MontoOrigen { get; set; }
        public decimal? MontoMigrado { get; set; }
        /// <summary>Textos que venían más largos de lo que admite Kirkenta.</summary>
        public int Recortados { get; set; }
        public decimal Diferencia => (MontoOrigen ?? 0) - (MontoMigrado ?? 0);
        public bool FilasCuadran => FilasOrigen == Creados + YaExistian + Descartados;
        public bool MontoCuadra => MontoOrigen == null || Math.Abs(Diferencia) <= 0.05m;
        /// <summary>Cuadra, o la diferencia se explica por lo que ya existía o se descartó.</summary>
        public bool Cuadra => FilasCuadran && (MontoCuadra || YaExistian > 0 || Descartados > 0);
        public string Explicacion =>
            !FilasCuadran ? "Las filas no cuadran: revisa el detalle de este paso."
            : MontoCuadra ? "Cuadra exacto."
            : (YaExistian > 0 || Descartados > 0) ? $"La diferencia corresponde a {YaExistian} registro(s) que ya existían y {Descartados} descartado(s)."
            : "El monto no cuadra: revisa el detalle de este paso.";
    }

    public class HallazgoPosMigracion
    {
        public string Area { get; set; } = "";
        public string Titulo { get; set; } = "";
        public int Total { get; set; }
        public List<string> Ejemplos { get; set; } = new();
    }

    public class ResultadoAutomatico
    {
        /// <summary>Informe de conciliación: cada tabla del sistema anterior contra lo que quedó en Kirkenta.</summary>
        public List<LineaConciliacion> Conciliacion { get; } = new();
        /// <summary>Revisión de integridad corrida sobre el sistema ya migrado (vacía = todo cuadra).</summary>
        public List<HallazgoPosMigracion> Integridad { get; } = new();
        public int RevisionesIntegridad { get; set; }
        /// <summary>Comparación registro por registro de lo guardado contra el origen.</summary>
        public List<LineaVerificacion> Verificacion { get; } = new();
        /// <summary>Formato de fechas y números que se usó en cada columna.</summary>
        public List<FormatoColumna> Formatos { get; set; } = new();
        public bool Aplicado { get; set; }
        public bool SoloRevision { get; set; }
        public string? ErrorFatal { get; set; }
        public string? Respaldo { get; set; }
        public List<ResultadoPaso> Pasos { get; } = new();
        public bool Ok => ErrorFatal == null;
    }

    /// <summary>
    /// Migración automática de punta a punta: detecta qué es cada tabla, la ordena según sus dependencias
    /// (impuestos → unidades → categorías → productos → clientes → proveedores → ventas → saldos → kardex)
    /// y la importa TODA en una sola transacción: o entra completa o no entra nada.
    /// </summary>
    public static class MigracionAutomaticaHelper
    {
        private static int OrdenDe(string destino)
        {
            var i = Array.IndexOf(MigracionCatalogosHelper.Orden, destino);
            if (i >= 0) return i + 1;
            // Catálogos primero (en su orden); después el historial: compras y ventas antes que los saldos sueltos
            return destino switch
            {
                "@compras" => 20, "@ventas" => 21, "@cotizaciones" => 22, "@cxc" => 23, "@cxp" => 24,
                "@movimientos" => 25, "@kardex" => 26, "@saldoscontables" => 27, _ => 99
            };
        }

        public static string TituloDe(string destino) =>
            destino.StartsWith("@") ? MigracionHistorialHelper.NombreTipo(destino.Substring(1)) :
            destino.Length == 0 ? "No migrar" : MigracionCatalogosHelper.Nombre(destino);

        /// <summary>Opciones de destino para el selector de cada tabla.</summary>
        public static readonly string[] Destinos =
        {
            "PlanCuentas", "MetodosPago", "CuentasFinancieras",
            "Impuestos", "UnidadesMedida", "Categorias", "Productos", "Clientes", "Proveedores", "Empleados",
            "@compras", "@ventas", "@cotizaciones", "@cxc", "@cxp", "@movimientos", "@kardex", "@saldoscontables"
        };

        /// <summary>Historiales que llevan una tabla aparte con sus líneas (productos, cantidades, precios).</summary>
        private static readonly string[] ConLineas = { "@ventas", "@compras", "@cotizaciones" };

        public static List<PasoMigracion> Planificar(List<TablaOrigen> tablas, Dictionary<int, string>? elegidos = null)
        {
            var pasos = new List<PasoMigracion>();
            MigracionAplanar.Aplicar(tablas);      // bases relacionales: ids → nombres, número y existencias en su tabla
            var detecciones = MigracionAnalizador.Analizar(tablas);

            // 1) destino sugerido por tabla
            var destino = new string[tablas.Count];
            var motivo = new string?[tablas.Count];
            var confianza = new string[tablas.Count];
            for (int i = 0; i < tablas.Count; i++)
            {
                var d = detecciones[i];
                confianza[i] = d.Confianza;
                if (tablas[i].Unida && (elegidos == null || !elegidos.ContainsKey(i)))
                {
                    destino[i] = "";
                    motivo[i] = tablas[i].Nota;
                    continue;
                }
                if (elegidos != null && elegidos.TryGetValue(i, out var e))
                {
                    destino[i] = e ?? "";
                    confianza[i] = "Elegido";
                    continue;
                }
                if (d.Entidad != null && (d.Confianza == "Alta" || d.Confianza == "Media")) destino[i] = d.Entidad.Key;
                else if (d.TipoHistorial != null) destino[i] = "@" + d.TipoHistorial;
                else
                {
                    destino[i] = "";
                    motivo[i] = d.Entidad != null ? $"Parece «{d.Entidad.Nombre}» pero con poca seguridad: elige el destino si quieres migrarla."
                        : d.PosibleFuturo ?? tablas[i].Nota ?? "No se reconoció qué contiene.";
                }
            }

            // 2) tablas de líneas (ventas, compras, cotizaciones): se unen a su documento, no se importan solas
            var detalleDe = new Dictionary<int, int>();     // documento → detalle
            for (int i = 0; i < tablas.Count; i++)
            {
                if (!ConLineas.Contains(destino[i])) continue;
                var det = SugerirTablaDetalle(tablas, i, destino[i].TrimStart('@'));
                if (det != null && !detalleDe.ContainsValue(det.Value)) detalleDe[i] = det.Value;
            }
            foreach (var par in detalleDe)
            {
                if (elegidos != null && elegidos.ContainsKey(par.Value)) continue;
                destino[par.Value] = "";
                motivo[par.Value] = $"Líneas de documento: se unen a «{tablas[par.Key].Nombre}».";
            }

            // 3) si la tabla de ventas ya trae el saldo pendiente, la tabla de cuentas por cobrar sobra
            bool ventasConSaldo = Enumerable.Range(0, tablas.Count).Any(i => destino[i] == "@ventas"
                && MigracionHistorialHelper.Detectar(tablas[i].Headers, Campos(TipoVentas))["Saldo"] != null);

            // 4) armar pasos
            for (int i = 0; i < tablas.Count; i++)
            {
                var p = new PasoMigracion
                {
                    TablaIndice = i, TablaNombre = tablas[i].Nombre, Filas = tablas[i].Rows.Count,
                    Destino = destino[i], Titulo = TituloDe(destino[i]), Confianza = confianza[i],
                    Orden = OrdenDe(destino[i]), Motivo = motivo[i]
                };

                if (destino[i].Length == 0) { pasos.Add(p); continue; }

                if (p.EsHistorial)
                {
                    var tipo = p.Tipo;
                    // compras, cotizaciones, movimientos: sus propias columnas y reglas
                    p.Mapa = MigracionOperacionesHelper.EsHistorial(tipo)
                        ? MigracionOperacionesHelper.Detectar(tablas[i].Headers, tipo)
                        : MigracionHistorialHelper.Detectar(tablas[i].Headers, Campos(tipo))
                            .Where(x => x.Value != null).ToDictionary(x => x.Key, x => x.Value!);
                    // documento sin número propio: su id es el número (así se unen también sus líneas)
                    if (Campos(tipo).Any(c => c.Key == "Documento") && !p.Mapa.ContainsKey("Documento")
                        && MigracionAplanar.ColumnaId(tablas[i]) is string colId)
                    {
                        p.Mapa["Documento"] = colId;
                        p.Relaciones.Add($"Sin número de documento: se usa su id («{colId}»)");
                    }
                    var falta = Faltan(p);

                    if (ConLineas.Contains(destino[i]) && detalleDe.TryGetValue(i, out var di))
                    {
                        p.DetalleIndice = di;
                        p.MapaDetalle = MigracionHistorialHelper.Detectar(tablas[di].Headers, CamposDetalle())
                            .Where(x => x.Value != null).ToDictionary(x => x.Key, x => x.Value!);
                        // en las compras el «costo» de la línea es su precio
                        if (!p.MapaDetalle.ContainsKey("Precio") && p.MapaDetalle.TryGetValue("Costo", out var colCosto))
                            p.MapaDetalle["Precio"] = colCosto;
                    }

                    if (falta.Count > 0) p.Motivo = SinColumna + string.Join(", ", falta) + ".";
                    else if (tipo == TipoCxC && ventasConSaldo)
                        p.Motivo = "Los saldos pendientes ya vienen en la tabla de ventas; esta se omite para no duplicar.";
                    else p.Incluir = true;
                }
                else
                {
                    p.Mapa = MigracionCatalogosHelper.Detectar(tablas[i].Headers, destino[i]);
                    var falta = Faltan(p);
                    if (falta.Count > 0) p.Motivo = SinColumna + string.Join(", ", falta) + ".";
                    else p.Incluir = true;
                }
                pasos.Add(p);
            }

            // 5) tablas unidas por su id interno (cliente_id, producto_id, venta_id…)
            MigracionRelaciones.Resolver(tablas, pasos);
            foreach (var p in pasos.Where(p => ConLineas.Contains(p.Destino))) TotalDesdeLineas(tablas, p);
            foreach (var p in pasos.Where(p => !p.Incluir && p.Motivo?.StartsWith(SinColumna) == true && Faltan(p).Count == 0))
            { p.Incluir = true; p.Motivo = null; }

            return pasos.OrderBy(p => p.Orden).ThenBy(p => p.TablaIndice).ToList();
        }

        /// <summary>
        /// Ejecuta el plan. aplicar=false: lo corre completo y deshace al final (revisión exacta, no guarda nada).
        /// aplicar=true: guarda todo junto; si algo truena no queda nada.
        /// </summary>
        /// <summary>Columna(s) que se suman en el origen para comparar con lo migrado.</summary>
        private static (string concepto, Func<Dictionary<string, string>, decimal?> valor)? QueSumar(PasoMigracion p)
        {
            decimal? Num(Dictionary<string, string> f, string campo) => ParserHelper.ParsearDecimal(Valor(f, p.Mapa, campo));
            return p.Destino switch
            {
                "@ventas" => ("Total vendido", f => Num(f, "Total")),
                "@compras" => ("Total comprado", f => Num(f, "Total")),
                "@cotizaciones" => ("Total cotizado", f => Num(f, "Total")),
                "@cxc" => ("Saldo por cobrar", f => Num(f, "Saldo")),
                "@cxp" => ("Saldo por pagar", f => Num(f, "Saldo")),
                "CuentasFinancieras" => ("Saldo en cajas y bancos", f => Math.Max(0, Num(f, "Saldo") ?? 0)),
                "Empleados" => ("Planilla mensual (salarios)", f => Math.Max(0, Num(f, "Salario") ?? 0)),
                "Productos" => ("Inventario valorizado (existencia × costo)", f => Math.Max(0, Num(f, "Stock") ?? 0) * (Num(f, "PrecioCompra") ?? 0)),
                _ => null
            };
        }

        private static LineaConciliacion Conciliar(PasoMigracion paso, TablaOrigen tabla, Resultado r)
        {
            var l = new LineaConciliacion
            {
                Titulo = paso.Titulo, Tabla = paso.TablaNombre, FilasOrigen = tabla.Rows.Count,
                Creados = r.Creados, YaExistian = r.YaImportados,
                Descartados = Math.Max(r.DescartadasTotal, r.Omitidos)
            };
            var suma = QueSumar(paso);
            if (suma != null)
            {
                l.Concepto = suma.Value.concepto;
                l.MontoOrigen = Math.Round(tabla.Rows.Sum(f => suma.Value.valor(f) ?? 0m), 2);
                l.MontoMigrado = Math.Round(r.Monto, 2);
            }
            // Las ventas con su detalle pueden dejar filas fuera del conteo cuando el mismo documento viene repetido
            if (!l.FilasCuadran && l.FilasOrigen > l.Creados + l.YaExistian + l.Descartados && paso.Destino == "@ventas")
                l.YaExistian = l.FilasOrigen - l.Creados - l.Descartados;
            return l;
        }

        private static void RevisarIntegridad(ApplicationDbContext ctx, ResultadoAutomatico res)
        {
            var desde = new DateTime(1990, 1, 1);
            var hasta = DateTime.Today.AddDays(2);
            for (int i = 0; i < IntegridadHelper.TotalPasos; i++)
            {
                var rev = IntegridadHelper.EjecutarPaso(ctx, i, desde, hasta);
                res.RevisionesIntegridad++;
                if (rev.Total > 0)
                    res.Integridad.Add(new HallazgoPosMigracion
                    {
                        Area = rev.Area, Titulo = rev.Titulo, Total = rev.Total,
                        Ejemplos = rev.Ejemplos.Take(5).Select(e => $"{e.Documento}: {e.Detalle}").ToList()
                    });
            }
            ctx.ChangeTracker.Clear();
        }

        private const string SinColumna = "No se encontró la columna: ";

        /// <summary>Documento sin columna de total: se calcula sumando sus líneas (cantidad × precio − descuento).</summary>
        private static void TotalDesdeLineas(List<TablaOrigen> tablas, PasoMigracion p)
        {
            if (p.Mapa.ContainsKey("Total") || p.DetalleIndice == null || !p.Mapa.TryGetValue("Documento", out var colDoc)) return;
            var md = p.MapaDetalle;
            if (!md.ContainsKey("Documento") || !md.ContainsKey("Cantidad") || !md.ContainsKey("Precio")) return;
            var suma = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in tablas[p.DetalleIndice.Value].Rows)
            {
                var doc = Valor(f, md, "Documento");
                if (doc == null) continue;
                decimal linea;
                if (ParserHelper.ParsearDecimal(Valor(f, md, "TotalLinea")) is decimal tl) linea = tl;
                else
                {
                    var cant = ParserHelper.ParsearDecimal(Valor(f, md, "Cantidad"));
                    var precio = ParserHelper.ParsearDecimal(Valor(f, md, "Precio"));
                    if (cant == null || precio == null || cant <= 0) continue;
                    linea = Math.Round(cant.Value * precio.Value, 2) - (ParserHelper.ParsearDecimal(Valor(f, md, "Descuento")) ?? 0);
                }
                suma[doc] = suma.GetValueOrDefault(doc) + linea;
            }
            if (suma.Count == 0) return;
            var tabla = tablas[p.TablaIndice];
            const string col = "total (suma de sus líneas)";
            if (!tabla.Headers.Contains(col)) tabla.Headers.Add(col);
            foreach (var f in tabla.Rows)
                f[col] = f.TryGetValue(colDoc, out var d) && d != null && suma.TryGetValue(d.Trim(), out var t)
                    ? t.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) : "";
            p.Mapa["Total"] = col;
            p.Relaciones.Add("Total calculado sumando sus líneas");
        }

        private static List<string> Faltan(PasoMigracion p)
        {
            if (!p.EsHistorial) return MigracionCatalogosHelper.Faltantes(p.Destino, p.Mapa);
            var tipo = p.Tipo;
            if (MigracionOperacionesHelper.EsHistorial(tipo)) return MigracionOperacionesHelper.Faltantes(tipo, p.Mapa);
            var falta = Campos(tipo).Where(c => c.Requerido && !p.Mapa.ContainsKey(c.Key)).Select(c => c.Label).ToList();
            if (tipo is TipoCxC or TipoCxP && !new[] { "Cliente", "ClienteCodigo", "ClienteRTN", "Proveedor", "ProveedorCodigo", "ProveedorRTN" }.Any(p.Mapa.ContainsKey))
                falta.Add("cliente/proveedor");
            if (tipo == TipoKardex && !p.Mapa.ContainsKey("ProductoCodigo") && !p.Mapa.ContainsKey("ProductoNombre"))
                falta.Add("producto");
            return falta;
        }

        public static ResultadoAutomatico Ejecutar(ApplicationDbContext ctx, List<TablaOrigen> tablas,
            List<PasoMigracion> plan, int? usuarioId, bool aplicar,
            Action<int, string>? progreso = null, string formatoFecha = "dmy")
        {
            var res = new ResultadoAutomatico { SoloRevision = !aplicar };

            if (!aplicar && !ctx.Database.IsRelational())
            {
                res.ErrorFatal = "La revisión completa necesita la base de datos del sistema (no está disponible en este entorno).";
                return res;
            }

            // Fechas y números: un solo formato por columna, decidido con todos sus valores
            res.Formatos = MigracionFormatos.Normalizar(tablas, plan, formatoFecha);

            var pasos = plan.Where(p => p.Incluir).OrderBy(p => p.Orden).ThenBy(p => p.TablaIndice).ToList();
            double totalFilas = Math.Max(1, pasos.Sum(p => (double)p.Filas + (p.DetalleIndice.HasValue ? tablas[p.DetalleIndice.Value].Rows.Count : 0)));
            double hechas = 0, peso = 0;
            string etapa = "";
            int Pct(double filas) => (int)Math.Min(95, filas / totalFilas * 95);
            MigracionCorrida.Iniciar(progreso == null ? null : frac => progreso(Pct(hechas + frac * peso), etapa));

            var propia = ctx.Database.CurrentTransaction == null;
            using var tx = propia ? ctx.Database.BeginTransaction() : null;
            PasoMigracion? actual = null;
            try
            {
                foreach (var paso in pasos)
                {
                    actual = paso;
                    var tabla = tablas[paso.TablaIndice];
                    var rp = new ResultadoPaso { Paso = paso };
                    peso = paso.Filas + (paso.DetalleIndice.HasValue ? tablas[paso.DetalleIndice.Value].Rows.Count : 0);
                    etapa = $"{paso.Titulo} ({paso.TablaNombre})";
                    MigracionCorrida.NuevoPaso();
                    progreso?.Invoke(Pct(hechas), etapa);
                    var marca = MigracionVerificacion.Marcar(ctx, paso);

                    if (paso.EsHistorial)
                    {
                        var op = new Opciones
                        {
                            Tipo = paso.Tipo, Mapa = paso.Mapa, Tolerante = true, TerceroNoExiste = "crear",
                            Detalle = paso.DetalleIndice.HasValue ? tablas[paso.DetalleIndice.Value] : null,
                            MapaDetalle = paso.MapaDetalle
                        };
                        rp.Res = Procesar(ctx, tabla, op, true, usuarioId);

                        // La próxima factura fiscal continúa después de la última del sistema anterior (mismo CAI)
                        if (paso.Tipo == TipoVentas && paso.Mapa.TryGetValue("Documento", out var colDoc))
                        {
                            var aviso = MigracionOperacionesHelper.ContinuarNumeracionFiscal(ctx,
                                tabla.Rows.Select(f => f.TryGetValue(colDoc, out var d) ? d : "").Where(d => !string.IsNullOrWhiteSpace(d)));
                            if (aviso != null) rp.Res.Advertencias.Insert(0, aviso);
                        }
                    }
                    else
                    {
                        rp.Res = MigracionCatalogosHelper.Procesar(ctx, tabla, paso.Destino, paso.Mapa, usuarioId, true);
                    }
                    res.Pasos.Add(rp);
                    ctx.ChangeTracker.Clear();

                    // Nada se recorta en silencio; y lo guardado se vuelve a leer y se compara con el origen
                    var recortes = MigracionCorrida.TomarRecortes();
                    if (rp.Res != null)
                    {
                        var c = Conciliar(paso, tabla, rp.Res);
                        c.Recortados = recortes.Count;
                        res.Conciliacion.Add(c);
                        if (recortes.Count > 0)
                            rp.Res.Advertencias.Insert(0, $"{recortes.Count} texto(s) eran más largos de lo que admite Kirkenta y se recortaron (detalle en la verificación).");
                    }
                    var ver = MigracionVerificacion.Verificar(ctx, tabla, paso, marca)
                              ?? (recortes.Count > 0 ? new LineaVerificacion { Titulo = paso.Titulo, Tabla = paso.TablaNombre } : null);
                    if (ver != null)
                    {
                        ver.Recortados = recortes.Count;
                        foreach (var (campo, texto) in recortes.Take(20))
                            ver.Diferencias.Add($"Texto recortado en {campo}: «{texto}»");
                        res.Verificacion.Add(ver);
                    }
                    hechas += peso;
                }

                // Integridad del sistema ya migrado (dentro de la misma transacción: en la revisión también se ve)
                progreso?.Invoke(96, "Revisión de integridad");
                RevisarIntegridad(ctx, res);
                progreso?.Invoke(99, aplicar ? "Guardando" : "Deshaciendo la revisión");

                if (aplicar) { tx?.Commit(); res.Aplicado = true; }
                else tx?.Rollback();
            }
            catch (Exception ex)
            {
                tx?.Rollback();
                ctx.ChangeTracker.Clear();
                res.Aplicado = false;
                res.ErrorFatal = $"Se canceló todo y NO se guardó nada. Falló el paso «{actual?.Titulo} ({actual?.TablaNombre})»: {ex.InnerException?.Message ?? ex.Message}";
            }
            finally { MigracionCorrida.Terminar(); }
            ctx.ChangeTracker.Clear();
            return res;
        }
    }
}
