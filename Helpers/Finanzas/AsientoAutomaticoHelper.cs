using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers.Finanzas
{
    /// <summary>
    /// Genera los asientos de partida doble de los movimientos financieros (ventas cobradas,
    /// compras pagadas, nómina, gastos, transferencias, aperturas, etc.) para que el Libro Diario,
    /// el Libro Mayor y la Balanza reflejen TODO el negocio y no solo los asientos manuales.
    ///
    /// Es idempotente y no toca los movimientos ni los saldos:
    ///   - Cada asiento automático lleva Referencia = "MOV:{Id del movimiento}".
    ///   - Un movimiento activo sin asiento recibe uno nuevo.
    ///   - Si el movimiento cambió de monto o de fecha (edición), el asiento viejo se anula y se crea otro.
    ///   - Si el movimiento se anuló, su asiento se anula.
    ///
    /// Regla de cuentas:
    ///   Ingreso        Debe: cuenta financiera       Haber: cuenta contable de la categoría
    ///   Egreso         Debe: cuenta de la categoría  Haber: cuenta financiera
    ///   Transferencia  Debe: cuenta destino          Haber: cuenta origen
    ///   Apertura       Debe: cuenta financiera       Haber: 3.1.01 Capital Social
    ///   Solo contable  (costo de ventas) la contrapartida es 1.1.03.001 Inventario
    /// </summary>
    public static class AsientoAutomaticoHelper
    {
        public const string PrefijoReferencia = "MOV:";

        /// <summary>
        /// Movimiento que no lleva asiento automático porque su efecto contable ya está en otro asiento
        /// (p. ej. la apertura de una caja migrada cuando se importó el balance de apertura del sistema anterior).
        /// </summary>
        public const string MarcaSinAsiento = "[SIN-ASIENTO]";

        /// <summary>Los históricos migrados y los marcados no generan asiento: su efecto ya está en el asiento de apertura.</summary>
        public static bool GeneraAsiento(MovimientoFinanciero m) =>
            m.Origen != "Migracion" && (m.Notas == null || !m.Notas.Contains(MarcaSinAsiento));
        public const string PrefijoDevengo = "DEV:";

        public class Resultado
        {
            public int Creados { get; set; }
            public int Reemplazados { get; set; }
            public int Anulados { get; set; }
            public int SinCuenta { get; set; }
            public bool HayMasPendientes { get; set; }
        }

        private static DateTime _ultimaSincronizacion = DateTime.MinValue;
        private static readonly object _candado = new();

        /// <summary>
        /// Para llamar al abrir los libros contables: sincroniza como máximo una vez por minuto
        /// y nunca lanza excepciones (si falla, el reporte se muestra igual con lo que ya hay).
        /// </summary>
        public static void SincronizarSilencioso(ApplicationDbContext context, int? usuarioId = null)
        {
            lock (_candado)
            {
                if ((DateTime.Now - _ultimaSincronizacion).TotalSeconds < 60) return;
                _ultimaSincronizacion = DateTime.Now;
            }

            try
            {
                Sincronizar(context, usuarioId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AsientoAutomatico] {ex.Message}");
                context.ChangeTracker.Clear();
            }
        }

        public static Resultado Sincronizar(ApplicationDbContext context, int? usuarioId = null, int maximoPorLlamada = 1500)
        {
            var r = new Resultado();

            var plan = context.PlanCuentas.ToList();
            var idPorCodigo = plan.GroupBy(p => p.Codigo).ToDictionary(g => g.Key, g => g.First().Id);
            if (idPorCodigo.Count == 0) return r;

            int? Cod(string codigo) => idPorCodigo.TryGetValue(codigo, out var id) ? id : (int?)null;
            var idPlanValido = new HashSet<int>(plan.Select(p => p.Id));

            var cuentas = context.CuentasFinancieras.ToDictionary(c => c.Id);
            var categorias = context.CategoriasFinancieras.ToDictionary(c => c.Id);

            int? idCxC = Cod("1.1.02.001");
            int? idIsvPorPagar = Cod("2.1.02.001");
            int? idEnvios = Cod("2.1.99");
            int? idVentas = Cod("4.1.01");
            int? idSaldoFavor = Cod("2.1.06");

            // Asientos automáticos que siguen activos, por referencia (MOV:{id} o DEV:V{id} / DEV:F{id})
            var asientosAuto = context.AsientosContables
                .Where(a => a.Estado == "Activo" && a.Referencia != null
                         && (a.Referencia.StartsWith(PrefijoReferencia) || a.Referencia.StartsWith(PrefijoDevengo)))
                .ToList();
            var idsAsientos = asientosAuto.Select(a => a.Id).ToList();
            var lineasPorAsiento = context.DetallesAsiento
                .Where(d => idsAsientos.Contains(d.AsientoContableId))
                .AsEnumerable()
                .GroupBy(d => d.AsientoContableId)
                .ToDictionary(g => g.Key, g => g.Select(d => (d.PlanCuentaId, d.Debe, d.Haber)).OrderBy(x => x.PlanCuentaId).ThenBy(x => x.Debe).ThenBy(x => x.Haber).ToList());

            var porReferencia = new Dictionary<string, List<AsientoContable>>();
            foreach (var a in asientosAuto)
            {
                if (!porReferencia.TryGetValue(a.Referencia!, out var lista))
                    porReferencia[a.Referencia!] = lista = new List<AsientoContable>();
                lista.Add(a);
            }

            // Proporciones de ISV / envío de cada documento de origen y si se cobra a crédito
            var ventasInfo = context.Ventas.AsNoTracking().Where(v => v.Total > 0)
                .Select(v => new { v.Id, v.Impuestos, v.MontoEnvio, v.Total, v.EsCredito }).ToList()
                .ToDictionary(v => v.Id, v => new InfoDoc(v.Impuestos / v.Total, v.MontoEnvio / v.Total, v.EsCredito));
            var facturasInfo = context.Facturas.AsNoTracking().Where(f => f.Total > 0)
                .Select(f => new { f.Id, f.Impuestos, f.MontoEnvio, f.Total, f.VentaId }).ToList()
                .ToDictionary(f => f.Id, f => (Info: new InfoDoc(f.Impuestos / f.Total, f.MontoEnvio / f.Total, f.VentaId == null), f.VentaId));
            var devolucionesOrigen = context.Devoluciones.AsNoTracking()
                .Select(d => new { d.Id, d.VentaId, d.FacturaId }).ToList()
                .ToDictionary(d => d.Id, d => (d.VentaId, d.FacturaId));

            // Proporción de ISV (crédito fiscal) de cada pago a proveedor, según su orden de compra
            var isvPagosProveedor = (from pp in context.PagosProveedor.AsNoTracking()
                                     join o in context.OrdenesCompra.AsNoTracking() on pp.OrdenCompraId equals (int?)o.Id
                                     where o.Impuestos > 0 && o.Total > 0
                                     select new { pp.Id, o.Impuestos, o.Total }).ToList()
                .ToDictionary(x => x.Id, x => x.Impuestos / x.Total);

            InfoDoc? InfoDe(string? origen, int docId)
            {
                if (origen == "Venta") return ventasInfo.TryGetValue(docId, out var v) ? v : null;
                if (origen == "Factura" && facturasInfo.TryGetValue(docId, out var f))
                {
                    if (f.VentaId.HasValue && ventasInfo.TryGetValue(f.VentaId.Value, out var vf)) return vf;
                    return f.Info;
                }
                return null;
            }

            bool Igual(List<AsientoContable>? existentes, DateTime fecha, List<(int cuenta, decimal debe, decimal haber)> esperadas)
            {
                if (existentes == null || existentes.Count == 0) return false;
                var vigente = existentes.OrderByDescending(a => a.Id).First();
                if (vigente.Fecha.Date != fecha.Date) return false;
                if (!lineasPorAsiento.TryGetValue(vigente.Id, out var actuales)) return false;
                var ord = esperadas.OrderBy(x => x.cuenta).ThenBy(x => x.debe).ThenBy(x => x.haber).ToList();
                if (ord.Count != actuales.Count) return false;
                for (int i = 0; i < ord.Count; i++)
                    if (ord[i].cuenta != actuales[i].PlanCuentaId || ord[i].debe != actuales[i].Debe || ord[i].haber != actuales[i].Haber)
                        return false;
                return true;
            }

            void Guardar(string referencia, string numeroBase, DateTime fecha, string concepto, int? usuario, bool reemplaza,
                         List<(int cuenta, decimal debe, decimal haber)> lineas, string detalle)
            {
                if (concepto.Length > 280) concepto = concepto.Substring(0, 280);
                var asiento = new AsientoContable
                {
                    // Número propio por documento (evita recorrer la serie miles de veces en la primera carga)
                    Numero = reemplaza ? $"{numeroBase}-R{DateTime.Now:ddHHmmssfff}" : numeroBase,
                    Fecha = fecha.Date,
                    Concepto = $"[Auto] {concepto}",
                    Referencia = referencia,
                    Estado = "Activo",
                    UsuarioId = usuario,
                    FechaCreacion = DateTime.Now
                };
                context.AsientosContables.Add(asiento);
                context.SaveChanges();
                foreach (var l in lineas)
                {
                    context.DetallesAsiento.Add(new DetalleAsiento
                    {
                        AsientoContableId = asiento.Id,
                        PlanCuentaId = l.cuenta,
                        Debe = l.debe,
                        Haber = l.haber,
                        Detalle = detalle
                    });
                }
                context.SaveChanges();
            }

            int procesadosEnEstaLlamada = 0;

            // ===== 1) Movimientos financieros =====
            var movimientos = context.MovimientosFinancieros.OrderBy(m => m.Id).ToList();

            foreach (var mov in movimientos)
            {
                var referencia = PrefijoReferencia + mov.Id;
                porReferencia.TryGetValue(referencia, out var existentes);

                // Movimiento anulado (o que no lleva asiento propio): se anulan sus asientos
                if (mov.Estado != "Activo" || !GeneraAsiento(mov))
                {
                    if (existentes != null)
                    {
                        foreach (var a in existentes)
                        {
                            a.Estado = "Anulado";
                            a.MotivoAnulacion = mov.Estado != "Activo" ? $"Movimiento {mov.Numero} anulado" : $"Movimiento {mov.Numero}: su efecto está en el asiento de apertura";
                            r.Anulados++;
                        }
                    }
                    continue;
                }

                var monto = Math.Round(mov.Monto * (mov.TipoCambio > 0 ? mov.TipoCambio : 1m), 2);
                // Monto negativo (p.ej. reversion de costo por devolucion): se registra el asiento inverso
                bool inverso = monto < 0;
                monto = Math.Abs(monto);
                if (monto == 0) continue;

                var (debeId, haberId) = ResolverCuentas(mov, cuentas, categorias, Cod, idPlanValido, context);
                // Saldo a favor del cliente: la contrapartida no es caja sino la deuda con el cliente
                // (pago de una venta con su saldo: Debe 2.1.06; devolución acreditada a su saldo: Haber 2.1.06).
                if (idSaldoFavor != null && string.Equals(mov.FormaPago, Kirkenta.Helpers.SaldoFavorHelper.NombreMetodo, StringComparison.OrdinalIgnoreCase))
                {
                    if (mov.Tipo == "Ingreso") debeId = idSaldoFavor;
                    else if (mov.Tipo == "Egreso" && mov.Origen == "Devolucion") haberId = idSaldoFavor;
                }
                if (inverso) (debeId, haberId) = (haberId, debeId);

                // ISV y envío contenidos en el movimiento: no son ingreso, van a sus cuentas de pasivo.
                // Un cobro de una venta a crédito / factura pendiente cancela la cuenta por cobrar (ya devengada).
                decimal isv = 0m, env = 0m;
                if (mov.Origen != "Apertura" && mov.Tipo != "Transferencia" && mov.OrigenId.HasValue)
                {
                    categorias.TryGetValue(mov.CategoriaId ?? 0, out var catMov);
                    var nombreCat = catMov?.Nombre ?? "";
                    var docId = mov.OrigenId.Value;

                    if (mov.Tipo == "Ingreso" && mov.FormaPago != "Crédito" && (mov.Origen == "Venta" || mov.Origen == "Factura"))
                    {
                        var info = InfoDe(mov.Origen, docId);
                        if (info != null)
                        {
                            if (info.AlCredito && idCxC != null && idVentas != null) haberId = idCxC;
                            else { isv = Math.Round(monto * info.Isv, 2); env = Math.Round(monto * info.Envio, 2); }
                        }
                    }
                    else if (mov.Tipo == "Egreso" && nombreCat == "Cambio entregado" && (mov.Origen == "Venta" || mov.Origen == "Factura"))
                    {
                        var info = InfoDe(mov.Origen, docId);
                        if (info != null && !info.AlCredito)
                        {
                            isv = Math.Round(monto * info.Isv, 2);
                            env = Math.Round(monto * info.Envio, 2);
                        }
                    }
                    else if (mov.Tipo == "Egreso" && mov.Origen == "PagoProveedor" && isvPagosProveedor.TryGetValue(docId, out var propIsvPago))
                    {
                        // El ISV pagado al proveedor es crédito fiscal contra el ISV por pagar, no costo de la mercadería
                        isv = Math.Round(monto * propIsvPago, 2);
                    }
                    else if (mov.Tipo == "Egreso" && mov.Origen == "Devolucion" && nombreCat == "Devoluciones en ventas"
                             && devolucionesOrigen.TryGetValue(docId, out var dv))
                    {
                        InfoDoc? info = null;
                        if (dv.VentaId.HasValue) info = InfoDe("Venta", dv.VentaId.Value);
                        else if (dv.FacturaId.HasValue) info = InfoDe("Factura", dv.FacturaId.Value);
                        // El ISV de lo devuelto se regresa siempre (en ventas a crédito ya se había devengado)
                        if (info != null) isv = Math.Round(monto * info.Isv, 2);
                    }
                }
                if (idIsvPorPagar == null) isv = 0m;
                if (idEnvios == null) env = 0m;
                if (isv + env >= monto) { isv = 0m; env = 0m; }

                if (debeId == null || haberId == null || debeId == haberId)
                {
                    if (existentes != null && existentes.Count > 0)
                    {
                        // El movimiento ya no puede asentarse: se anula el asiento viejo
                        foreach (var a in existentes) { a.Estado = "Anulado"; a.MotivoAnulacion = $"Movimiento {mov.Numero} modificado"; r.Anulados++; }
                    }
                    r.SinCuenta++;
                    continue;
                }

                // Líneas del asiento. Ingreso: Debe caja/banco; Haber ventas (base) + ISV + envío.
                // Egreso (cambio / devolución): Debe base + ISV + envío; Haber caja/banco.
                bool ladoDebe = (mov.Tipo == "Egreso") ^ inverso;
                var baseMonto = monto - isv - env;
                var lineas = new List<(int cuenta, decimal debe, decimal haber)>();
                if (!ladoDebe)
                {
                    lineas.Add((debeId.Value, monto, 0m));
                    lineas.Add((haberId.Value, 0m, baseMonto));
                    if (isv > 0m) lineas.Add((idIsvPorPagar!.Value, 0m, isv));
                    if (env > 0m) lineas.Add((idEnvios!.Value, 0m, env));
                }
                else
                {
                    lineas.Add((debeId.Value, baseMonto, 0m));
                    if (isv > 0m) lineas.Add((idIsvPorPagar!.Value, isv, 0m));
                    if (env > 0m) lineas.Add((idEnvios!.Value, env, 0m));
                    lineas.Add((haberId.Value, 0m, monto));
                }

                if (Igual(existentes, mov.Fecha, lineas)) continue;

                bool reemplaza = existentes != null && existentes.Count > 0;
                if (procesadosEnEstaLlamada >= maximoPorLlamada)
                {
                    r.HayMasPendientes = true;
                    continue;
                }
                if (reemplaza)
                {
                    foreach (var a in existentes!) { a.Estado = "Anulado"; a.MotivoAnulacion = $"Movimiento {mov.Numero} modificado"; }
                }

                var concepto = string.IsNullOrWhiteSpace(mov.Concepto) ? $"Movimiento {mov.Numero}" : mov.Concepto;
                Guardar(referencia, $"AUT-{mov.Id:D7}", mov.Fecha, concepto, usuarioId ?? mov.UsuarioCreoId, reemplaza, lineas, mov.Numero);

                if (reemplaza) r.Reemplazados++; else r.Creados++;
                procesadosEnEstaLlamada++;
            }

            // ===== 2) Devengo: ventas a crédito y facturas sin venta se asientan al facturar =====
            // Debe Clientes (total); Haber Ventas (base) + ISV por pagar + envíos por aplicar.
            if (idCxC != null && idVentas != null)
            {
                var docs = new List<(string Ref, string NumeroBase, string Numero, string Etq, DateTime Fecha, decimal Total, decimal Isv, decimal Envio, bool Vigente, int? Usuario)>();
                foreach (var v in context.Ventas.AsNoTracking().Where(v => v.EsCredito).ToList())
                    docs.Add((PrefijoDevengo + "V" + v.Id, $"AUT-V{v.Id:D7}", v.Numero, "Venta a crédito", v.Fecha, v.Total, v.Impuestos, v.MontoEnvio,
                              v.Estado != "Anulada" && v.Estado != "Cancelada" && v.Total > 0, v.UsuarioCreoId));
                foreach (var f in context.Facturas.AsNoTracking().Where(f => f.VentaId == null).ToList())
                    docs.Add((PrefijoDevengo + "F" + f.Id, $"AUT-F{f.Id:D7}", f.Numero, "Factura", f.Fecha, f.Total, f.Impuestos, f.MontoEnvio,
                              f.Estado != "Anulada" && f.Estado != "Borrador" && f.Total > 0, null));

                foreach (var d in docs)
                {
                    porReferencia.TryGetValue(d.Ref, out var existentes);

                    if (!d.Vigente)
                    {
                        if (existentes != null)
                            foreach (var a in existentes) { a.Estado = "Anulado"; a.MotivoAnulacion = $"{d.Etq} {d.Numero} anulada"; r.Anulados++; }
                        continue;
                    }

                    var isv = idIsvPorPagar != null ? Math.Round(Math.Max(0m, d.Isv), 2) : 0m;
                    var env = idEnvios != null ? Math.Round(Math.Max(0m, d.Envio), 2) : 0m;
                    var total = Math.Round(d.Total, 2);
                    var neto = total - isv - env;
                    if (neto < 0) { isv = 0m; env = 0m; neto = total; }

                    var lineas = new List<(int cuenta, decimal debe, decimal haber)> { (idCxC.Value, total, 0m) };
                    if (neto > 0m) lineas.Add((idVentas.Value, 0m, neto));
                    if (isv > 0m) lineas.Add((idIsvPorPagar!.Value, 0m, isv));
                    if (env > 0m) lineas.Add((idEnvios!.Value, 0m, env));

                    if (Igual(existentes, d.Fecha, lineas)) continue;

                    bool reemplaza = existentes != null && existentes.Count > 0;
                    if (procesadosEnEstaLlamada >= maximoPorLlamada)
                    {
                        r.HayMasPendientes = true;
                        continue;
                    }
                    if (reemplaza)
                        foreach (var a in existentes!) { a.Estado = "Anulado"; a.MotivoAnulacion = $"{d.Etq} {d.Numero} modificada"; }

                    Guardar(d.Ref, d.NumeroBase, d.Fecha, $"{d.Etq} {d.Numero} (devengado)", usuarioId ?? d.Usuario, reemplaza, lineas, d.Numero);
                    if (reemplaza) r.Reemplazados++; else r.Creados++;
                    procesadosEnEstaLlamada++;
                }
            }

            // ===== 3) Devoluciones que bajan una cuenta por cobrar (venta a crédito aún sin cobrar) =====
            // No mueven dinero: Debe Devoluciones en ventas (base) + ISV por pagar + envíos; Haber Clientes.
            if (idCxC != null && idVentas != null)
            {
                var idDevVentas = Cod("4.1.03") ?? idVentas;
                var ventasVigentes = new HashSet<int>(context.Ventas.AsNoTracking()
                    .Where(v => v.EsCredito && v.Estado != "Anulada" && v.Estado != "Cancelada" && v.Total > 0)
                    .Select(v => v.Id).ToList());

                foreach (var dv in context.Devoluciones.AsNoTracking().Where(x => x.MontoAplicadoSaldo > 0 && x.VentaId != null).ToList())
                {
                    var refDev = PrefijoDevengo + "D" + dv.Id;
                    porReferencia.TryGetValue(refDev, out var existentes);
                    var vigente = dv.Estado == "Aplicada" && ventasVigentes.Contains(dv.VentaId!.Value)
                                  && ventasInfo.ContainsKey(dv.VentaId.Value);
                    if (!vigente)
                    {
                        if (existentes != null)
                            foreach (var a in existentes) { a.Estado = "Anulado"; a.MotivoAnulacion = $"Devolución {dv.Numero} anulada"; r.Anulados++; }
                        continue;
                    }

                    var info = ventasInfo[dv.VentaId!.Value];
                    var aplicado = Math.Round(dv.MontoAplicadoSaldo, 2);
                    var isv = idIsvPorPagar != null ? Math.Round(aplicado * info.Isv, 2) : 0m;
                    var env = idEnvios != null ? Math.Round(aplicado * info.Envio, 2) : 0m;
                    if (isv + env >= aplicado) { isv = 0m; env = 0m; }
                    var baseDev = aplicado - isv - env;

                    var lineas = new List<(int cuenta, decimal debe, decimal haber)> { (idDevVentas!.Value, baseDev, 0m) };
                    if (isv > 0m) lineas.Add((idIsvPorPagar!.Value, isv, 0m));
                    if (env > 0m) lineas.Add((idEnvios!.Value, env, 0m));
                    lineas.Add((idCxC.Value, 0m, aplicado));

                    if (Igual(existentes, dv.Fecha, lineas)) continue;
                    bool reemplaza = existentes != null && existentes.Count > 0;
                    if (procesadosEnEstaLlamada >= maximoPorLlamada) { r.HayMasPendientes = true; continue; }
                    if (reemplaza)
                        foreach (var a in existentes!) { a.Estado = "Anulado"; a.MotivoAnulacion = $"Devolución {dv.Numero} modificada"; }

                    Guardar(refDev, $"AUT-D{dv.Id:D7}", dv.Fecha, $"Devolución {dv.Numero} contra cuenta por cobrar", usuarioId ?? dv.UsuarioCreoId, reemplaza, lineas, dv.Numero);
                    if (reemplaza) r.Reemplazados++; else r.Creados++;
                    procesadosEnEstaLlamada++;
                }
            }

            // ===== 4) Compras recibidas (total o parcialmente) y aún sin pagar =====
            // Debe Mercadería (base) + ISV (crédito fiscal); Haber Proveedores. Se asienta lo pendiente de pago:
            // lo que ya se pagó entra por el asiento del pago (Debe Mercadería / Haber Caja o Banco).
            int? idInventario = Cod("1.1.03.001");
            int? idProveedores = Cod("2.1.01.001");
            if (idInventario != null && idProveedores != null)
            {
                var pendientes = DevengoHelper.ComprasPendientes(context, DateTime.Today.AddDays(1));
                var refsVigentes = new HashSet<string>(pendientes.Select(c => PrefijoDevengo + "C" + c.OrdenId));

                foreach (var kv in porReferencia.Where(k => k.Key.StartsWith(PrefijoDevengo + "C") && !refsVigentes.Contains(k.Key)))
                    foreach (var a in kv.Value)
                    {
                        a.Estado = "Anulado";
                        a.MotivoAnulacion = "La compra ya no tiene saldo pendiente con el proveedor";
                        r.Anulados++;
                    }

                foreach (var c in pendientes)
                {
                    var refCompra = PrefijoDevengo + "C" + c.OrdenId;
                    porReferencia.TryGetValue(refCompra, out var existentes);

                    var isv = idIsvPorPagar != null ? c.Isv : 0m;
                    var lineas = new List<(int cuenta, decimal debe, decimal haber)>
                    {
                        (idInventario.Value, c.Pendiente - isv, 0m)
                    };
                    if (isv > 0m) lineas.Add((idIsvPorPagar!.Value, isv, 0m));
                    lineas.Add((idProveedores.Value, 0m, c.Pendiente));

                    if (Igual(existentes, c.Fecha, lineas)) continue;
                    bool reemplaza = existentes != null && existentes.Count > 0;
                    if (procesadosEnEstaLlamada >= maximoPorLlamada) { r.HayMasPendientes = true; continue; }
                    if (reemplaza)
                        foreach (var a in existentes!) { a.Estado = "Anulado"; a.MotivoAnulacion = $"Compra {c.Numero} modificada"; }

                    Guardar(refCompra, $"AUT-C{c.OrdenId:D7}", c.Fecha, $"Compra {c.Numero} recibida, pendiente de pago", usuarioId, reemplaza, lineas, c.Numero);
                    if (reemplaza) r.Reemplazados++; else r.Creados++;
                    procesadosEnEstaLlamada++;
                }
            }

            context.SaveChanges();
            return r;
        }

        private record InfoDoc(decimal Isv, decimal Envio, bool AlCredito);

        private static (int? debe, int? haber) ResolverCuentas(
            MovimientoFinanciero mov,
            Dictionary<int, CuentaFinanciera> cuentas,
            Dictionary<int, CategoriaFinanciera> categorias,
            Func<string, int?> cod,
            HashSet<int> idPlanValido,
            ApplicationDbContext context)
        {
            int? CuentaContableDe(int cuentaId)
            {
                if (!cuentas.TryGetValue(cuentaId, out var c)) return null;
                if (!string.IsNullOrWhiteSpace(c.CuentaContable))
                {
                    var porCodigo = cod(c.CuentaContable.Trim());
                    if (porCodigo != null) return porCodigo;
                }
                if (c.Subtipo == "CuentasPorCobrar") return cod("1.1.02.001");
                return c.Tipo == "Banco" ? cod("1.1.01.004") : cod("1.1.01.001");
            }

            int? CategoriaContable(string tipo)
            {
                if (mov.CategoriaId.HasValue && categorias.TryGetValue(mov.CategoriaId.Value, out var cat))
                {
                    if (cat.PlanCuentaId.HasValue && idPlanValido.Contains(cat.PlanCuentaId.Value))
                        return cat.PlanCuentaId;
                    if (!string.IsNullOrWhiteSpace(cat.CuentaContable))
                    {
                        var porCodigo = cod(cat.CuentaContable.Trim());
                        if (porCodigo != null) return porCodigo;
                    }
                }
                return tipo == "Ingreso" ? cod("4.2.02") : cod("6.3.02");
            }

            if (mov.Origen == "Apertura")
                return (CuentaContableDe(mov.CuentaId), cod("3.1.01"));

            switch (mov.Tipo)
            {
                case "Transferencia":
                    if (!mov.CuentaDestinoId.HasValue) return (null, null);
                    return (CuentaContableDe(mov.CuentaDestinoId.Value), CuentaContableDe(mov.CuentaId));

                case "Ingreso":
                    return (CuentaContableDe(mov.CuentaId), CategoriaContable("Ingreso"));

                case "Egreso":
                    if (SaldoHelper.EsSoloContable(context, mov))
                        return (CategoriaContable("Egreso"), cod("1.1.03.001"));
                    return (CategoriaContable("Egreso"), CuentaContableDe(mov.CuentaId));

                default:
                    return (null, null);
            }
        }
    }
}
