using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers.Finanzas
{
    /// <summary>
    /// Genera reportes contables (Estado de Resultados y Balance General)
    /// agrupando MovimientoFinanciero por la cuenta del Plan de Cuentas
    /// asociada a su CategoriaFinanciera.
    ///
    /// REGLAS CONTABLES APLICADAS (Opción A):
    ///   - Solo se registran movimientos cuando entra/sale dinero real.
    ///   - La CxC NO es un ingreso devengado: es un saldo en Venta.SaldoPendiente.
    ///   - Al cobrar, se crea el único Ingreso real.
    ///   - Las Transferencias entre cuentas de la misma naturaleza no afectan resultados.
    ///   - Monto firmado por naturaleza:
    ///       * Cuenta Acreedora (Ingreso, Pasivo, Patrimonio): +Monto en Ingreso, -Monto en Egreso
    ///       * Cuenta Deudora (Activo, Costo, Gasto): +Monto en Egreso, -Monto en Ingreso
    ///   - Transferencias no afectan ni Ingreso ni Costo ni Gasto (no entran al Select de resultados).
    ///
    /// FIX BALANCE (21/10/2026):
    ///   - El Balance General SUMA los saldos reales de las cuentas de
    ///     Caja/Banco como Activo. Esto es necesario porque los movimientos
    ///     de esas cuentas no llevan una cuenta contable de Activo asociada
    ///     (van contra su categoría de ingreso/egreso).
    ///   - Se excluyen los movimientos de "Apertura" del conteo de
    ///     "sin clasificar" (ya tienen plan de cuentas asignado).
    /// 
    /// FIX SIGNOS (22/10/2026):
    ///   - Se invirtió el signo de la fórmula para que Ingresos y Patrimonio
    ///     se muestren en POSITIVO en los reportes, como corresponde.
    ///
    /// FIX BALANCE (23/10/2026):
    ///   - Los movimientos de "Apertura" ya no se restan del Activo: el saldo
    ///     inicial entra al Activo vía SaldoActual y al Patrimonio como
    ///     "Capital inicial (saldos de apertura)".
    ///   - El costo de ventas acumulado se descuenta del Inventario (1.1.03.001),
    ///     cuyas compras entran por la categoría "Pago a proveedores".
    ///   - Se agrega "Resultados de ejercicios anteriores" al Patrimonio
    ///     (antes solo se contaba el resultado del año en curso).
    ///   - Las Transferencias ya no cuentan como "sin clasificar".
    /// </summary>
    public static class ContabilidadHelper
    {
        // ============================================================
        // DTO DE SALIDA
        // ============================================================

        public class LineaReporte
        {
            public int? PlanCuentaId { get; set; }
            public string Codigo { get; set; } = "";
            public string Cuenta { get; set; } = "";
            public string Tipo { get; set; } = "";
            public string Naturaleza { get; set; } = "";
            public int Nivel { get; set; }
            public decimal Monto { get; set; }
            public int CantidadMovimientos { get; set; }

            /// <summary>
            /// True cuando la línea representa el saldo real de una cuenta
            /// financiera (Caja/Banco), no un movimiento contable agrupado.
            /// </summary>
            public bool EsSaldoCuentaFinanciera { get; set; }
        }

        public class EstadoResultados
        {
            public DateTime Desde { get; set; }
            public DateTime Hasta { get; set; }

            // Operativos
            public List<LineaReporte> IngresosOperativos { get; set; } = new(); // 4.1
            public List<LineaReporte> IngresosNoOperativos { get; set; } = new(); // 4.2
            public List<LineaReporte> Costos { get; set; } = new(); // 5.1
            public List<LineaReporte> GastosVenta { get; set; } = new(); // 6.1
            public List<LineaReporte> GastosAdministracion { get; set; } = new(); // 6.2
            public List<LineaReporte> OtrosGastos { get; set; } = new(); // 6.3

            // Totales
            public decimal TotalIngresosOperativos { get; set; }
            public decimal TotalIngresosNoOperativos { get; set; }
            public decimal TotalIngresos => TotalIngresosOperativos + TotalIngresosNoOperativos;

            public decimal TotalCostos { get; set; }
            public decimal TotalGastosVenta { get; set; }
            public decimal TotalGastosAdministracion { get; set; }
            public decimal TotalOtrosGastos { get; set; }
            public decimal TotalGastos => TotalGastosVenta + TotalGastosAdministracion + TotalOtrosGastos;

            public decimal UtilidadBruta => TotalIngresosOperativos - TotalCostos;
            public decimal UtilidadOperativa => UtilidadBruta - TotalGastosVenta - TotalGastosAdministracion;
            public decimal UtilidadNeta => UtilidadOperativa + TotalIngresosNoOperativos - TotalOtrosGastos;

            public decimal MargenBruto => TotalIngresosOperativos > 0
                ? Math.Round(UtilidadBruta / TotalIngresosOperativos * 100m, 2, MidpointRounding.AwayFromZero)
                : 0m;

            public decimal MargenNeto => TotalIngresos > 0
                ? Math.Round(UtilidadNeta / TotalIngresos * 100m, 2, MidpointRounding.AwayFromZero)
                : 0m;

            // Punto 11: ingresos cobrados (contra caja/banco real)
            public decimal IngresosCobrados { get; set; }

            /// <summary>Base de envío cobrada en el período (ya descontada de los ingresos operativos).</summary>
            public decimal EnviosCobrados { get; set; }
            /// <summary>ISV contenido en lo cobrado de ventas (menos el de devoluciones): es del fisco, no ingreso.</summary>
            public decimal IsvVentasNeto { get; set; }
            /// <summary>Ingreso devengado de ventas a crédito/facturas, menos lo ya contado al cobrar (sin ISV ni envío).</summary>
            public decimal AjusteDevengado { get; set; }

            // Punto 6: movimientos sin clasificar
            public decimal MontoSinClasificar { get; set; }
            public int MovimientosSinClasificar { get; set; }

            // Punto 12: estado del cierre contable
            public bool PeriodoCerrado { get; set; }
            public string? MensajeCierre { get; set; }
        }

        public class BalanceGeneral
        {
            public DateTime FechaCorte { get; set; }

            public List<LineaReporte> Activos { get; set; } = new();
            public List<LineaReporte> Pasivos { get; set; } = new();
            public List<LineaReporte> Patrimonio { get; set; } = new();

            public decimal TotalActivos { get; set; }
            public decimal TotalPasivos { get; set; }
            public decimal TotalPatrimonio { get; set; }
            public decimal TotalPasivoPatrimonio { get; set; }
            public decimal Diferencia { get; set; }

            public decimal ResultadoEjercicio { get; set; }
            public int AnioResultado { get; set; }

            // Punto 6
            public decimal MontoSinClasificar { get; set; }
            public int MovimientosSinClasificar { get; set; }

            public bool Cuadra => Math.Abs(Diferencia) <= 1.00m;

            // Punto 12
            public bool PeriodoCerrado { get; set; }
            public string? MensajeCierre { get; set; }
        }

        // ============================================================
        // HELPERS INTERNOS
        // ============================================================

        /// <summary>Datos del plan de cuentas asociados a una categoría financiera.</summary>
        private sealed record PlanInfo(int? PlanCuentaId, string? Codigo, string? Nombre, string? Tipo, string? Naturaleza, int Nivel);

        /// <summary>
        /// Carga de una sola vez categoría → cuenta del plan. Antes cada movimiento
        /// ejecutaba varias subconsultas correlacionadas (muy lento con muchos movimientos).
        /// </summary>
        private static Dictionary<int, PlanInfo> CargarPlanPorCategoria(ApplicationDbContext context)
        {
            var filas = (from c in context.CategoriasFinancieras.AsNoTracking()
                         join p in context.PlanCuentas.AsNoTracking() on c.PlanCuentaId equals (int?)p.Id into gj
                         from p in gj.DefaultIfEmpty()
                         select new
                         {
                             c.Id,
                             c.PlanCuentaId,
                             Codigo = p != null ? p.Codigo : null,
                             Nombre = p != null ? p.Nombre : null,
                             Tipo = p != null ? p.Tipo : null,
                             Naturaleza = p != null ? p.Naturaleza : null,
                             Nivel = p != null ? p.Nivel : 0
                         })
                .ToList();

            return filas.ToDictionary(
                f => f.Id,
                f => new PlanInfo(f.PlanCuentaId, f.Codigo, f.Nombre, f.Tipo, f.Naturaleza, f.Nivel));
        }

        /// <summary>
        /// +1 si la cuenta es Deudora (Activo, Costo, Gasto), -1 si Acreedora (Pasivo, Patrimonio, Ingreso).
        /// </summary>
        private static int SignoNaturaleza(string naturaleza)
        {
            return naturaleza == "Deudora" ? 1 : -1;
        }

        /// <summary>
        /// +1 si el movimiento es Ingreso, -1 si Egreso, 0 si Transferencia.
        /// </summary>
        private static int SignoMovimiento(string tipo)
        {
            return tipo switch
            {
                "Ingreso" => 1,
                "Egreso" => -1,
                _ => 0
            };
        }

        /// <summary>
        /// Devuelve el prefijo de código para agrupar (ej: "4.1", "6.2"). Null si no aplica.
        /// </summary>
        private static string? GrupoCodigo(string tipo, string codigo)
        {
            if (string.IsNullOrEmpty(codigo)) return null;

            if (tipo == "Ingreso")
            {
                if (codigo.StartsWith("4.1")) return "Operativo";
                if (codigo.StartsWith("4.2")) return "NoOperativo";
            }
            if (tipo == "Costo" && codigo.StartsWith("5.1")) return "CostoVenta";
            if (tipo == "Gasto")
            {
                if (codigo.StartsWith("6.1")) return "Venta";
                if (codigo.StartsWith("6.2")) return "Administracion";
                if (codigo.StartsWith("6.3")) return "Otros";
            }
            return null;
        }

        // ============================================================
        // ESTADO DE RESULTADOS
        // ============================================================

        public static EstadoResultados GenerarEstadoResultados(
            ApplicationDbContext context,
            DateTime desde,
            DateTime hasta)
        {
            var resultado = new EstadoResultados
            {
                Desde = desde.Date,
                Hasta = hasta.Date
            };

            var desdeFull = desde.Date;
            var hastaFull = hasta.Date.AddDays(1).AddSeconds(-1);

            // Punto 12: verificar cierre contable del mes de la fecha "hasta"
            var cierre = CierreContableHelper.Obtener(context, hasta.Year, hasta.Month);
            if (cierre != null && cierre.Estado == "Cerrado")
            {
                resultado.PeriodoCerrado = true;
                resultado.MensajeCierre = $"El período {cierre.PeriodoTexto} está cerrado contablemente. Los movimientos están bloqueados.";
            }

            // Traer movimientos con plan y cuenta financiera (para saber si es cobro real)
            var planPorCategoria = CargarPlanPorCategoria(context);
            var tipoCuenta = context.CuentasFinancieras.AsNoTracking()
                .Select(cf => new { cf.Id, cf.Tipo })
                .ToDictionary(cf => cf.Id, cf => cf.Tipo);

            var movimientos = context.MovimientosFinancieros
                .AsNoTracking()
                .Where(m => m.Estado == "Activo"
                         && m.Fecha >= desdeFull
                         && m.Fecha <= hastaFull)
                .Select(m => new { m.Id, m.Tipo, m.Monto, m.CategoriaId, m.CuentaId, m.Origen, m.FormaPago })
                .ToList()
                .Select(m =>
                {
                    PlanInfo? plan = null;
                    if (m.CategoriaId.HasValue) planPorCategoria.TryGetValue(m.CategoriaId.Value, out plan);
                    return new
                    {
                        m.Id,
                        m.Tipo,
                        m.Monto,
                        m.CategoriaId,
                        m.CuentaId,
                        m.Origen,
                        m.FormaPago,
                        PlanCuentaId = plan?.PlanCuentaId,
                        PlanCodigo = plan?.Codigo,
                        PlanNombre = plan?.Nombre,
                        PlanTipo = plan?.Tipo,
                        PlanNaturaleza = plan?.Naturaleza,
                        PlanNivel = plan?.Nivel ?? 0,
                        CuentaTipo = tipoCuenta.TryGetValue(m.CuentaId, out var ct) ? ct : null
                    };
                })
                .ToList();

            // Punto 6: contar sin clasificar
            var sinClasificar = movimientos.Where(m => m.PlanCuentaId == null && m.Tipo != "Transferencia").ToList();
            resultado.MontoSinClasificar = sinClasificar.Sum(m => m.Monto);
            resultado.MovimientosSinClasificar = sinClasificar.Count;

            // Punto 11: ingresos cobrados (contra caja/banco)
            // No cuentan los saldos de apertura de las cuentas ni la parte de una venta pagada con saldo a favor
            // del cliente (no entra dinero): solo lo cobrado de verdad.
            resultado.IngresosCobrados = movimientos
                .Where(m => m.Tipo == "Ingreso"
                         && m.Origen != "Apertura"
                         && !string.Equals(m.FormaPago, SaldoFavorHelper.NombreMetodo, StringComparison.OrdinalIgnoreCase)
                         && (m.CuentaTipo == "Caja" || m.CuentaTipo == "Banco"))
                .Sum(m => m.Monto)
                // El ingreso de una venta en efectivo es lo RECIBIDO: el cambio que se devolvió no quedó en la empresa
                - movimientos.Where(m => m.Tipo == "Egreso" && m.PlanCodigo == "4.1.01").Sum(m => m.Monto);

            // Punto 3: monto firmado por naturaleza
            var agrupado = movimientos
                .Where(m => m.PlanCuentaId != null)
                .GroupBy(m => new
                {
                    PlanId = m.PlanCuentaId!.Value,
                    Codigo = m.PlanCodigo ?? "",
                    Nombre = m.PlanNombre ?? "",
                    Tipo = m.PlanTipo ?? "",
                    Naturaleza = m.PlanNaturaleza ?? "",
                    Nivel = m.PlanNivel
                })
                .Select(g => new LineaReporte
                {
                    PlanCuentaId = g.Key.PlanId,
                    Codigo = g.Key.Codigo,
                    Cuenta = g.Key.Nombre,
                    Tipo = g.Key.Tipo,
                    Naturaleza = g.Key.Naturaleza,
                    Nivel = g.Key.Nivel,
                    // ✅ FIX SIGNOS: Invertimos el signo de la naturaleza para que 
                    // los Ingresos (Acreedora) se muestren en POSITIVO.
                    Monto = g.Sum(x => x.Monto * SignoMovimiento(x.Tipo) * (g.Key.Naturaleza == "Acreedora" ? 1 : -1)),
                    CantidadMovimientos = g.Count(),
                    EsSaldoCuentaFinanciera = false
                })
                .ToList();

            // Punto 10: separar por código jerárquico
            resultado.IngresosOperativos = agrupado
                .Where(l => l.Tipo == "Ingreso" && GrupoCodigo(l.Tipo, l.Codigo) == "Operativo")
                .OrderBy(l => l.Codigo).ToList();

            resultado.IngresosNoOperativos = agrupado
                .Where(l => l.Tipo == "Ingreso" && GrupoCodigo(l.Tipo, l.Codigo) == "NoOperativo")
                .OrderBy(l => l.Codigo).ToList();

            resultado.Costos = agrupado
                .Where(l => l.Tipo == "Costo")
                .OrderBy(l => l.Codigo).ToList();

            resultado.GastosVenta = agrupado
                .Where(l => l.Tipo == "Gasto" && GrupoCodigo(l.Tipo, l.Codigo) == "Venta")
                .OrderBy(l => l.Codigo).ToList();

            resultado.GastosAdministracion = agrupado
                .Where(l => l.Tipo == "Gasto" && GrupoCodigo(l.Tipo, l.Codigo) == "Administracion")
                .OrderBy(l => l.Codigo).ToList();

            resultado.OtrosGastos = agrupado
                .Where(l => l.Tipo == "Gasto" && GrupoCodigo(l.Tipo, l.Codigo) == "Otros")
                .OrderBy(l => l.Codigo).ToList();

            // El dinero cobrado por envíos no es utilidad: se descuenta de los ingresos operativos
            // (el ISV del envío se sigue pagando; solo la base del envío sale de la utilidad)
            resultado.EnviosCobrados = EnviosCobrados(context, desdeFull, hastaFull.AddSeconds(1));
            if (resultado.EnviosCobrados > 0.004m)
            {
                resultado.IngresosOperativos.Add(new LineaReporte
                {
                    Codigo = "4.1.99",
                    Cuenta = "Menos: envíos cobrados (no forman parte de la utilidad)",
                    Tipo = "Ingreso",
                    Naturaleza = "Acreedora",
                    Nivel = 4,
                    Monto = -resultado.EnviosCobrados,
                    CantidadMovimientos = 0
                });
            }

            // El ISV cobrado en las ventas se debe al fisco: no es ingreso ni utilidad.
            resultado.IsvVentasNeto = IsvVentasNetoPorAnio(context, desdeFull, hastaFull.AddSeconds(1)).Values.Sum();
            if (Math.Abs(resultado.IsvVentasNeto) > 0.004m)
            {
                resultado.IngresosOperativos.Add(new LineaReporte
                {
                    Codigo = "4.1.98",
                    Cuenta = "Menos: ISV cobrado en ventas (impuesto por pagar, no es ingreso)",
                    Tipo = "Ingreso",
                    Naturaleza = "Acreedora",
                    Nivel = 4,
                    Monto = -resultado.IsvVentasNeto,
                    CantidadMovimientos = 0
                });
            }

            // Devengado: una venta a crédito o factura es ingreso al facturarse, no al cobrarse.
            // Se suma lo facturado en el período y se resta lo cobrado de esos documentos (ya contado arriba).
            var devengoPeriodo = DevengoHelper.Calcular(context, desdeFull, hastaFull.AddSeconds(1));
            resultado.AjusteDevengado = devengoPeriodo.AjusteIngreso;
            if (Math.Abs(resultado.AjusteDevengado) > 0.004m)
            {
                resultado.IngresosOperativos.Add(new LineaReporte
                {
                    Codigo = "4.1.97",
                    Cuenta = "Ventas a crédito y facturas devengadas, neto de cobros ya contados",
                    Tipo = "Ingreso",
                    Naturaleza = "Acreedora",
                    Nivel = 4,
                    Monto = resultado.AjusteDevengado,
                    CantidadMovimientos = 0
                });
            }

            // Totales
            resultado.TotalIngresosOperativos = resultado.IngresosOperativos.Sum(l => l.Monto);
            resultado.TotalIngresosNoOperativos = resultado.IngresosNoOperativos.Sum(l => l.Monto);
            resultado.TotalCostos = resultado.Costos.Sum(l => l.Monto);
            resultado.TotalGastosVenta = resultado.GastosVenta.Sum(l => l.Monto);
            resultado.TotalGastosAdministracion = resultado.GastosAdministracion.Sum(l => l.Monto);
            resultado.TotalOtrosGastos = resultado.OtrosGastos.Sum(l => l.Monto);

            return resultado;
        }

        // ============================================================
        // ENVÍOS COBRADOS (no son utilidad)
        // ============================================================

        /// <summary>
        /// Parte de lo cobrado en el período que corresponde a la base del envío (sin ISV), según los cobros
        /// de ventas vigentes: cada cobro se reparte en proporción envío/total de su venta.
        /// </summary>
        public static decimal EnviosCobrados(ApplicationDbContext context, DateTime desde, DateTime hastaExcl)
        {
            return EnviosCobradosPorAnio(context, desde, hastaExcl).Values.Sum();
        }

        /// <summary>Lo mismo, agrupado por año del cobro.</summary>
        public static Dictionary<int, decimal> EnviosCobradosPorAnio(ApplicationDbContext context, DateTime? desde, DateTime hastaExcl)
        {
            var q = from p in context.Pagos.AsNoTracking()
                    join v in context.Ventas.AsNoTracking() on p.VentaId equals (int?)v.Id
                    where v.Estado != "Anulada" && v.MontoEnvio > 0 && v.Total > 0
                          && p.Fecha < hastaExcl
                    select new { p.Fecha, p.Monto, v.MontoEnvio, v.Total };
            if (desde.HasValue)
            {
                var d = desde.Value;
                q = q.Where(x => x.Fecha >= d);
            }

            var res = q.AsEnumerable()
                .GroupBy(x => x.Fecha.Year)
                .ToDictionary(g => g.Key, g => Math.Round(g.Sum(x => x.Monto * x.MontoEnvio / x.Total), 2, MidpointRounding.AwayFromZero));

            var qf = from p in context.Pagos.AsNoTracking()
                     join f in context.Facturas.AsNoTracking() on p.FacturaId equals (int?)f.Id
                     where p.VentaId == null && f.Estado != "Anulada" && f.MontoEnvio > 0 && f.Total > 0
                           && p.Fecha < hastaExcl
                     select new { p.Fecha, p.Monto, f.MontoEnvio, f.Total };
            if (desde.HasValue)
            {
                var d2 = desde.Value;
                qf = qf.Where(x => x.Fecha >= d2);
            }
            foreach (var g in qf.AsEnumerable().GroupBy(x => x.Fecha.Year))
                res[g.Key] = (res.TryGetValue(g.Key, out var prev) ? prev : 0m) + Math.Round(g.Sum(x => x.Monto * x.MontoEnvio / x.Total), 2, MidpointRounding.AwayFromZero);
            return res;
        }

        // ============================================================
        // ISV COBRADO EN VENTAS (es del fisco, no ingreso)
        // ============================================================

        /// <summary>
        /// ISV contenido en lo cobrado por ventas (cada cobro se reparte en proporción ISV/total de su venta),
        /// menos el ISV de las devoluciones aplicadas. Agrupado por año del cobro / devolución.
        /// </summary>
        public static Dictionary<int, decimal> IsvVentasNetoPorAnio(ApplicationDbContext context, DateTime? desde, DateTime hastaExcl)
        {
            var q = from p in context.Pagos.AsNoTracking()
                    join v in context.Ventas.AsNoTracking() on p.VentaId equals (int?)v.Id
                    where v.Estado != "Anulada" && v.Estado != "Cancelada" && v.Impuestos > 0 && v.Total > 0
                          && p.Fecha < hastaExcl
                    select new { p.Fecha, p.Monto, v.Impuestos, v.Total };
            if (desde.HasValue)
            {
                var d0 = desde.Value;
                q = q.Where(x => x.Fecha >= d0);
            }

            var res = q.AsEnumerable()
                .GroupBy(x => x.Fecha.Year)
                .ToDictionary(g => g.Key, g => Math.Round(g.Sum(x => x.Monto * x.Impuestos / x.Total), 2, MidpointRounding.AwayFromZero));

            // Cobros de facturas que no vienen de una venta (p.ej. cotización → factura)
            var qf = from p in context.Pagos.AsNoTracking()
                     join f in context.Facturas.AsNoTracking() on p.FacturaId equals (int?)f.Id
                     where p.VentaId == null && f.Estado != "Anulada" && f.Impuestos > 0 && f.Total > 0
                           && p.Fecha < hastaExcl
                     select new { p.Fecha, p.Monto, f.Impuestos, f.Total };
            if (desde.HasValue)
            {
                var d2 = desde.Value;
                qf = qf.Where(x => x.Fecha >= d2);
            }
            foreach (var g in qf.AsEnumerable().GroupBy(x => x.Fecha.Year))
                res[g.Key] = (res.TryGetValue(g.Key, out var prev) ? prev : 0m) + Math.Round(g.Sum(x => x.Monto * x.Impuestos / x.Total), 2, MidpointRounding.AwayFromZero);

            // Devoluciones aplicadas: devuelven también su parte de ISV
            var dq = context.Devoluciones.AsNoTracking()
                .Where(d => d.Estado == "Aplicada" && d.Fecha < hastaExcl);
            if (desde.HasValue)
            {
                var d1 = desde.Value;
                dq = dq.Where(d => d.Fecha >= d1);
            }
            var devs = dq.ToList();
            if (devs.Count > 0)
            {
                var ventaIds = devs.Where(d => d.VentaId.HasValue).Select(d => d.VentaId!.Value).Distinct().ToList();
                var facturaIds = devs.Where(d => !d.VentaId.HasValue && d.FacturaId.HasValue).Select(d => d.FacturaId!.Value).Distinct().ToList();
                var ventas = context.Ventas.AsNoTracking().Where(v => ventaIds.Contains(v.Id)).ToDictionary(v => v.Id, v => (v.Impuestos, v.Total));
                var facturas = context.Facturas.AsNoTracking().Where(f => facturaIds.Contains(f.Id)).ToDictionary(f => f.Id, f => (f.Impuestos, f.Total));
                foreach (var d in devs)
                {
                    (decimal imp, decimal tot) origen = (0m, 0m);
                    if (d.VentaId.HasValue && ventas.TryGetValue(d.VentaId.Value, out var vv)) origen = (vv.Impuestos, vv.Total);
                    else if (d.FacturaId.HasValue && facturas.TryGetValue(d.FacturaId.Value, out var ff)) origen = (ff.Impuestos, ff.Total);
                    if (origen.tot <= 0 || origen.imp <= 0) continue;
                    // La parte aplicada a una cuenta por cobrar ya reduce el ISV pendiente en el devengo
                    var isvDev = Math.Round((d.Total - d.MontoAplicadoSaldo) * origen.imp / origen.tot, 2, MidpointRounding.AwayFromZero);
                    res[d.Fecha.Year] = (res.TryGetValue(d.Fecha.Year, out var cur) ? cur : 0m) - isvDev;
                }
            }
            return res;
        }

        /// <summary>
        /// ISV (crédito fiscal) contenido en lo pagado a proveedores hasta la fecha: cada pago se reparte
        /// en proporción ISV/total de su orden de compra.
        /// </summary>
        public static decimal IsvComprasPagado(ApplicationDbContext context, DateTime hastaExcl)
        {
            var q = from pp in context.PagosProveedor.AsNoTracking()
                    join o in context.OrdenesCompra.AsNoTracking() on pp.OrdenCompraId equals (int?)o.Id
                    where o.Impuestos > 0 && o.Total > 0 && pp.Fecha < hastaExcl && pp.Estado != "Anulado"
                          && o.Estado != "Cancelada" && o.Estado != "Anulada" && o.Estado != "Borrador"
                    select new { pp.Monto, o.Impuestos, o.Total };
            return Math.Round(q.AsEnumerable().Sum(x => x.Monto * x.Impuestos / x.Total), 2, MidpointRounding.AwayFromZero);
        }

        // ============================================================
        // BALANCE GENERAL
        // ============================================================

        public static BalanceGeneral GenerarBalanceGeneral(
            ApplicationDbContext context,
            DateTime fechaCorte)
        {
            var resultado = new BalanceGeneral
            {
                FechaCorte = fechaCorte.Date,
                AnioResultado = fechaCorte.Year
            };

            var fechaFin = fechaCorte.Date.AddDays(1).AddSeconds(-1);

            // Punto 12
            var cierre = CierreContableHelper.Obtener(context, fechaCorte.Year, fechaCorte.Month);
            if (cierre != null && cierre.Estado == "Cerrado")
            {
                resultado.PeriodoCerrado = true;
                resultado.MensajeCierre = $"El período {cierre.PeriodoTexto} está cerrado contablemente.";
            }

            // Punto 5: incluir Anio en el Select
            var planPorCategoria = CargarPlanPorCategoria(context);

            var movimientos = context.MovimientosFinancieros
                .AsNoTracking()
                .Where(m => m.Estado == "Activo"
                         && m.Fecha <= fechaFin)
                .Select(m => new { m.Id, m.Tipo, m.Monto, Anio = m.Fecha.Year, m.Origen, m.CategoriaId })
                .ToList()
                .Select(m =>
                {
                    PlanInfo? plan = null;
                    if (m.CategoriaId.HasValue) planPorCategoria.TryGetValue(m.CategoriaId.Value, out plan);
                    return new
                    {
                        m.Id,
                        m.Tipo,
                        m.Monto,
                        m.Anio,
                        m.Origen,
                        m.CategoriaId,
                        PlanCuentaId = plan?.PlanCuentaId,
                        PlanCodigo = plan?.Codigo,
                        PlanNombre = plan?.Nombre,
                        PlanTipo = plan?.Tipo,
                        PlanNaturaleza = plan?.Naturaleza,
                        PlanNivel = plan?.Nivel ?? 0
                    };
                })
                .ToList();

            // Punto 6
            var sinClasificar = movimientos.Where(m => m.PlanCuentaId == null && m.Tipo != "Transferencia").ToList();
            resultado.MontoSinClasificar = sinClasificar.Sum(m => m.Monto);
            resultado.MovimientosSinClasificar = sinClasificar.Count;

            // Punto 3: monto firmado
            var agrupado = movimientos
                .Where(m => m.PlanCuentaId != null && m.Origen != "Apertura")
                .GroupBy(m => new
                {
                    PlanId = m.PlanCuentaId!.Value,
                    Codigo = m.PlanCodigo ?? "",
                    Nombre = m.PlanNombre ?? "",
                    Tipo = m.PlanTipo ?? "",
                    Naturaleza = m.PlanNaturaleza ?? "",
                    Nivel = m.PlanNivel
                })
                .Select(g => new LineaReporte
                {
                    PlanCuentaId = g.Key.PlanId,
                    Codigo = g.Key.Codigo,
                    Cuenta = g.Key.Nombre,
                    Tipo = g.Key.Tipo,
                    Naturaleza = g.Key.Naturaleza,
                    Nivel = g.Key.Nivel,
                    // ✅ FIX SIGNOS: Misma corrección que en Estado de Resultados.
                    Monto = g.Sum(x => x.Monto * SignoMovimiento(x.Tipo) * (g.Key.Naturaleza == "Acreedora" ? 1 : -1)),
                    CantidadMovimientos = g.Count(),
                    EsSaldoCuentaFinanciera = false
                })
                .ToList();

            // ===== Activos contables (de movimientos con plan) =====
            resultado.Activos = agrupado
                .Where(l => l.Tipo == "Activo")
                .OrderBy(l => l.Codigo).ToList();

            // ===== Activos desde saldos reales de Caja/Banco =====
            var cuentasFin = ObtenerCuentasFinancierasDeActivo(context);

            // FIX 25/10/2026: para una fecha de corte pasada, el saldo de Caja/Banco se
            // reconstruye (saldo inicial + movimientos hasta la fecha) en vez de usar el
            // saldo de hoy. Para hoy o fechas futuras se mantiene SaldoActual.
            Dictionary<int, decimal>? saldosAFecha = null;
            if (fechaCorte.Date < DateTime.Today)
            {
                saldosAFecha = CalcularSaldosAFecha(context, cuentasFin, fechaFin);
            }

            resultado.Activos.AddRange(ConstruirActivosDesdeCuentasFinancieras(cuentasFin, saldosAFecha));

            // La depreciación baja la utilidad y también el valor de los activos: sin esta línea el balance no cuadraba
            var depreciacionAcumulada = movimientos.Where(m => m.Origen == "Depreciacion" && m.Tipo == "Egreso").Sum(m => m.Monto);
            if (depreciacionAcumulada != 0)
            {
                resultado.Activos.Add(new LineaReporte
                {
                    Codigo = "1.2.02",
                    Cuenta = "Depreciación acumulada",
                    Tipo = "Activo",
                    Naturaleza = "Deudora",
                    Nivel = 3,
                    Monto = -depreciacionAcumulada
                });
            }

            // ===== Inventario: compras pagadas (1.1.03.x) - costo de ventas acumulado =====
            decimal costoAcumulado = movimientos
                .Where(m => m.PlanTipo == "Costo")
                .Sum(m => m.Monto * SignoMovimiento(m.Tipo) * (m.PlanNaturaleza == "Acreedora" ? 1 : -1));

            if (costoAcumulado != 0)
            {
                var lineaInventario = resultado.Activos.FirstOrDefault(l => l.Codigo == "1.1.03.001");
                if (lineaInventario == null)
                {
                    lineaInventario = new LineaReporte
                    {
                        Codigo = "1.1.03.001",
                        Cuenta = "Mercadería para la Venta",
                        Tipo = "Activo",
                        Naturaleza = "Deudora",
                        Nivel = 4,
                        Monto = 0,
                        CantidadMovimientos = 0
                    };
                    resultado.Activos.Add(lineaInventario);
                }
                lineaInventario.Monto -= costoAcumulado;
            }

            resultado.Activos = resultado.Activos.OrderBy(l => l.Codigo).ToList();

            resultado.Pasivos = agrupado
                .Where(l => l.Tipo == "Pasivo")
                .OrderBy(l => l.Codigo).ToList();

            // Lo cobrado por envíos no es utilidad: se muestra como pasivo (se aplica al costo del flete)
            // y se descuenta del resultado, igual que en el Estado de Resultados.
            var enviosPorAnio = EnviosCobradosPorAnio(context, null, fechaFin.AddSeconds(1));
            var devengo = DevengoHelper.Calcular(context, null, fechaFin.AddSeconds(1));
            var devengoAnterior = devengo.IngresoPorAnio.Where(k => k.Key < fechaCorte.Year).Sum(k => k.Value);
            var devengoAnio = devengo.IngresoPorAnio.TryGetValue(fechaCorte.Year, out var da) ? da : 0m;
            var enviosTotal = enviosPorAnio.Values.Sum() + devengo.EnvioPendienteVentas;
            var enviosAnterior = enviosPorAnio.Where(k => k.Key < fechaCorte.Year).Sum(k => k.Value);
            var enviosAnio = enviosPorAnio.TryGetValue(fechaCorte.Year, out var ea) ? ea : 0m;
            if (enviosTotal > 0.004m)
            {
                // Los fletes ya pagados (categoría «Fletes de envíos a clientes») vienen en esta línea en negativo
                var lineaEnvios = resultado.Pasivos.FirstOrDefault(l => l.Codigo == "2.1.99");
                if (lineaEnvios != null) lineaEnvios.Monto += enviosTotal;
                else resultado.Pasivos.Add(new LineaReporte
                {
                    Codigo = "2.1.99",
                    Cuenta = "Envíos cobrados por aplicar al flete",
                    Tipo = "Pasivo",
                    Naturaleza = "Acreedora",
                    Nivel = 4,
                    Monto = enviosTotal
                });
                resultado.Pasivos = resultado.Pasivos.OrderBy(l => l.Codigo).ToList();
            }

            // Nómina: lo retenido y aportado es deuda con cada entidad (los pagos ya vienen en negativo en su línea)
            // y los vales descontados bajan el anticipo que se había entregado.
            var generadoNomina = RetencionesHelper.Generado(context, fechaFin.AddSeconds(1));
            foreach (var c in RetencionesHelper.Conceptos)
            {
                if (!generadoNomina.TryGetValue(c.Clave, out var generado) || generado == 0) continue;
                var linea = resultado.Pasivos.FirstOrDefault(l => l.Codigo == c.CodigoPlan);
                if (linea != null) linea.Monto += generado;
                else resultado.Pasivos.Add(new LineaReporte
                {
                    Codigo = c.CodigoPlan,
                    Cuenta = $"{c.Nombre} por pagar",
                    Tipo = "Pasivo",
                    Naturaleza = "Acreedora",
                    Nivel = 4,
                    Monto = generado
                });
            }
            resultado.Pasivos = resultado.Pasivos.OrderBy(l => l.Codigo).ToList();
            if (generadoNomina.TryGetValue(RetencionesHelper.ClaveVale, out var valesDescontados) && valesDescontados != 0)
            {
                var lineaAnticipos = resultado.Activos.FirstOrDefault(l => l.Codigo == RetencionesHelper.CodigoAnticipos);
                if (lineaAnticipos != null) lineaAnticipos.Monto -= valesDescontados;
                else resultado.Activos.Add(new LineaReporte
                {
                    Codigo = RetencionesHelper.CodigoAnticipos,
                    Cuenta = "Anticipos a empleados",
                    Tipo = "Activo",
                    Naturaleza = "Deudora",
                    Nivel = 4,
                    Monto = -valesDescontados
                });
            }

            // Saldo a favor de clientes (devoluciones dejadas como crédito y aún no usadas): la empresa
            // les debe esa mercadería o ese dinero, por eso es un pasivo hasta que lo usen o se les reembolse.
            var saldoFavorClientes = context.SaldoFavorMovimientos.AsNoTracking()
                .Where(m => m.Fecha <= fechaFin)
                .Sum(m => (decimal?)m.Monto) ?? 0m;
            if (saldoFavorClientes > 0.004m)
            {
                resultado.Pasivos.Add(new LineaReporte
                {
                    Codigo = "2.1.06",
                    Cuenta = "Saldos a favor de clientes",
                    Tipo = "Pasivo",
                    Naturaleza = "Acreedora",
                    Nivel = 3,
                    Monto = Math.Round(saldoFavorClientes, 2, MidpointRounding.AwayFromZero)
                });
                resultado.Pasivos = resultado.Pasivos.OrderBy(l => l.Codigo).ToList();
            }

            // ISV por pagar a la SAR = ISV cobrado en ventas (neto de devoluciones)
            //                          - ISV pagado en compras (crédito fiscal)
            //                          - pagos ya hechos a la SAR (movimientos contra 2.1.02.001).
            // Si el resultado es negativo (pagado de más / crédito a favor) se muestra como Activo.
            var isvPorAnio = IsvVentasNetoPorAnio(context, null, fechaFin.AddSeconds(1));
            var isvTotal = isvPorAnio.Values.Sum();
            var isvAnterior = isvPorAnio.Where(k => k.Key < fechaCorte.Year).Sum(k => k.Value);
            var isvAnio = isvPorAnio.TryGetValue(fechaCorte.Year, out var ia) ? ia : 0m;
            var creditoCompras = IsvComprasPagado(context, fechaFin.AddSeconds(1));

            var lineaIsv = resultado.Pasivos.FirstOrDefault(l => l.Codigo == "2.1.02.001");
            var saldoIsv = (lineaIsv?.Monto ?? 0m) + isvTotal + devengo.IsvPendienteVentas - creditoCompras - devengo.IsvCreditoPendiente;
            if (lineaIsv != null) resultado.Pasivos.Remove(lineaIsv);
            if (saldoIsv > 0.004m)
            {
                resultado.Pasivos.Add(new LineaReporte
                {
                    Codigo = "2.1.02.001",
                    Cuenta = "ISV por pagar (SAR)",
                    Tipo = "Pasivo",
                    Naturaleza = "Acreedora",
                    Nivel = 4,
                    Monto = saldoIsv
                });
            }
            else if (saldoIsv < -0.004m)
            {
                resultado.Activos.Add(new LineaReporte
                {
                    Codigo = "1.1.05",
                    Cuenta = "ISV a favor (crédito fiscal / pagado por adelantado)",
                    Tipo = "Activo",
                    Naturaleza = "Deudora",
                    Nivel = 3,
                    Monto = -saldoIsv
                });
            }
            resultado.Pasivos = resultado.Pasivos.OrderBy(l => l.Codigo).ToList();

            // Cuentas por cobrar (ventas a crédito y facturas pendientes) y por pagar (compras recibidas sin pagar)
            if (devengo.CuentasPorCobrar > 0.004m)
            {
                var lineaCxC = resultado.Activos.FirstOrDefault(l => l.Codigo == "1.1.02.001");
                if (lineaCxC != null) lineaCxC.Monto += devengo.CuentasPorCobrar;
                else resultado.Activos.Add(new LineaReporte
                {
                    Codigo = "1.1.02.001",
                    Cuenta = "Clientes (cuentas por cobrar)",
                    Tipo = "Activo",
                    Naturaleza = "Deudora",
                    Nivel = 4,
                    Monto = devengo.CuentasPorCobrar
                });
            }
            if (devengo.CuentasPorPagar > 0.004m)
            {
                var lineaCxP = resultado.Pasivos.FirstOrDefault(l => l.Codigo == "2.1.01.001");
                if (lineaCxP != null) lineaCxP.Monto += devengo.CuentasPorPagar;
                else resultado.Pasivos.Add(new LineaReporte
                {
                    Codigo = "2.1.01.001",
                    Cuenta = "Proveedores (cuentas por pagar)",
                    Tipo = "Pasivo",
                    Naturaleza = "Acreedora",
                    Nivel = 4,
                    Monto = devengo.CuentasPorPagar
                });
                resultado.Pasivos = resultado.Pasivos.OrderBy(l => l.Codigo).ToList();
            }

            // El ISV de las compras pagadas está dentro del inventario: pasa a crédito fiscal.
            // Las compras recibidas sin pagar suman su base al inventario.
            var ajusteInventario = devengo.InventarioPendiente - creditoCompras;
            if (Math.Abs(ajusteInventario) > 0.004m)
            {
                var lineaInv = resultado.Activos.FirstOrDefault(l => l.Codigo == "1.1.03.001");
                if (lineaInv == null)
                {
                    lineaInv = new LineaReporte
                    {
                        Codigo = "1.1.03.001",
                        Cuenta = "Mercadería para la Venta",
                        Tipo = "Activo",
                        Naturaleza = "Deudora",
                        Nivel = 4,
                        Monto = 0
                    };
                    resultado.Activos.Add(lineaInv);
                }
                lineaInv.Monto += ajusteInventario;
            }
            resultado.Activos = resultado.Activos.OrderBy(l => l.Codigo).ToList();

            // Punto 4: excluir 3.2.04 (Resultado del Ejercicio) del Patrimonio
            resultado.Patrimonio = agrupado
                .Where(l => l.Tipo == "Patrimonio" && l.Codigo != "3.2.04")
                .OrderBy(l => l.Codigo).ToList();

            // Capital inicial: los saldos de apertura de las cuentas incluidas en el Activo
            decimal saldosIniciales = cuentasFin.Sum(c => c.SaldoInicial);
            if (saldosIniciales != 0)
            {
                resultado.Patrimonio.Add(new LineaReporte
                {
                    Codigo = "3.1.01",
                    Cuenta = "Capital inicial (saldos de apertura)",
                    Tipo = "Patrimonio",
                    Naturaleza = "Acreedora",
                    Nivel = 3,
                    Monto = saldosIniciales
                });
            }

            // Resultados de ejercicios anteriores (años < año de corte)
            decimal resultadoAnterior = movimientos
                .Where(m => m.Anio < fechaCorte.Year)
                .Sum(m =>
                {
                    decimal firmado = m.Monto * SignoMovimiento(m.Tipo) * (m.PlanNaturaleza == "Acreedora" ? 1 : -1);
                    return m.PlanTipo == "Ingreso" ? firmado
                         : (m.PlanTipo == "Costo" || m.PlanTipo == "Gasto") ? -firmado
                         : 0m;
                });

            if (resultadoAnterior != 0)
            {
                resultado.Patrimonio.Add(new LineaReporte
                {
                    Codigo = "3.2.02",
                    Cuenta = "Resultados de ejercicios anteriores",
                    Tipo = "Patrimonio",
                    Naturaleza = "Acreedora",
                    Nivel = 3,
                    Monto = resultadoAnterior - enviosAnterior - isvAnterior + devengoAnterior
                });
            }
            else if (Math.Abs(devengoAnterior - enviosAnterior - isvAnterior) > 0.004m)
            {
                resultado.Patrimonio.Add(new LineaReporte
                {
                    Codigo = "3.2.02",
                    Cuenta = "Resultados de ejercicios anteriores",
                    Tipo = "Patrimonio",
                    Naturaleza = "Acreedora",
                    Nivel = 3,
                    Monto = devengoAnterior - enviosAnterior - isvAnterior
                });
            }

            resultado.Patrimonio = resultado.Patrimonio.OrderBy(l => l.Codigo).ToList();

            resultado.TotalActivos = resultado.Activos.Sum(l => l.Monto);
            resultado.TotalPasivos = resultado.Pasivos.Sum(l => l.Monto);
            resultado.TotalPatrimonio = resultado.Patrimonio.Sum(l => l.Monto);

            // Punto 5: calcular ResultadoEjercicio solo del año en curso
            // ✅ FIX SIGNOS: Aquí también corregimos para que los ingresos sumen positivo.
            var ingresosAnio = movimientos
                .Where(m => m.Anio == fechaCorte.Year && m.PlanTipo == "Ingreso")
                .Sum(m => m.Monto * SignoMovimiento(m.Tipo) * (m.PlanNaturaleza == "Acreedora" ? 1 : -1));

            var costosAnio = movimientos
                .Where(m => m.Anio == fechaCorte.Year && m.PlanTipo == "Costo")
                .Sum(m => m.Monto * SignoMovimiento(m.Tipo) * (m.PlanNaturaleza == "Acreedora" ? 1 : -1));

            var gastosAnio = movimientos
                .Where(m => m.Anio == fechaCorte.Year && m.PlanTipo == "Gasto")
                .Sum(m => m.Monto * SignoMovimiento(m.Tipo) * (m.PlanNaturaleza == "Acreedora" ? 1 : -1));

            resultado.ResultadoEjercicio = ingresosAnio - costosAnio - gastosAnio - enviosAnio - isvAnio + devengoAnio;

            // Punto 4: sumar ResultadoEjercicio UNA sola vez
            resultado.TotalPatrimonio += resultado.ResultadoEjercicio;
            resultado.TotalPasivoPatrimonio = resultado.TotalPasivos + resultado.TotalPatrimonio;
            resultado.Diferencia = resultado.TotalActivos - resultado.TotalPasivoPatrimonio;

            return resultado;
        }

        /// <summary>
        /// FIX 21/10/2026: lee los SaldosActuales de todas las cuentas
        /// financieras de tipo Caja/Banco activas (excepto CuentasPorCobrar)
        /// y los devuelve como líneas de Activo para el Balance General.
        /// </summary>
        private static List<CuentaFinanciera> ObtenerCuentasFinancierasDeActivo(
            ApplicationDbContext context)
        {
            return context.CuentasFinancieras
                // Una cuenta inactiva con dinero sigue siendo un activo: si se omite, el balance no cuadra
                .Where(c => c.Activa || c.SaldoActual != 0)
                .Where(c => c.Subtipo == null || c.Subtipo != "CuentasPorCobrar")
                .OrderBy(c => c.Tipo)
                .ThenBy(c => c.Codigo)
                .ToList();
        }

        /// <summary>
        /// Saldo de cada cuenta financiera a una fecha: SaldoInicial + efecto de los
        /// movimientos activos hasta esa fecha. Aplica las mismas reglas que SaldoHelper:
        /// las aperturas y los movimientos solo contables (costo de ventas 5.1.01) no
        /// mueven el saldo real, y las transferencias restan del origen y suman al destino.
        /// </summary>
        private static Dictionary<int, decimal> CalcularSaldosAFecha(
            ApplicationDbContext context,
            List<CuentaFinanciera> cuentas,
            DateTime fechaFin)
        {
            var saldos = cuentas.ToDictionary(c => c.Id, c => c.SaldoInicial);

            var categoriasCosto = (from c in context.CategoriasFinancieras
                                   join p in context.PlanCuentas on c.PlanCuentaId equals (int?)p.Id
                                   where p.Codigo == "5.1.01"
                                   select c.Id).ToList();
            var setCosto = new HashSet<int>(categoriasCosto);

            var movimientos = context.MovimientosFinancieros
                .AsNoTracking()
                .Where(m => m.Estado == "Activo" && m.Origen != "Apertura" && m.Fecha <= fechaFin)
                .Select(m => new { m.Tipo, m.Monto, m.CuentaId, m.CuentaDestinoId, m.CategoriaId, m.Notas })
                .ToList();

            foreach (var m in movimientos)
            {
                // Misma regla que SaldoHelper.EsSoloContable
                if (m.Notas != null && m.Notas.Contains(SaldoHelper.MarcaSoloContable)) continue;
                if (m.Tipo == "Egreso" && m.CategoriaId.HasValue && setCosto.Contains(m.CategoriaId.Value)) continue;

                switch (m.Tipo)
                {
                    case "Ingreso":
                        if (saldos.ContainsKey(m.CuentaId)) saldos[m.CuentaId] += m.Monto;
                        break;
                    case "Egreso":
                        if (saldos.ContainsKey(m.CuentaId)) saldos[m.CuentaId] -= m.Monto;
                        break;
                    case "Transferencia":
                        if (saldos.ContainsKey(m.CuentaId)) saldos[m.CuentaId] -= m.Monto;
                        if (m.CuentaDestinoId.HasValue && saldos.ContainsKey(m.CuentaDestinoId.Value))
                            saldos[m.CuentaDestinoId.Value] += m.Monto;
                        break;
                }
            }

            return saldos;
        }

        private static List<LineaReporte> ConstruirActivosDesdeCuentasFinancieras(
            List<CuentaFinanciera> cuentas,
            Dictionary<int, decimal>? saldosAFecha = null)
        {
            var lineas = new List<LineaReporte>();

            foreach (var cuenta in cuentas)
            {
                // Determinar el código del plan a mostrar
                string codigo = cuenta.CuentaContable ?? "";
                if (string.IsNullOrWhiteSpace(codigo))
                {
                    codigo = cuenta.Tipo == "Banco" ? "1.1.01.004" : "1.1.01.001";
                }

                // Nivel 4 = cuenta de movimiento (hoja)
                int nivel = 4;
                if (!string.IsNullOrEmpty(codigo) && codigo.Contains('.'))
                {
                    nivel = codigo.Split('.').Length;
                }

                lineas.Add(new LineaReporte
                {
                    PlanCuentaId = null,
                    Codigo = codigo,
                    Cuenta = cuenta.Nombre,
                    Tipo = "Activo",
                    Naturaleza = "Deudora",
                    Nivel = nivel,
                    Monto = saldosAFecha != null && saldosAFecha.TryGetValue(cuenta.Id, out var saldoFecha)
                        ? saldoFecha
                        : cuenta.SaldoActual,
                    CantidadMovimientos = 0,
                    EsSaldoCuentaFinanciera = true
                });
            }

            return lineas;
        }

        // ============================================================
        // UTILIDADES
        // ============================================================

        public static int ContarSinPlanCuenta(
            ApplicationDbContext context,
            DateTime desde,
            DateTime hasta)
        {
            var desdeFull = desde.Date;
            var hastaFull = hasta.Date.AddDays(1).AddSeconds(-1);

            return context.MovimientosFinancieros
                .Where(m => m.Estado == "Activo"
                         && m.Fecha >= desdeFull
                         && m.Fecha <= hastaFull)
                .Where(m => m.Origen != "Apertura" && m.Tipo != "Transferencia") // Apertura SÍ tiene plan; una transferencia no lleva categoría
                .Where(m => m.CategoriaId == null
                         || !context.CategoriasFinancieras
                            .Where(c => c.Id == m.CategoriaId)
                            .Any(c => c.PlanCuentaId != null))
                .Count();
        }
    }
}